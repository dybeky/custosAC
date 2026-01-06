using CustosAC.Constants;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Menu;

public static class MainMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("ГЛАВНОЕ МЕНЮ", new[]
            {
                "Ручная проверка",
                "Автоматическая проверка",
                "Экстра"
            }, false);

            int choice = ConsoleUI.GetChoice(3);

            switch (choice)
            {
                case 0:
                    ConsoleUI.ClearScreen();
                    Console.WriteLine("\n\n");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}╔════════════════════════════════════════════╗{ConsoleUI.ColorReset}");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}║     + Закрываем открытые процессы... +     ║{ConsoleUI.ColorReset}");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}╚════════════════════════════════════════════╝{ConsoleUI.ColorReset}");
                    Thread.Sleep(AppConstants.ExitDelay);
                    AdminHelper.KillAllProcesses();
                    ConsoleUI.PrintCleanupMessage();
                    Thread.Sleep(AppConstants.CleanupDelay);
                    return;
                case 1:
                    ManualMenu.Run();
                    break;
                case 2:
                    AutoMenu.Run();
                    break;
                case 3:
                    ExtraMenu.Run();
                    break;
            }
        }
    }
}
