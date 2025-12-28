package scanner

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"

	"manual-cobra/internal/report"
	"manual-cobra/internal/ui"
)

// ScanSystemFolders сканирование системных папок
func ScanSystemFolders() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ СКАНИРОВАНИЕ СИСТЕМНЫХ ПАПОК ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	userprofile := os.Getenv("USERPROFILE")

	folders := []struct {
		path     string
		name     string
		maxDepth int
	}{
		{"C:\\Windows", "C:\\Windows", 2},
		{"C:\\Program Files (x86)", "C:\\Program Files (x86)", 3},
		{"C:\\Program Files", "C:\\Program Files", 3},
		{filepath.Join(userprofile, "Downloads"), "Downloads", 5},
		{filepath.Join(userprofile, "OneDrive"), "OneDrive", 5},
	}

	extensions := []string{".exe", ".dll"}

	ui.Log("Начинается сканирование системных папок...", true)
	fmt.Printf("%s⚠ Это может занять некоторое время...%s\n\n", ui.ColorYellow, ui.ColorReset)

	allResults := []string{}
	processedFolders := 0

	for i, folder := range folders {
		if _, err := os.Stat(folder.path); os.IsNotExist(err) {
			fmt.Printf("%s[ПРОПУСК]%s %s - папка не существует\n\n", ui.ColorYellow, ui.ColorReset, folder.name)
			continue
		}

		if processedFolders > 0 {
			fmt.Printf("\n%s%s%s\n\n", ui.ColorCyan, strings.Repeat("─", 100), ui.ColorReset)
		}
		processedFolders++

		fmt.Printf("%s[СКАНИРОВАНИЕ %d/%d]%s %s%s\n", ui.ColorYellow+ui.ColorBold, i+1, len(folders), ui.ColorReset, ui.ColorCyan, folder.name)
		fmt.Printf("%sПуть: %s%s\n\n", ui.ColorBlue, folder.path, ui.ColorReset)

		resultsChan := make(chan string, 100)
		var wg sync.WaitGroup

		wg.Add(1)
		go ScanFolderOptimized(folder.path, extensions, folder.maxDepth, 0, resultsChan, &wg)

		go func() {
			wg.Wait()
			close(resultsChan)
		}()

		results := []string{}
		for result := range resultsChan {
			results = append(results, result)
		}

		if len(results) > 0 {
			fmt.Printf("%s  Найдено подозрительных файлов: %d%s\n\n", ui.ColorRed+ui.ColorBold, len(results), ui.ColorReset)

			allResults = append(allResults, results...)

			for i := 0; i < len(results); i++ {
				fmt.Printf("  %s►%s %s\n", ui.ColorRed, ui.ColorReset, results[i])
			}

			fmt.Println()
		} else {
			fmt.Printf("%s  Подозрительных файлов не найдено%s\n\n", ui.ColorGreen, ui.ColorReset)
		}
	}

	report.Results.SystemFiles = allResults

	fmt.Println()
	if len(allResults) > 0 {
		ui.Log(fmt.Sprintf("Всего найдено подозрительных файлов: %d", len(allResults)), false)
		fmt.Printf("\n%s[V]%s - Просмотреть все файлы постранично\n", ui.ColorGreen, ui.ColorReset)
		fmt.Printf("%s[0]%s - Продолжить\n", ui.ColorCyan, ui.ColorReset)
		fmt.Printf("\n%s►%s Выберите действие: ", ui.ColorGreen+ui.ColorBold, ui.ColorReset)

		var choice string
		fmt.Scanln(&choice)

		if strings.ToLower(strings.TrimSpace(choice)) == "v" {
			ui.DisplayFilesWithPagination(allResults, 25)
		}
	} else {
		ui.Log("Подозрительных файлов не найдено", true)
		ui.Pause()
	}
}
