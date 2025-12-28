package report

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// CreateLogFile создает красиво форматированный текстовый файл с логами
func CreateLogFile() (string, error) {
	hostname, _ := os.Hostname()
	username := os.Getenv("USERNAME")
	timestamp := time.Now().Format("02.01.2006 15:04:05")

	logDir := filepath.Join(os.Getenv("USERPROFILE"), "Desktop", "CustosAC_Logs")
	os.MkdirAll(logDir, 0755)

	fileName := fmt.Sprintf("CUSTOSAC_Report_%s.txt", time.Now().Format("2006-01-02_15-04-05"))
	filePath := filepath.Join(logDir, fileName)

	var logContent strings.Builder

	logContent.WriteString("╔═══════════════════════════════════════════════════════════════════════════╗\n")
	logContent.WriteString("║                     CUSTOSAC ANTI-CHEAT CHECKER                           ║\n")
	logContent.WriteString("║                         ОТЧЕТ О ПРОВЕРКЕ                                  ║\n")
	logContent.WriteString("╚═══════════════════════════════════════════════════════════════════════════╝\n\n")

	logContent.WriteString("┌─────────────────────────────────────────────────────────────────────────┐\n")
	logContent.WriteString("│ ИНФОРМАЦИЯ О СИСТЕМЕ                                                    │\n")
	logContent.WriteString("└─────────────────────────────────────────────────────────────────────────┘\n\n")
	logContent.WriteString(fmt.Sprintf("  Дата и время проверки:  %s\n", timestamp))
	logContent.WriteString(fmt.Sprintf("  Пользователь:           %s\n", username))
	logContent.WriteString(fmt.Sprintf("  Имя компьютера:         %s\n\n", hostname))

	// AppData файлы
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  📁 РЕЗУЛЬТАТЫ СКАНИРОВАНИЯ APPDATA\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.AppDataFiles) > 0 {
		logContent.WriteString(fmt.Sprintf("⚠ ОБНАРУЖЕНО: %d подозрительных файлов\n\n", len(Results.AppDataFiles)))
		for i, file := range Results.AppDataFiles {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, file))
		}
	} else {
		logContent.WriteString("✓ Подозрительных файлов не обнаружено\n")
	}
	logContent.WriteString("\n\n")

	// Системные файлы
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  💻 РЕЗУЛЬТАТЫ СКАНИРОВАНИЯ СИСТЕМНЫХ ПАПОК\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.SystemFiles) > 0 {
		logContent.WriteString(fmt.Sprintf("⚠ ОБНАРУЖЕНО: %d подозрительных файлов\n\n", len(Results.SystemFiles)))
		for i, file := range Results.SystemFiles {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, file))
		}
	} else {
		logContent.WriteString("✓ Подозрительных файлов не обнаружено\n")
	}
	logContent.WriteString("\n\n")

	// Prefetch файлы
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  📋 РЕЗУЛЬТАТЫ СКАНИРОВАНИЯ PREFETCH\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.PrefetchFiles) > 0 {
		logContent.WriteString(fmt.Sprintf("⚠ ОБНАРУЖЕНО: %d .pf файлов\n\n", len(Results.PrefetchFiles)))
		for i, file := range Results.PrefetchFiles {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, file))
		}
	} else {
		logContent.WriteString("✓ Подозрительных .pf файлов не обнаружено\n")
	}
	logContent.WriteString("\n\n")

	// Реестр
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  📝 РЕЗУЛЬТАТЫ ПОИСКА В РЕЕСТРЕ\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.RegistryFindings) > 0 {
		logContent.WriteString(fmt.Sprintf("⚠ ОБНАРУЖЕНО: %d записей с ключевыми словами\n\n", len(Results.RegistryFindings)))
		for i, finding := range Results.RegistryFindings {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, finding))
		}
	} else {
		logContent.WriteString("✓ Подозрительных записей не обнаружено\n")
	}
	logContent.WriteString("\n\n")

	// Steam аккаунты
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  🎮 НАЙДЕННЫЕ STEAM АККАУНТЫ\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.SteamAccounts) > 0 {
		logContent.WriteString(fmt.Sprintf("Обнаружено аккаунтов: %d\n\n", len(Results.SteamAccounts)))

		for i, acc := range Results.SteamAccounts {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, acc))
		}
	} else {
		logContent.WriteString("Аккаунты не найдены\n")
	}
	logContent.WriteString("\n\n")

	// История браузеров
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  🌐 РЕЗУЛЬТАТЫ СКАНИРОВАНИЯ ИСТОРИИ БРАУЗЕРОВ\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")
	if len(Results.BrowserHistory) > 0 {
		logContent.WriteString(fmt.Sprintf("⚠ ОБНАРУЖЕНО: %d подозрительных записей\n\n", len(Results.BrowserHistory)))
		for i, finding := range Results.BrowserHistory {
			logContent.WriteString(fmt.Sprintf("  [%d] %s\n", i+1, finding))
		}
	} else {
		logContent.WriteString("✓ Подозрительных записей не обнаружено\n")
	}
	logContent.WriteString("\n\n")

	// Итоги
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n")
	logContent.WriteString("  📊 ИТОГИ ПРОВЕРКИ\n")
	logContent.WriteString("═══════════════════════════════════════════════════════════════════════════\n\n")

	totalFindings := len(Results.AppDataFiles) + len(Results.SystemFiles) +
		len(Results.PrefetchFiles) + len(Results.RegistryFindings) + len(Results.BrowserHistory)

	logContent.WriteString(fmt.Sprintf("  Подозрительных файлов в AppData:      %d\n", len(Results.AppDataFiles)))
	logContent.WriteString(fmt.Sprintf("  Подозрительных файлов в системе:      %d\n", len(Results.SystemFiles)))
	logContent.WriteString(fmt.Sprintf("  Подозрительных .pf файлов:            %d\n", len(Results.PrefetchFiles)))
	logContent.WriteString(fmt.Sprintf("  Подозрительных записей в реестре:     %d\n", len(Results.RegistryFindings)))
	logContent.WriteString(fmt.Sprintf("  Подозрительных записей в браузерах:   %d\n", len(Results.BrowserHistory)))
	logContent.WriteString(fmt.Sprintf("  Steam аккаунтов:                      %d\n\n", len(Results.SteamAccounts)))

	if totalFindings == 0 {
		logContent.WriteString("  ✓ СТАТУС: СИСТЕМА ЧИСТА\n")
	} else {
		logContent.WriteString(fmt.Sprintf("  ⚠ СТАТУС: ОБНАРУЖЕНО ПОДОЗРИТЕЛЬНЫХ ЭЛЕМЕНТОВ: %d\n", totalFindings))
	}

	logContent.WriteString("\n╔═══════════════════════════════════════════════════════════════════════════╗\n")
	logContent.WriteString("║                    КОНЕЦ ОТЧЕТА                                           ║\n")
	logContent.WriteString("╚═══════════════════════════════════════════════════════════════════════════╝\n")

	err := os.WriteFile(filePath, []byte(logContent.String()), 0644)
	if err != nil {
		return "", err
	}

	return filePath, nil
}
