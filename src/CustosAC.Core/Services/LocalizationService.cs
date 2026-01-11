using System.ComponentModel;

namespace CustosAC.Core.Services;

/// <summary>
/// Simple localization service supporting Russian and English
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private static readonly object _lock = new();

    public static LocalizationService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalizationService();
                }
            }
            return _instance;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _currentLanguage = "en";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged(nameof(Strings));
            }
        }
    }

    public LocalizedStrings Strings => CurrentLanguage == "ru" ? _russianStrings : _englishStrings;

    private readonly LocalizedStrings _englishStrings = new()
    {
        // Settings
        Settings = "Settings",
        SettingsSubtitle = "Configure custosAC preferences",
        Updates = "Updates",
        CheckOnStartup = "Check for updates on startup",
        CheckOnStartupDesc = "Automatically check for new versions when the app starts",
        AutoDownload = "Auto-download updates",
        AutoDownloadDesc = "Automatically download updates when available",
        CheckForUpdates = "CHECK FOR UPDATES",
        About = "About",
        Version = "Version:",
        Developer = "Developer:",
        ViewOnGitHub = "VIEW ON GITHUB",
        DangerZone = "Danger Zone",
        DeleteAfterUse = "Delete program after use",
        DeleteAfterUseDesc = "Automatically delete custosAC when you close it",
        DeleteProgramNow = "DELETE PROGRAM NOW",
        Back = "BACK",
        Language = "Language",
        LanguageDesc = "Select interface language",

        // Update dialog
        UpdateAvailable = "Update Available",
        NewVersionAvailable = "A new version is available",
        CurrentVersion = "Current version:",
        LatestVersion = "Latest version:",
        Update = "UPDATE",
        Dismiss = "DISMISS",
        UpToDate = "You're up to date!",
        YouHaveLatestVersion = "You have the latest version",
        Ok = "OK",

        // Dashboard
        Dashboard = "Dashboard",
        Welcome = "Welcome",
        WelcomeMessage = "Program written for COBRA servers to detect third-party software.",
        Info = "Info",
        Changelog = "Changelog",
        Loading = "Loading...",
        Released = "Released:",
        ForCobraServers = "for COBRA servers",
        ChangelogNotFound = "Changelog not found on GitHub",
        FailedToLoad = "Failed to load. Check internet connection.",

        // Scan
        Scan = "Scan",
        StartScan = "START SCAN",
        ReadyToScan = "Ready to scan",
        Scanning = "Scanning...",
        ScanningSubtitle = "Please wait, checking your system...",
        ScanComplete = "Scan Complete",
        CancelScan = "CANCEL SCAN",
        ScanDescription = "Scan AppData, Registry, Processes, Prefetch, Steam and more",
        ScannersReady = "scanners ready",
        Progress = "Progress:",
        Complete = "Complete!",
        Pending = "Pending",
        Found = "found",

        // Manual Check
        ManualCheck = "Manual Check",
        ManualCheckSubtitle = "Open folders and tools manually for investigation",
        SystemTools = "System Tools",
        Folders = "Folders",
        Games = "Games",
        Registry = "Registry",
        TelegramCheatBots = "Telegram Cheat Bots",
        TelegramBotsDesc = "Open these bots to check if the user has interacted with them",
        DataUsage = "Data Usage",
        Videos = "Videos",
        Downloads = "Downloads",

        // Utilities
        Utilities = "Utilities",
        UtilitiesSubtitle = "Useful tools for anti-cheat verification",
        OpenWebsite = "OPEN WEBSITE",
        LastActivityViewDesc = "Shows computer activity log including program executions, file operations and more",
        UsbDeviewDesc = "Lists all USB devices ever connected to the computer with dates and details",
        EverythingDesc = "Instant file search tool that indexes all files on your drives",
        SystemInformerDesc = "Advanced system monitor and process viewer (formerly Process Hacker)",

        // General
        Home = "Home",
        CheckUpdates = "Check Updates",
        NoReleases = "No releases found on GitHub",
        Error = "Error",
        NetworkUnavailable = "Network unavailable",
        Timeout = "Timeout",

        // Confirm dialogs
        ConfirmDelete = "Are you sure you want to delete custosAC?\n\nThis will close the application and remove all program files.",
        ConfirmDeleteTitle = "Confirm Delete",
        DeleteFailed = "Failed to delete program:",

        // Results
        Clean = "Clean",
        Suspicious = "Suspicious",
        FilesScanned = "Files scanned"
    };

    private readonly LocalizedStrings _russianStrings = new()
    {
        // Settings
        Settings = "Настройки",
        SettingsSubtitle = "Настройка параметров custosAC",
        Updates = "Обновления",
        CheckOnStartup = "Проверять обновления при запуске",
        CheckOnStartupDesc = "Автоматически проверять новые версии при запуске программы",
        AutoDownload = "Автозагрузка обновлений",
        AutoDownloadDesc = "Автоматически загружать обновления при их наличии",
        CheckForUpdates = "ПРОВЕРИТЬ ОБНОВЛЕНИЯ",
        About = "О программе",
        Version = "Версия:",
        Developer = "Разработчик:",
        ViewOnGitHub = "ОТКРЫТЬ НА GITHUB",
        DangerZone = "Опасная зона",
        DeleteAfterUse = "Удалить программу после использования",
        DeleteAfterUseDesc = "Автоматически удалить custosAC при закрытии",
        DeleteProgramNow = "УДАЛИТЬ СЕЙЧАС",
        Back = "НАЗАД",
        Language = "Язык",
        LanguageDesc = "Выберите язык интерфейса",

        // Update dialog
        UpdateAvailable = "Доступно обновление",
        NewVersionAvailable = "Доступна новая версия",
        CurrentVersion = "Текущая версия:",
        LatestVersion = "Последняя версия:",
        Update = "ОБНОВИТЬ",
        Dismiss = "ЗАКРЫТЬ",
        UpToDate = "Обновлений нет!",
        YouHaveLatestVersion = "У вас установлена последняя версия",
        Ok = "ОК",

        // Dashboard
        Dashboard = "Главная",
        Welcome = "Добро пожаловать",
        WelcomeMessage = "Программа написана для серверов COBRA, для выявления стороннего ПО.",
        Info = "Инфо",
        Changelog = "История изменений",
        Loading = "Загрузка...",
        Released = "Дата:",
        ForCobraServers = "для серверов COBRA",
        ChangelogNotFound = "История изменений не найдена на GitHub",
        FailedToLoad = "Не удалось загрузить. Проверьте интернет-соединение.",

        // Scan
        Scan = "Сканирование",
        StartScan = "НАЧАТЬ СКАНИРОВАНИЕ",
        ReadyToScan = "Готов к сканированию",
        Scanning = "Сканирование...",
        ScanningSubtitle = "Пожалуйста подождите, проверяем систему...",
        ScanComplete = "Сканирование завершено",
        CancelScan = "ОТМЕНИТЬ СКАНИРОВАНИЕ",
        ScanDescription = "Сканирование AppData, реестра, процессов, Prefetch, Steam и др.",
        ScannersReady = "сканеров готово",
        Progress = "Прогресс:",
        Complete = "Готово!",
        Pending = "Ожидание",
        Found = "найдено",

        // Manual Check
        ManualCheck = "Ручная проверка",
        ManualCheckSubtitle = "Откройте папки и инструменты для ручной проверки",
        SystemTools = "Системные инструменты",
        Folders = "Папки",
        Games = "Игры",
        Registry = "Реестр",
        TelegramCheatBots = "Telegram боты читов",
        TelegramBotsDesc = "Откройте этих ботов, чтобы проверить взаимодействие пользователя с ними",
        DataUsage = "Использование данных",
        Videos = "Видео",
        Downloads = "Загрузки",

        // Utilities
        Utilities = "Утилиты",
        UtilitiesSubtitle = "Полезные инструменты для проверки античита",
        OpenWebsite = "ОТКРЫТЬ САЙТ",
        LastActivityViewDesc = "Показывает журнал активности компьютера, включая запуски программ, операции с файлами и др.",
        UsbDeviewDesc = "Список всех USB-устройств, когда-либо подключенных к компьютеру с датами и деталями",
        EverythingDesc = "Мгновенный поиск файлов, индексирующий все файлы на ваших дисках",
        SystemInformerDesc = "Продвинутый монитор системы и просмотрщик процессов (ранее Process Hacker)",

        // General
        Home = "Главная",
        CheckUpdates = "Проверить обновления",
        NoReleases = "Релизы не найдены на GitHub",
        Error = "Ошибка",
        NetworkUnavailable = "Сеть недоступна",
        Timeout = "Таймаут",

        // Confirm dialogs
        ConfirmDelete = "Вы уверены, что хотите удалить custosAC?\n\nЭто закроет приложение и удалит все файлы программы.",
        ConfirmDeleteTitle = "Подтверждение удаления",
        DeleteFailed = "Не удалось удалить программу:",

        // Results
        Clean = "Чисто",
        Suspicious = "Подозрительно",
        FilesScanned = "Просканировано файлов"
    };

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class LocalizedStrings
{
    // Settings
    public string Settings { get; set; } = "";
    public string SettingsSubtitle { get; set; } = "";
    public string Updates { get; set; } = "";
    public string CheckOnStartup { get; set; } = "";
    public string CheckOnStartupDesc { get; set; } = "";
    public string AutoDownload { get; set; } = "";
    public string AutoDownloadDesc { get; set; } = "";
    public string CheckForUpdates { get; set; } = "";
    public string About { get; set; } = "";
    public string Version { get; set; } = "";
    public string Developer { get; set; } = "";
    public string ViewOnGitHub { get; set; } = "";
    public string DangerZone { get; set; } = "";
    public string DeleteAfterUse { get; set; } = "";
    public string DeleteAfterUseDesc { get; set; } = "";
    public string DeleteProgramNow { get; set; } = "";
    public string Back { get; set; } = "";
    public string Language { get; set; } = "";
    public string LanguageDesc { get; set; } = "";

    // Update dialog
    public string UpdateAvailable { get; set; } = "";
    public string NewVersionAvailable { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string Update { get; set; } = "";
    public string Dismiss { get; set; } = "";
    public string UpToDate { get; set; } = "";
    public string YouHaveLatestVersion { get; set; } = "";
    public string Ok { get; set; } = "";

    // Dashboard
    public string Dashboard { get; set; } = "";
    public string Welcome { get; set; } = "";
    public string WelcomeMessage { get; set; } = "";
    public string Info { get; set; } = "";
    public string Changelog { get; set; } = "";
    public string Loading { get; set; } = "";
    public string Released { get; set; } = "";
    public string ForCobraServers { get; set; } = "";
    public string ChangelogNotFound { get; set; } = "";
    public string FailedToLoad { get; set; } = "";

    // Scan
    public string Scan { get; set; } = "";
    public string StartScan { get; set; } = "";
    public string ReadyToScan { get; set; } = "";
    public string Scanning { get; set; } = "";
    public string ScanningSubtitle { get; set; } = "";
    public string ScanComplete { get; set; } = "";
    public string CancelScan { get; set; } = "";
    public string ScanDescription { get; set; } = "";
    public string ScannersReady { get; set; } = "";
    public string Progress { get; set; } = "";
    public string Complete { get; set; } = "";
    public string Pending { get; set; } = "";
    public string Found { get; set; } = "";

    // Manual Check
    public string ManualCheck { get; set; } = "";
    public string ManualCheckSubtitle { get; set; } = "";
    public string SystemTools { get; set; } = "";
    public string Folders { get; set; } = "";
    public string Games { get; set; } = "";
    public string Registry { get; set; } = "";
    public string TelegramCheatBots { get; set; } = "";
    public string TelegramBotsDesc { get; set; } = "";
    public string DataUsage { get; set; } = "";
    public string Videos { get; set; } = "";
    public string Downloads { get; set; } = "";

    // Utilities
    public string Utilities { get; set; } = "";
    public string UtilitiesSubtitle { get; set; } = "";
    public string OpenWebsite { get; set; } = "";
    public string LastActivityViewDesc { get; set; } = "";
    public string UsbDeviewDesc { get; set; } = "";
    public string EverythingDesc { get; set; } = "";
    public string SystemInformerDesc { get; set; } = "";

    // General
    public string Home { get; set; } = "";
    public string CheckUpdates { get; set; } = "";
    public string NoReleases { get; set; } = "";
    public string Error { get; set; } = "";
    public string NetworkUnavailable { get; set; } = "";
    public string Timeout { get; set; } = "";

    // Confirm dialogs
    public string ConfirmDelete { get; set; } = "";
    public string ConfirmDeleteTitle { get; set; } = "";
    public string DeleteFailed { get; set; } = "";

    // Results
    public string Clean { get; set; } = "";
    public string Suspicious { get; set; } = "";
    public string FilesScanned { get; set; } = "";
}
