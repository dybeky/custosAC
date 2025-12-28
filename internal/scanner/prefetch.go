package scanner

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"manual-cobra/internal/report"
	"manual-cobra/internal/ui"
	"manual-cobra/pkg/keywords"
)

// ScanPrefetch автоматическое сканирование Prefetch
func ScanPrefetch() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ PREFETCH ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	prefetchPath := "C:\\Windows\\Prefetch"

	if _, err := os.Stat(prefetchPath); os.IsNotExist(err) {
		ui.Log("Папка Prefetch не найдена или недоступна", false)
		ui.Pause()
		return
	}

	ui.Log("Начинается сканирование Prefetch...", true)
	fmt.Println()

	files, err := os.ReadDir(prefetchPath)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка чтения Prefetch: %v", err), false)
		ui.Pause()
		return
	}

	suspiciousFiles := []string{}

	for _, file := range files {
		if file.IsDir() {
			continue
		}

		fileName := file.Name()
		if !strings.HasSuffix(strings.ToLower(fileName), ".pf") {
			continue
		}

		if keywords.ContainsKeyword(fileName) {
			fullPath := filepath.Join(prefetchPath, fileName)
			suspiciousFiles = append(suspiciousFiles, fullPath)

			info, err := file.Info()
			if err == nil {
				fmt.Printf("%s►%s %s\n", ui.ColorRed+ui.ColorBold, ui.ColorReset, fileName)
				fmt.Printf("   Последнее изменение: %s\n\n", info.ModTime().Format("02.01.2006 15:04:05"))
			}
		}
	}

	report.Results.PrefetchFiles = suspiciousFiles

	fmt.Println()
	if len(suspiciousFiles) > 0 {
		ui.Log(fmt.Sprintf("Найдено подозрительных .pf файлов: %d", len(suspiciousFiles)), false)
		fmt.Printf("\n%s[V]%s - Просмотреть все файлы постранично\n", ui.ColorGreen, ui.ColorReset)
		fmt.Printf("%s[0]%s - Продолжить\n", ui.ColorCyan, ui.ColorReset)
		fmt.Printf("\n%s►%s Выберите действие: ", ui.ColorGreen+ui.ColorBold, ui.ColorReset)

		var choice string
		fmt.Scanln(&choice)

		if strings.ToLower(strings.TrimSpace(choice)) == "v" {
			ui.DisplayFilesWithPagination(suspiciousFiles, 25)
		}
	} else {
		ui.Log("Подозрительных .pf файлов не найдено", true)
		ui.Pause()
	}
}
