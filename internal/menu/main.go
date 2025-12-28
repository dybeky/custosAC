package menu

import (
	"fmt"
	"time"

	"manual-cobra/internal/report"
	"manual-cobra/internal/ui"
	"manual-cobra/internal/winapi"
)

// MainMenu главное меню программы
func MainMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("ГЛАВНОЕ МЕНЮ", []string{
			"🔍 Ручная проверка",
			"🤖 Автоматическая проверка",
			"✨ EXXXXXTRA",
			"────────────────────────────────",
			"📤 ЗАВЕРШИТЬ ПРОВЕРКУ (отправить отчет)",
		}, false)

		choice := ui.GetChoice(5)

		switch choice {
		case 0:
			ui.ClearScreen()
			// Анимация выхода
			fmt.Printf("\n\n")
			fmt.Printf("  %s╔════════════════════════════════════════════╗%s\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)
			fmt.Printf("  %s║                                            ║%s\n", ui.ColorCyan, ui.ColorReset)
			fmt.Printf("  %s║%s     %s⚡ Закрываем открытые процессы... ⚡%s    %s║%s\n", ui.ColorCyan, ui.ColorReset, ui.ColorYellow, ui.ColorReset, ui.ColorCyan, ui.ColorReset)
			fmt.Printf("  %s║                                            ║%s\n", ui.ColorCyan, ui.ColorReset)
			fmt.Printf("  %s╚════════════════════════════════════════════╝%s\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)
			time.Sleep(800 * time.Millisecond)
			winapi.Cleanup()
			return
		case 1:
			ManualCheckMenu()
		case 2:
			AutoCheckMenu()
		case 3:
			ExtraMenu()
		case 4:
			continue
		case 5:
			ui.ClearScreen()
			report.SendDiscordReport()
		}
	}
}
