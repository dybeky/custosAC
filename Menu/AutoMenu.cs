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
            ConsoleUI.PrintMenu("🤖 АВТОМАТИЧЕСКАЯ ПРОВЕРКА", new[]
            {
                "Автосканирование AppData",
                "Автосканирование системных папок",
                "Автосканирование Prefetch",
                "Поиск в реестре по ключевым словам",
                "Парсинг Steam аккаунтов",
                "Проверка сайтов (oplata.info, funpay.com)",
                "Проверка Telegram (боты и загрузки)",
                "────────────────────────────────",
                "🚀 ЗАПУСТИТЬ ВСЕ ПРОВЕРКИ"
            }, true);

            int choice = ConsoleUI.GetChoice(9);

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
                    // Разделитель - пропускаем
                    continue;
                case 9:
                    // Запустить все проверки
                    AppDataScanner.ScanAppData();
                    SystemScanner.ScanSystemFolders();
                    PrefetchScanner.ScanPrefetch();
                    RegistryScanner.SearchRegistry();
                    SteamScanner.ParseSteamAccounts();
                    Common.CheckWebsites();
                    Common.CheckTelegram();

                    ConsoleUI.PrintHeader();
                    Console.WriteLine($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}═══ ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ ═══{ConsoleUI.ColorReset}\n");
                    ConsoleUI.Log("✓ Все автоматические проверки выполнены!", true);
                    ConsoleUI.Pause();
                    break;
            }
        }
    }
}
