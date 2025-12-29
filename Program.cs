using CustosAC.Menu;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC;

class Program
{
    static void Main(string[] args)
    {
        // Проверка и запрос прав администратора
        AdminHelper.RunAsAdmin();

        // Установка статуса администратора для UI
        ConsoleUI.SetAdminStatus(AdminHelper.IsAdmin());

        // Настройка обработчика закрытия консоли
        AdminHelper.SetupCloseHandler(() =>
        {
            AdminHelper.KillAllProcesses();
            ConsoleUI.PrintCleanupMessage();
        });

        // Настройка консоли (цвета, фиксированный размер)
        ConsoleUI.SetupConsole();

        // Запуск главного меню
        MainMenu.Run();
    }
}
