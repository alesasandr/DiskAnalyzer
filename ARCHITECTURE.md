# Архитектура проекта Disk Analyzer

## Обзор

Проект реализован по принципу **чистой архитектуры** (Clean Architecture): бизнес-логика полностью отделена от UI и не имеет зависимостей на WPF или любой другой UI-фреймворк. Это позволяет повторно использовать ядро из консольного приложения, MAUI, Avalonia и т.д.

```
┌─────────────────────────────────────────────────┐
│               DiskAnalyzer.WPF                  │  ← UI-слой
│         (Views, ViewModels, Converters)          │
└──────────────────────┬──────────────────────────┘
                       │ ссылается на
┌──────────────────────▼──────────────────────────┐
│              DiskAnalyzer.Core                  │  ← Бизнес-логика
│       (Models, Interfaces, Services)             │
└─────────────────────────────────────────────────┘
                       ▲
                       │ ссылается на
┌──────────────────────┴──────────────────────────┐
│             DiskAnalyzer.Console                │  ← CLI-слой
└─────────────────────────────────────────────────┘
```

---

## DiskAnalyzer.Core

### Модели (`Models/`)

#### `FileSystemNode` (абстрактный базовый класс)

Общий предок для папки и файла. Хранит имя, полный путь, размер, дату изменения, ссылку на родителя, процент от родителя и от корня.

```
FileSystemNode (abstract)
├── FolderNode      — директория, содержит дочерние узлы
└── FileNode        — файл с расширением
```

**Ключевое решение:** `PercentOfParent` и `PercentOfRoot` — это **вычисляемые свойства**, которые проставляются после полного обхода дерева в методе `DiskScanner.CalculatePercentages()`. Это эффективнее, чем пересчитывать их при каждом обращении.

#### `FolderNode`

Содержит `ObservableCollection<FileSystemNode> Children` — коллекцию дочерних узлов. Использование `ObservableCollection` позволяет TreeView и DataGrid автоматически реагировать на изменения через механизм привязки WPF.

`FileCount` и `FolderCount` — рекурсивные вычисляемые свойства:
```csharp
public int FileCount => Children.Sum(c => c is FolderNode f ? f.FileCount : 1);
```

#### `DriveItem`

Обёртка над `DriveInfo` с удобными форматированными свойствами (`FormattedTotal`, `FormattedFree`, `UsedPercent`). Не наследуется от `FileSystemNode` намеренно — диск это не узел файловой системы в контексте навигации.

#### `AppSettings`

POCO-класс настроек, сериализуемый через `System.Text.Json`. Хранится в `%AppData%\DiskAnalyzer\settings.json`.

#### `ScanCache` и `CachedFileSystemNode`

Сериализуемые модели для хранения результатов сканирования на диске.

`ScanCache` — корневой объект кэша: содержит путь сканирования, дату, суммарные счётчики и корневой узел дерева.

`CachedFileSystemNode` — плоская сериализуемая копия узла файловой системы. Отличается от `FileSystemNode` тем, что:
- не требует абстрактного базового класса (сериализатор работает с конкретными типами)
- хранит `IsDirectory` и `Extension` как явные поля вместо полиморфизма
- содержит рекурсивный список `Children`

При загрузке `JsonScanCacheService` восстанавливает живое дерево `FolderNode`/`FileNode` из этого плоского представления.

---

### Интерфейсы (`Interfaces/`)

| Интерфейс           | Назначение                                              |
|--------------------|---------------------------------------------------------|
| `IDiskScanner`      | Асинхронное сканирование с прогрессом и отменой        |
| `IFileSystemProvider` | Абстракция над `System.IO` (для тестируемости)       |
| `IReportExporter`   | Экспорт дерева в CSV и текстовый формат                |
| `ISettingsService`  | Загрузка/сохранение настроек                           |
| `IDriveProvider`    | Получение списка доступных дисков                      |
| `IScanCacheService` | Сохранение и загрузка результатов сканирования         |

**Зачем `IFileSystemProvider`?** В реальном проекте это позволяет подменить настоящий `System.IO` на mock в тестах. Тесты могут проверять логику сканирования на искусственной структуре папок, не касаясь диска.

---

### Сервисы (`Services/`)

#### `DiskScanner`

Центральный сервис. Алгоритм работы:

```
ScanAsync(path)
  └─ Task.Run(...)         ← весь обход в фоновом потоке
       └─ ScanFolder(path, depth=0)
            ├─ GetFiles() → создать FileNode для каждого файла
            ├─ GetDirectories() → рекурсивный ScanFolder для каждой подпапки
            ├─ RecalculateSize() ← снизу вверх
            └─ каждые 100 файлов → IProgress<ScanProgress>.Report()
  └─ CalculatePercentages()  ← однократно, после полного обхода
```

**Обработка ошибок:**  
`UnauthorizedAccessException` перехватывается на уровне каждой папки. Папка помечается флагом `IsAccessDenied = true` и продолжает существовать в дереве с нулевым размером — сканирование не прерывается.

**Отмена:**  
`CancellationToken` проверяется перед каждой директорией через `ct.ThrowIfCancellationRequested()`. При отмене выбрасывается `OperationCanceledException`, которое перехватывается в ViewModel.

**Прогресс:**  
Репортится через `IProgress<ScanProgress>`. `IProgress<T>` автоматически маршалит вызов в поток, из которого был создан (UI-поток) — это ключевое свойство интерфейса, позволяющее безопасно обновлять UI без `Dispatcher.Invoke`.

#### `SizeFormatter` (статический хелпер)

```csharp
SizeFormatter.Format(1_500_000_000) // → "1.4 GB"
```

Одно место форматирования — нет дублирования в моделях и конвертерах.

#### `ExtensionAnalyzer` (статический сервис)

Обходит всё дерево рекурсивно, группирует файлы по расширению (`Dictionary<string, (int count, long bytes)>`), затем сортирует по убыванию размера. Сложность: O(N) по числу файлов.

#### `JsonSettingsService`

Использует `System.Text.Json` (встроен в .NET). Файл создаётся при первом сохранении. При любой ошибке чтения (файл повреждён, не существует) возвращаются дефолтные настройки — приложение не падает.

#### `JsonScanCacheService`

Реализует `IScanCacheService`. Сохраняет и восстанавливает полное дерево файловой системы в JSON-файл.

**Расположение кэша:** `%AppData%\DiskAnalyzer\Cache\scan_<SHA256>.json`

**Именование файлов:** имя файла — первые 16 символов SHA256-хеша пути в верхнем регистре. Используется `SHA256.HashData` из `System.Security.Cryptography` вместо `string.GetHashCode()` — это принципиально важно: `GetHashCode()` в .NET Core рандомизируется при каждом запуске процесса, что делало бы невозможным найти сохранённый файл после перезапуска.

```csharp
var bytes = Encoding.UTF8.GetBytes(path.ToUpperInvariant());
var hash = SHA256.HashData(bytes);
var hashStr = Convert.ToHexString(hash)[..16]; // стабильно между запусками
```

**Восстановление дерева:** `ReconstructFolderNode` рекурсивно обходит `CachedFileSystemNode` и создаёт живые объекты `FolderNode`/`FileNode` с правильными ссылками `Parent` и пересчитанными `PercentOfParent`.

**Интеграция с запуском:** `MainViewModel` вызывает `LoadCache` в конструкторе, используя `LastScannedPath` из настроек. Так как это происходит до создания `MainWindow`, в `MainWindow` добавлена явная проверка `RootNode != null` после подписки на `PropertyChanged` — иначе TreeView оставался бы пустым, так как событие уже отработало.

---

## DiskAnalyzer.WPF

### Паттерн MVVM

```
View (XAML)  ←─ DataBinding ─→  ViewModel  ←─ использует ─→  Core Services
     │                               │
     └── нет логики в code-behind    └── INotifyPropertyChanged
```

**Правило:** В `*.xaml.cs` только:
- Передача ViewModel в `DataContext`
- Подписка на события ViewModel (диалоги, навигация)
- Обработчики UI-событий, которые делегируют в ViewModel (двойной клик → `vm.NavigateInto()`)

#### `RelayCommand`

Реализация `ICommand`. Используется `CommandManager.RequerySuggested` — WPF автоматически переспрашивает `CanExecute` при любом UI-действии. Это означает, что кнопка "Scan" автоматически серее, пока `IsScanning == true`, без ручного вызова.

```csharp
ScanCommand = new RelayCommand(
    async _ => await StartScanAsync(),
    _ => !IsScanning          // CanExecute
);
```

#### `MainViewModel`

Главная ViewModel. Ключевые принципы:

- **Состояние сканирования** — `IsScanning` и `IsCancelling` управляют видимостью кнопок через `BoolToVisibilityConverter`
- **Навигация** — стек хлебных крошек (`ObservableCollection<BreadcrumbItem>`) хранит путь. `NavigateInto` добавляет узел, `NavigateUp` удаляет последний, `NavigateToBreadcrumb` усекает до выбранного
- **Сортировка** — `CurrentSortMode` → `ApplySort()` → пересоздаёт `CurrentLevelItems` из текущей папки

**События для диалогов:**
```csharp
public event Func<string?>? BrowseFolderRequested;
public event Func<string, string?>? SaveFileRequested;
```
ViewModel не знает о конкретных диалогах WPF — она объявляет события, View подписывается и реализует диалог. Это сохраняет тестируемость ViewModel.

#### `SettingsViewModel`

Работает с копией настроек (`CopySettings`). Изменения применяются только при нажатии "Save". При "Cancel" — оригинал остаётся нетронутым. При "Reset" — поля заполняются из `new AppSettings()`.

---

### Конвертеры (`Converters/`)

| Конвертер                      | Назначение                                       |
|-------------------------------|--------------------------------------------------|
| `BoolToVisibilityConverter`    | `bool` → `Visible/Collapsed` (с флагом `Invert`) |
| `InverseBoolConverter`         | `bool` → `!bool`                                 |
| `FileSystemNodeToIconConverter`| Тип узла → эмодзи-иконка                         |
| `FolderBoldConverter`          | `FolderNode` → `FontWeights.Bold`                |
| `PercentToWidthConverter`      | `double` процент → нормализованное значение      |

---

### Темы (`Themes/`)

Все цвета определены как `SolidColorBrush` в словарях `DarkTheme.xaml` и `LightTheme.xaml`. В коде и XAML используются только `StaticResource`, ни одного hardcoded цвета.

Переключение темы в `App.xaml.cs`:
```csharp
Resources.MergedDictionaries[0] = new ResourceDictionary { Source = ... };
```

Первый словарь в `MergedDictionaries` — всегда активная тема. Замена одного словаря перекрашивает всё приложение.

---

## DiskAnalyzer.Console

Использует те же интерфейсы и сервисы из `Core`. Точка входа:

```
Program.cs (top-level statements)
  └─ CommandLineParser.Parse(args)
  └─ DiskScanner.ScanAsync(...)
  └─ PrintTree(root, ...)      ← рекурсивный вывод с Unicode-псевдографикой
  └─ ExtensionAnalyzer.Analyze(root)
  └─ CsvReportExporter.ExportToCsvAsync(...)
```

Прогресс в консоли — перезапись строки через `\r` (carriage return) без перевода строки, имитируя живой счётчик.

---

## Принятые архитектурные решения

| Решение | Обоснование |
|--------|-------------|
| `ObservableCollection` в моделях | TreeView и DataGrid реагируют на изменения автоматически |
| Расчёт процентов после обхода | Однократный проход O(N) вместо O(N²) при вычислении в геттерах |
| `IProgress<T>` вместо `Dispatcher.Invoke` | Потокобезопасность встроена в интерфейс, нет связанности с WPF |
| События для диалогов в ViewModel | ViewModel не зависит от WPF-диалогов, остаётся тестируемой |
| Нет сторонних библиотек | Только стандартная библиотека .NET — меньше зависимостей, проще сборка |
| `IFileSystemProvider` интерфейс | Позволяет подменить `System.IO` в unit-тестах |
| SHA256 вместо `GetHashCode()` для имён кэша | `GetHashCode()` в .NET Core нестабилен между процессами — один и тот же путь даёт разный хеш при каждом запуске, файл невозможно найти повторно |
| Проверка `RootNode` после подписки в `MainWindow` | `LoadCache` вызывается в конструкторе ViewModel до создания View; `PropertyChanged` к этому моменту уже отработало — без явной проверки TreeView оставался бы пустым |
