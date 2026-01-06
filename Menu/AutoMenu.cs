using CustosAC.Scanner;
using CustosAC.UI;
using CustosAC.Memory;
using CustosAC.ProcessAnalysis;
using CustosAC.SystemAnalysis;
using CustosAC.Network;

namespace CustosAC.Menu;

public static class AutoMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("АВТОМАТИЧЕСКАЯ ПРОВЕРКА", new[]
            {
                "Автосканирование AppData",
                "Автосканирование системных папок",
                "Автосканирование Prefetch",
                "Поиск в реестре по ключевым словам",
                "Парсинг Steam аккаунтов",
                "Проверка сайтов (oplata.info, funpay.com)",
                "Проверка Telegram (боты и загрузки)",
                "────────────────────────────────",
                "Сканирование по хешам",
                "Анализ PE-файлов",
                "Проверка цифровых подписей",
                "────────────────────────────────",
                "> ЗАПУСТИТЬ ВСЕ ПРОВЕРКИ"
            }, true);

            int choice = ConsoleUI.GetChoice(11);

            switch (choice)
            {
                case 0:
                    return;
                case 1:
                    AppDataScanner.ScanAppData();
                    break;
                case 2:
                    SystemScanner.ScanSystemFolders();
                    break;
                case 3:
                    PrefetchScanner.ScanPrefetch();
                    break;
                case 4:
                    RegistryScanner.SearchRegistry();
                    break;
                case 5:
                    SteamScanner.ParseSteamAccounts();
                    break;
                case 6:
                    Common.CheckWebsites();
                    break;
                case 7:
                    Common.CheckTelegram();
                    break;
                case 8:
                    HashScanner.ScanDirectory();
                    break;
                case 9:
                    PEAnalyzer.ScanDirectory();
                    break;
                case 10:
                    SignatureVerifier.ScanUnsignedFiles();
                    break;
                case 11:
                    // Запустить все проверки
                    ConsoleUI.PrintHeader();
                    Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ЗАПУСК ВСЕХ АВТОМАТИЧЕСКИХ ПРОВЕРОК ═══{ConsoleUI.ColorReset}\n");

                    // Базовые проверки
                    Console.WriteLine($"{ConsoleUI.ColorYellow}[1/10] Сканирование AppData...{ConsoleUI.ColorReset}");
                    AppDataScanner.ScanAppData();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[2/10] Сканирование системных папок...{ConsoleUI.ColorReset}");
                    SystemScanner.ScanSystemFolders();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[3/10] Сканирование Prefetch...{ConsoleUI.ColorReset}");
                    PrefetchScanner.ScanPrefetch();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[4/10] Поиск в реестре...{ConsoleUI.ColorReset}");
                    RegistryScanner.SearchRegistry();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[5/10] Парсинг Steam...{ConsoleUI.ColorReset}");
                    SteamScanner.ParseSteamAccounts();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[6/10] Проверка сайтов...{ConsoleUI.ColorReset}");
                    Common.CheckWebsites();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[7/10] Проверка Telegram...{ConsoleUI.ColorReset}");
                    Common.CheckTelegram();

                    // Расширенные проверки
                    Console.WriteLine($"{ConsoleUI.ColorYellow}[8/10] Сканирование по хешам...{ConsoleUI.ColorReset}");
                    HashScanner.ScanDirectory();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[9/10] Анализ PE-файлов...{ConsoleUI.ColorReset}");
                    PEAnalyzer.ScanDirectory();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[10/10] Проверка подписей...{ConsoleUI.ColorReset}");
                    SignatureVerifier.ScanUnsignedFiles();

                    ConsoleUI.PrintHeader();
                    Console.WriteLine($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}═══ ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ ═══{ConsoleUI.ColorReset}\n");
                    ConsoleUI.Log("+ Все автоматические проверки выполнены!", true);
                    ConsoleUI.Pause();
                    break;
            }
        }
    }
}
