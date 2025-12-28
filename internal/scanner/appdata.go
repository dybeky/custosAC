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

// ScanAppData автоматическое сканирование AppData
func ScanAppData() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ APPDATA ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	appdata := os.Getenv("APPDATA")
	localappdata := os.Getenv("LOCALAPPDATA")
	userprofile := os.Getenv("USERPROFILE")
	locallow := filepath.Join(userprofile, "AppData", "LocalLow")

	folders := []struct {
		path string
		name string
	}{
		{appdata, "AppData\\Roaming"},
		{localappdata, "AppData\\Local"},
		{locallow, "AppData\\LocalLow"},
	}

	extensions := []string{".exe", ".dll"}

	ui.Log("Начинается сканирование AppData...", true)
	fmt.Println()

	allResults := []string{}

	for i, folder := range folders {
		if i > 0 {
			fmt.Printf("\n%s%s%s\n\n", ui.ColorCyan, strings.Repeat("─", 100), ui.ColorReset)
		}

		fmt.Printf("%s[СКАНИРОВАНИЕ %d/%d]%s %s%s\n", ui.ColorYellow+ui.ColorBold, i+1, len(folders), ui.ColorReset, ui.ColorCyan, folder.name)
		fmt.Printf("%sПуть: %s%s\n\n", ui.ColorBlue, folder.path, ui.ColorReset)

		resultsChan := make(chan string, 100)
		var wg sync.WaitGroup

		wg.Add(1)
		go ScanFolderOptimized(folder.path, extensions, 10, 0, resultsChan, &wg)

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

			for j := 0; j < len(results); j++ {
				fmt.Printf("  %s►%s %s\n", ui.ColorRed, ui.ColorReset, results[j])
			}

			fmt.Println()
		} else {
			fmt.Printf("%s  Подозрительных файлов не найдено%s\n\n", ui.ColorGreen, ui.ColorReset)
		}
	}

	report.Results.AppDataFiles = allResults

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
