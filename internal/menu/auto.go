package menu

import (
	"fmt"

	"manual-cobra/internal/scanner"
	"manual-cobra/internal/ui"
)

// AutoCheckMenu меню автоматической проверки
func AutoCheckMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("🤖 АВТОМАТИЧЕСКАЯ ПРОВЕРКА", []string{
			"Автосканирование AppData",
			"Автосканирование системных папок",
			"Автосканирование Prefetch",
			"Поиск в реестре по ключевым словам",
			"Парсинг Steam аккаунтов",
			"Проверка сайтов (oplata.info, funpay.com)",
			"Проверка Telegram (боты и загрузки)",
			"────────────────────────────────",
			"🚀 ЗАПУСТИТЬ ВСЕ ПРОВЕРКИ",
		}, true)

		choice := ui.GetChoice(9)

		switch choice {
		case 0:
			return
		case 1:
			scanner.ScanAppData()
		case 2:
			scanner.ScanSystemFolders()
		case 3:
			scanner.ScanPrefetch()
		case 4:
			scanner.SearchRegistry()
		case 5:
			scanner.ParseSteamAccounts()
		case 6:
			scanner.CheckWebsites()
		case 7:
			scanner.CheckTelegram()
		case 8:
			continue
		case 9:
			scanner.ScanAppData()
			scanner.ScanSystemFolders()
			scanner.ScanPrefetch()
			scanner.SearchRegistry()
			scanner.ParseSteamAccounts()
			scanner.CheckWebsites()
			scanner.CheckTelegram()

			ui.PrintHeader()
			fmt.Printf("\n%s═══ ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ ═══%s\n\n", ui.ColorGreen+ui.ColorBold, ui.ColorReset)
			ui.Log("✓ Все автоматические проверки выполнены!", true)
			ui.Pause()
		}
	}
}
