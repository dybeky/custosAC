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
                "Расширенная проверка",
                "Экстра"
            }, false);

            int choice = ConsoleUI.GetChoice(4);

            switch (choice)
            {
                case 0:
                    ConsoleUI.ClearScreen();
                    Console.WriteLine("\n\n");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}╔════════════════════════════════════════════╗{ConsoleUI.ColorReset}");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}║     + Закрываем открытые процессы... +     ║{ConsoleUI.ColorReset}");
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}╚════════════════════════════════════════════╝{ConsoleUI.ColorReset}");
                    Thread.Sleep(800);
                    AdminHelper.KillAllProcesses();
                    ConsoleUI.PrintCleanupMessage();
                    Thread.Sleep(1500);
                    return;
                case 1:
                    ManualMenu.Run();
                    break;
                case 2:
                    AutoMenu.Run();
                    break;
                case 3:
                    AdvancedMenu.Run();
                    break;
                case 4:
                    ExtraMenu.Run();
                    break;
            }
        }
    }
}
