package scanner

import (
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"manual-cobra/internal/report"
	"manual-cobra/internal/ui"
	"manual-cobra/pkg/keywords"
)

// SearchRegistry поиск по ключевым словам в реестре
func SearchRegistry() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ПОИСК В РЕЕСТРЕ ПО КЛЮЧЕВЫМ СЛОВАМ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	registryKeys := []struct {
		path string
		name string
	}{
		{`HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache`, "MuiCache"},
		{`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched`, "AppSwitched"},
		{`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView`, "ShowJumpView"},
	}

	ui.Log("Начинается поиск в реестре...", true)
	fmt.Printf("%s⚠ Для поиска используется временный экспорт%s\n\n", ui.ColorYellow, ui.ColorReset)

	allFindings := []string{}
	tempDir := filepath.Join(os.TempDir(), "custosAC_temp")
	os.MkdirAll(tempDir, 0755)
	defer os.RemoveAll(tempDir)

	processedKeys := 0

	for i, regKey := range registryKeys {
		if processedKeys > 0 {
			fmt.Printf("%s%s%s\n", ui.ColorCyan, strings.Repeat("─", 80), ui.ColorReset)
		}

		fmt.Printf("\n%s[СКАНИРОВАНИЕ %d/%d]%s %s\n", ui.ColorYellow, i+1, len(registryKeys), ui.ColorReset, regKey.name)
		fmt.Printf("%sПуть: %s%s\n\n", ui.ColorBlue, regKey.path, ui.ColorReset)

		outputFile := filepath.Join(tempDir, regKey.name+".reg")

		cmd := exec.Command("reg", "export", regKey.path, outputFile, "/y")
		var stderr bytes.Buffer
		cmd.Stderr = &stderr
		err := cmd.Run()

		if err != nil {
			fmt.Printf("%s✗%s Ключ не существует или недоступен: %s\n", ui.ColorYellow, ui.ColorReset, regKey.name)
			fmt.Printf("   %s\n\n", strings.TrimSpace(stderr.String()))
			continue
		}

		processedKeys++

		if _, err := os.Stat(outputFile); os.IsNotExist(err) {
			fmt.Printf("%s✗%s Не удалось создать файл экспорта для %s\n\n", ui.ColorRed, ui.ColorReset, regKey.name)
			continue
		}

		data, err := os.ReadFile(outputFile)
		if err != nil {
			fmt.Printf("%s✗%s Ошибка чтения %s: %v\n\n", ui.ColorRed, ui.ColorReset, regKey.name, err)
			continue
		}

		content := string(data)
		lines := strings.Split(content, "\n")

		findings := []string{}
		for _, line := range lines {
			trimmedLine := strings.TrimSpace(line)
			if trimmedLine == "" || strings.HasPrefix(trimmedLine, "Windows Registry") || strings.HasPrefix(trimmedLine, "[HKEY") {
				continue
			}

			if keywords.ContainsKeyword(line) {
				findings = append(findings, trimmedLine)
			}
		}

		if len(findings) > 0 {
			fmt.Printf("%s  Найдено записей с ключевыми словами: %d%s\n\n", ui.ColorRed+ui.ColorBold, len(findings), ui.ColorReset)

			for _, finding := range findings {
				allFindings = append(allFindings, fmt.Sprintf("[%s] %s", regKey.name, finding))
			}

			for j := 0; j < len(findings); j++ {
				fmt.Printf("  %s►%s %s\n", ui.ColorRed, ui.ColorReset, findings[j])
			}

			fmt.Println()
		} else {
			fmt.Printf("%s  Подозрительных записей не найдено%s\n\n", ui.ColorGreen, ui.ColorReset)
		}
	}

	report.Results.RegistryFindings = allFindings

	fmt.Println()
	if len(allFindings) > 0 {
		ui.Log(fmt.Sprintf("Всего найдено подозрительных записей: %d", len(allFindings)), false)
		fmt.Printf("\n%s[V]%s - Просмотреть все записи постранично\n", ui.ColorGreen, ui.ColorReset)
		fmt.Printf("%s[0]%s - Продолжить\n", ui.ColorCyan, ui.ColorReset)
		fmt.Printf("\n%s►%s Выберите действие: ", ui.ColorGreen+ui.ColorBold, ui.ColorReset)

		var choice string
		fmt.Scanln(&choice)

		if strings.ToLower(strings.TrimSpace(choice)) == "v" {
			ui.DisplayFilesWithPagination(allFindings, 25)
		}
	} else {
		ui.Log("Подозрительных записей в реестре не найдено", true)
		ui.Pause()
	}
}
