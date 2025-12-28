package menu

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"

	"manual-cobra/internal/scanner"
	"manual-cobra/internal/ui"
	"manual-cobra/internal/winapi"
)

// NetworkMenu меню сети и интернета
func NetworkMenu() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ СЕТЬ И ИНТЕРНЕТ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	scanner.RunCommand("ms-settings:datausage", "Использование данных")

	fmt.Printf("\n%sЧТО НУЖНО ПРОВЕРИТЬ:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s Неизвестные .exe файлы с сетевой активностью\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Подозрительные названия процессов\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Большой объем переданных данных\n", ui.ColorRed, ui.ColorReset)
	ui.Pause()
}

// DefenderMenu меню защиты Windows
func DefenderMenu() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ЗАЩИТА WINDOWS ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	scanner.RunCommand("windowsdefender://threat/", "Журнал защиты Windows Defender")

	fmt.Printf("\n%sКЛЮЧЕВЫЕ СЛОВА ДЛЯ ПОИСКА:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s undead, melony, ancient, loader\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s hack, cheat, unturned, bypass\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s inject, overlay, esp, aimbot\n", ui.ColorRed, ui.ColorReset)
	ui.Pause()
}

// FoldersMenu меню системных папок
func FoldersMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("СИСТЕМНЫЕ ПАПКИ", []string{
			"AppData\\Roaming",
			"AppData\\Local",
			"AppData\\LocalLow",
			"Videos (видео)",
			"Prefetch (запущенные .exe)",
			"Открыть все",
		}, true)

		choice := ui.GetChoice(6)
		if choice == 0 {
			break
		}

		ui.PrintHeader()
		appdata := os.Getenv("APPDATA")
		localappdata := os.Getenv("LOCALAPPDATA")
		userprofile := os.Getenv("USERPROFILE")

		switch choice {
		case 1:
			scanner.OpenFolder(appdata, "AppData\\Roaming")
			ui.Pause()
		case 2:
			scanner.OpenFolder(localappdata, "AppData\\Local")
			ui.Pause()
		case 3:
			scanner.OpenFolder(filepath.Join(userprofile, "AppData", "LocalLow"), "AppData\\LocalLow")
			ui.Pause()
		case 4:
			scanner.OpenFolder(filepath.Join(userprofile, "Videos"), "Videos")
			ui.Pause()
		case 5:
			scanner.OpenFolder("C:\\Windows\\Prefetch", "Prefetch")
			ui.Pause()
		case 6:
			scanner.OpenFolder(appdata, "Roaming")
			scanner.OpenFolder(localappdata, "Local")
			scanner.OpenFolder(filepath.Join(userprofile, "AppData", "LocalLow"), "LocalLow")
			scanner.OpenFolder(filepath.Join(userprofile, "Videos"), "Videos")
			scanner.OpenFolder("C:\\Windows\\Prefetch", "Prefetch")
			ui.Pause()
		}
	}
}

// RegistryMenu меню реестра Windows
func RegistryMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("РЕЕСТР WINDOWS", []string{
			"Открыть regedit",
			"MuiCache (запущенные программы)",
			"AppSwitched (переключения Alt+Tab)",
			"ShowJumpView (JumpList история)",
		}, true)

		choice := ui.GetChoice(4)
		if choice == 0 {
			break
		}

		ui.PrintHeader()
		switch choice {
		case 1:
			cmd := exec.Command("regedit.exe")
			err := cmd.Start()
			if err == nil {
				winapi.TrackProcess(cmd)
				go func() {
					cmd.Wait()
					winapi.UntrackProcess(cmd)
				}()
				ui.Log("Regedit открыт", true)
			} else {
				ui.Log(fmt.Sprintf("Ошибка: %v", err), false)
			}
			ui.Pause()
		case 2:
			scanner.OpenRegistry(`HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache`)
			ui.Pause()
		case 3:
			scanner.OpenRegistry(`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched`)
			ui.Pause()
		case 4:
			scanner.OpenRegistry(`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView`)
			ui.Pause()
		}
	}
}

// UtilitiesMenu меню утилит
func UtilitiesMenu() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ УТИЛИТЫ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	fmt.Printf("  %s[i]%s Открываем ссылки на утилиты для проверки...%s\n\n", ui.ColorBlue, ui.ColorReset, ui.ColorReset)

	scanner.RunCommand("https://www.voidtools.com/downloads/", "Everything (поиск файлов)")
	scanner.RunCommand("https://www.nirsoft.net/utils/computer_activity_view.html", "ComputerActivityView")
	scanner.RunCommand("https://www.nirsoft.net/utils/usb_devices_view.html", "USBDevicesView")
	scanner.RunCommand("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php", "ShellBag Analyzer")

	fmt.Printf("\n%sУТИЛИТЫ:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s Everything - быстрый поиск файлов на ПК\n", ui.ColorCyan, ui.ColorReset)
	fmt.Printf("  %s►%s ComputerActivityView - активность компьютера\n", ui.ColorCyan, ui.ColorReset)
	fmt.Printf("  %s►%s USBDevicesView - история USB устройств\n", ui.ColorCyan, ui.ColorReset)
	fmt.Printf("  %s►%s ShellBag Analyzer - анализ посещенных папок\n", ui.ColorCyan, ui.ColorReset)
	ui.Pause()
}

// SteamCheckMenu меню проверки Steam аккаунтов
func SteamCheckMenu() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ПРОВЕРКА STEAM АККАУНТОВ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	// Парсинг аккаунтов Steam
	vdfPaths := []string{
		`C:\Program Files (x86)\Steam\config\loginusers.vdf`,
		`C:\Program Files\Steam\config\loginusers.vdf`,
	}

	drives := []string{"D:", "E:", "F:"}
	for _, drive := range drives {
		vdfPaths = append(vdfPaths, filepath.Join(drive, "Steam", "config", "loginusers.vdf"))
		vdfPaths = append(vdfPaths, filepath.Join(drive, "Program Files (x86)", "Steam", "config", "loginusers.vdf"))
		vdfPaths = append(vdfPaths, filepath.Join(drive, "Program Files", "Steam", "config", "loginusers.vdf"))
	}

	var vdfPath string
	for _, path := range vdfPaths {
		if _, err := os.Stat(path); err == nil {
			vdfPath = path
			break
		}
	}

	if vdfPath == "" {
		ui.Log("Файл loginusers.vdf не найден", false)
		fmt.Printf("\n%s⚠ Steam может быть не установлен или находится в нестандартной директории%s\n", ui.ColorYellow, ui.ColorReset)
		ui.Pause()
		return
	}

	ui.Log(fmt.Sprintf("Найден файл: %s", vdfPath), true)
	fmt.Println()

	// Парсинг Steam аккаунтов
	scanner.ParseSteamAccountsFromPath(vdfPath)

	// Дополнительная информация
	fmt.Printf("\n%s%s%s\n", ui.ColorCyan, "─────────────────────────────────────────", ui.ColorReset)

	fmt.Printf("\n%sЧТО НУЖНО ПРОВЕРИТЬ:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s Конфигурационные файлы Steam\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Информация об аккаунтах\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Логи и настройки\n", ui.ColorRed, ui.ColorReset)

	ui.Pause()
}

// UnturnedMenu меню Unturned
func UnturnedMenu() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ UNTURNED ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	possiblePaths := []string{
		`C:\Program Files (x86)\Steam\steamapps\common\Unturned\Screenshots`,
		`C:\Program Files\Steam\steamapps\common\Unturned\Screenshots`,
	}

	drives := []string{"D:", "E:", "F:"}
	for _, drive := range drives {
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Steam", "steamapps", "common", "Unturned", "Screenshots"))
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Program Files (x86)", "Steam", "steamapps", "common", "Unturned", "Screenshots"))
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Program Files", "Steam", "steamapps", "common", "Unturned", "Screenshots"))
	}

	found := false
	for _, screenshots := range possiblePaths {
		if _, err := os.Stat(screenshots); !os.IsNotExist(err) {
			found = true
			fmt.Printf("  %s[i]%s Найдено: %s%s%s\n\n", ui.ColorBlue, ui.ColorReset, ui.ColorCyan, screenshots, ui.ColorReset)
			if scanner.OpenFolder(screenshots, "Папка Screenshots Unturned") {
				fmt.Printf("\n%sЧТО НУЖНО ПРОВЕРИТЬ:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
				fmt.Printf("  %s►%s UI читов на скриншотах\n", ui.ColorRed, ui.ColorReset)
				fmt.Printf("  %s►%s ESP/Wallhack индикаторы\n", ui.ColorRed, ui.ColorReset)
				fmt.Printf("  %s►%s Overlay меню\n", ui.ColorRed, ui.ColorReset)
				fmt.Printf("  %s►%s Необычные элементы интерфейса\n", ui.ColorRed, ui.ColorReset)
			}
			break
		}
	}

	if !found {
		ui.Log("Папка Steam\\steamapps\\common\\Unturned\\Screenshots не найдена в системе", false)
		fmt.Printf("\n%s⚠ Unturned может быть не установлен или находится в нестандартной директории%s\n", ui.ColorYellow, ui.ColorReset)
	}

	ui.Pause()
}

// ExtraMenu дополнительное меню с утилитами
func ExtraMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("EXXXXXTRA", []string{
			"Включить реестр",
			"Включить параметры системы и сеть",
		}, true)

		choice := ui.GetChoice(2)
		if choice == 0 {
			break
		}

		ui.PrintHeader()
		switch choice {
		case 1:
			// Включить реестр
			cmd := exec.Command("reg", "delete", `HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\regedit.exe`, "/f")
			err := cmd.Run()
			if err != nil {
				ui.Log(fmt.Sprintf("Ошибка при включении реестра: %v", err), false)
				fmt.Printf("\n%s⚠ Возможно реестр уже включен или требуются права администратора%s\n", ui.ColorYellow, ui.ColorReset)
			} else {
				ui.Log("Реестр успешно включен", true)
				fmt.Printf("\n%s✓ Теперь вы можете открыть regedit%s\n", ui.ColorGreen, ui.ColorReset)
			}
			ui.Pause()
		case 2:
			// Включить параметры системы (Settings)
			ui.Log("Разблокируем доступ к параметрам системы...", true)
			fmt.Println()

			success := true

			// 1. Удаляем блокировку Settings через групповые политики (HKCU)
			cmd := exec.Command("reg", "delete", `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`, "/v", "NoControlPanel", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка NoControlPanel (HKCU)", true)
			}

			// 2. Удаляем блокировку Settings через групповые политики (HKLM)
			cmd = exec.Command("reg", "delete", `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`, "/v", "NoControlPanel", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка NoControlPanel (HKLM)", true)
			}

			// 3. Удаляем блокировку доступа к настройкам сети (HKCU)
			cmd = exec.Command("reg", "delete", `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network`, "/v", "NoNetSetup", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка NoNetSetup (HKCU)", true)
			}

			// 4. Удаляем блокировку доступа к настройкам сети (HKLM)
			cmd = exec.Command("reg", "delete", `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network`, "/v", "NoNetSetup", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка NoNetSetup (HKLM)", true)
			}

			// 5. Удаляем блокировку Settings App
			cmd = exec.Command("reg", "delete", `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`, "/v", "SettingsPageVisibility", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка SettingsPageVisibility (HKCU)", true)
			}

			// 6. Удаляем блокировку Settings App (HKLM)
			cmd = exec.Command("reg", "delete", `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`, "/v", "SettingsPageVisibility", "/f")
			if err := cmd.Run(); err == nil {
				ui.Log("✓ Удалена блокировка SettingsPageVisibility (HKLM)", true)
			}

			// 7. Проверяем доступность Settings
			fmt.Println()
			ui.Log("Проверяем доступность параметров системы...", true)

			cmd = exec.Command("cmd", "/c", "start", "ms-settings:network")
			if err := cmd.Run(); err != nil {
				ui.Log("Не удалось открыть параметры сети", false)
				success = false
			} else {
				ui.Log("✓ Параметры сети открыты успешно", true)
			}

			fmt.Println()
			if success {
				fmt.Printf("%s╔════════════════════════════════════════════╗%s\n", ui.ColorGreen+ui.ColorBold, ui.ColorReset)
				fmt.Printf("%s║  ✓ ПАРАМЕТРЫ СИСТЕМЫ РАЗБЛОКИРОВАНЫ       ║%s\n", ui.ColorGreen, ui.ColorReset)
				fmt.Printf("%s╚════════════════════════════════════════════╝%s\n", ui.ColorGreen+ui.ColorBold, ui.ColorReset)
			} else {
				fmt.Printf("\n%s⚠ Если параметры не открылись:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
				fmt.Printf("  %s►%s Запустите программу от имени администратора\n", ui.ColorYellow, ui.ColorReset)
				fmt.Printf("  %s►%s Проверьте групповые политики (gpedit.msc)\n", ui.ColorYellow, ui.ColorReset)
			}
			ui.Pause()
		}
	}
}

// ManualCheckMenu главное меню ручной проверки
func ManualCheckMenu() {
	for {
		ui.PrintHeader()
		ui.PrintMenu("🔍 РУЧНАЯ ПРОВЕРКА", []string{
			"Сеть и интернет",
			"Защита Windows",
			"Утилиты",
			"Системные папки",
			"Реестр Windows",
			"Проверка Steam аккаунтов",
			"Unturned",
			"Проверка сайтов (oplata.info, funpay.com)",
			"Проверка Telegram (боты и загрузки)",
			"📋 Скопировать ключевые слова",
		}, true)

		choice := ui.GetChoice(10)

		switch choice {
		case 0:
			return
		case 1:
			NetworkMenu()
		case 2:
			DefenderMenu()
		case 3:
			UtilitiesMenu()
		case 4:
			FoldersMenu()
		case 5:
			RegistryMenu()
		case 6:
			SteamCheckMenu()
		case 7:
			UnturnedMenu()
		case 8:
			scanner.CheckWebsites()
		case 9:
			scanner.CheckTelegram()
		case 10:
			ui.PrintHeader()
			scanner.CopyKeywordsToClipboard()
			ui.Pause()
		}
	}
}
