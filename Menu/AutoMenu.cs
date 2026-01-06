using CustosAC.Scanner;
using CustosAC.UI;

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
                "> ЗАПУСТИТЬ ВСЕ ПРОВЕРКИ"
            }, true);

            int choice = ConsoleUI.GetChoice(8);

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
                    // Запустить все проверки
                    ConsoleUI.PrintHeader();
                    Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ЗАПУСК ВСЕХ АВТОМАТИЧЕСКИХ ПРОВЕРОК ═══{ConsoleUI.ColorReset}\n");

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[1/7] Сканирование AppData...{ConsoleUI.ColorReset}");
                    AppDataScanner.ScanAppData();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[2/7] Сканирование системных папок...{ConsoleUI.ColorReset}");
                    SystemScanner.ScanSystemFolders();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[3/7] Сканирование Prefetch...{ConsoleUI.ColorReset}");
                    PrefetchScanner.ScanPrefetch();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[4/7] Поиск в реестре...{ConsoleUI.ColorReset}");
                    RegistryScanner.SearchRegistry();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[5/7] Парсинг Steam...{ConsoleUI.ColorReset}");
                    SteamScanner.ParseSteamAccounts();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[6/7] Проверка сайтов...{ConsoleUI.ColorReset}");
                    Common.CheckWebsites();

                    Console.WriteLine($"{ConsoleUI.ColorYellow}[7/7] Проверка Telegram...{ConsoleUI.ColorReset}");
                    Common.CheckTelegram();

                    ConsoleUI.PrintHeader();
                    Console.WriteLine($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}═══ ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ ═══{ConsoleUI.ColorReset}\n");
                    ConsoleUI.Log("+ Все автоматические проверки выполнены!", true);
                    ConsoleUI.Pause();
                    break;
            }
        }
    }
}
