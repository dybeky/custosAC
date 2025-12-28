package main

import (
	"manual-cobra/internal/menu"
	"manual-cobra/internal/ui"
	"manual-cobra/internal/winapi"
)

func main() {
	// Проверка и запрос прав администратора
	winapi.RunAsAdmin()

	// Установка статуса администратора для UI
	ui.SetAdminStatus(winapi.IsAdmin())

	// Настройка обработчика закрытия консоли
	winapi.SetupCloseHandler(winapi.Cleanup)

	// Настройка консоли (цвета, фиксированный размер)
	ui.SetupConsole()

	// Запуск главного меню
	menu.MainMenu()

	// Очистка при нормальном выходе
	winapi.Cleanup()
}
