package scanner

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"manual-cobra/internal/report"
	"manual-cobra/internal/ui"
)

// ParseSteamAccountsFromPath парсинг loginusers.vdf из указанного пути
func ParseSteamAccountsFromPath(vdfPath string) {
	data, err := os.ReadFile(vdfPath)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка чтения файла: %v", err), false)
		return
	}

	content := string(data)
	lines := strings.Split(content, "\n")

	accounts := []string{}
	accountsInfo := []report.SteamAccountInfo{}
	var currentSteamID string

	for _, line := range lines {
		line = strings.TrimSpace(line)

		if strings.HasPrefix(line, "\"7656") && strings.Contains(line, "\"") {
			parts := strings.Split(line, "\"")
			if len(parts) >= 2 {
				currentSteamID = parts[1]
			}
		}

		if strings.Contains(line, "\"AccountName\"") && currentSteamID != "" {
			parts := strings.Split(line, "\"")
			if len(parts) >= 4 {
				accountName := parts[3]

				accountInfo := report.SteamAccountInfo{
					SteamID:     currentSteamID,
					AccountName: accountName,
				}
				accountsInfo = append(accountsInfo, accountInfo)

				// Формируем строку для отображения
				displayStr := fmt.Sprintf("SteamID: %s | Имя: %s", currentSteamID, accountName)
				fmt.Printf("%s►%s %s\n", ui.ColorCyan, ui.ColorReset, displayStr)

				accounts = append(accounts, displayStr)
				currentSteamID = ""
			}
		}
	}

	report.Results.SteamAccounts = accounts
	report.Results.SteamAccountsInfo = accountsInfo

	fmt.Println()
	if len(accounts) > 0 {
		ui.Log(fmt.Sprintf("Найдено аккаунтов Steam: %d", len(accounts)), true)
	} else {
		ui.Log("Аккаунты не найдены", false)
	}
}

// ParseSteamAccounts парсинг loginusers.vdf
func ParseSteamAccounts() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ПАРСИНГ STEAM АККАУНТОВ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	possiblePaths := []string{
		`C:\Program Files (x86)\Steam\config\loginusers.vdf`,
		`C:\Program Files\Steam\config\loginusers.vdf`,
	}

	drives := []string{"D:", "E:", "F:"}
	for _, drive := range drives {
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Steam", "config", "loginusers.vdf"))
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Program Files (x86)", "Steam", "config", "loginusers.vdf"))
		possiblePaths = append(possiblePaths, filepath.Join(drive, "Program Files", "Steam", "config", "loginusers.vdf"))
	}

	var vdfPath string
	for _, path := range possiblePaths {
		if _, err := os.Stat(path); err == nil {
			vdfPath = path
			break
		}
	}

	if vdfPath == "" {
		ui.Log("Файл loginusers.vdf не найден", false)
		ui.Pause()
		return
	}

	ui.Log(fmt.Sprintf("Найден файл: %s", vdfPath), true)
	fmt.Println()

	ParseSteamAccountsFromPath(vdfPath)

	ui.Pause()
}
