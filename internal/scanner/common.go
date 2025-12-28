package scanner

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"

	"manual-cobra/internal/ui"
	"manual-cobra/internal/winapi"
	"manual-cobra/pkg/keywords"
)

// OpenFolder открывает папку в проводнике
func OpenFolder(path, desc string) bool {
	if _, err := os.Stat(path); os.IsNotExist(err) {
		ui.Log(fmt.Sprintf("Папка не найдена: %s", path), false)
		return false
	}

	cmd := exec.Command("explorer", path)
	err := cmd.Start()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка: %v", err), false)
		return false
	}

	winapi.TrackProcess(cmd)
	go func() {
		cmd.Wait()
		winapi.UntrackProcess(cmd)
	}()

	ui.Log(fmt.Sprintf("%s: %s", desc, path), true)
	return true
}

// RunCommand выполняет команду
func RunCommand(command, desc string) bool {
	cmd := exec.Command("cmd", "/c", "start", command)
	err := cmd.Start()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка: %v", err), false)
		return false
	}

	winapi.TrackProcess(cmd)
	go func() {
		cmd.Wait()
		winapi.UntrackProcess(cmd)
	}()

	ui.Log(desc, true)
	return true
}

// CopyKeywordsToClipboard копирует ключевые слова в буфер обмена
func CopyKeywordsToClipboard() {
	keywordsStr := keywords.GetKeywordsString()

	// Используем stdin pipe вместо echo для безопасности
	cmd := exec.Command("clip")
	stdin, err := cmd.StdinPipe()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания pipe: %v", err), false)
		return
	}

	if err := cmd.Start(); err != nil {
		ui.Log(fmt.Sprintf("Ошибка запуска clip: %v", err), false)
		return
	}

	_, err = stdin.Write([]byte(keywordsStr))
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка записи: %v", err), false)
		stdin.Close()
		cmd.Wait()
		return
	}

	stdin.Close()
	err = cmd.Wait()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка копирования: %v", err), false)
		return
	}

	ui.Log("Ключевые слова скопированы в буфер обмена!", true)
	fmt.Printf("\n%sТеперь можно вставить (Ctrl+V) в Everything, LastActivityView и другие программы%s\n", ui.ColorYellow, ui.ColorReset)
}

// CheckWebsites открывает сайты oplata.info и funpay.com для проверки доступности
func CheckWebsites() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ПРОВЕРКА САЙТОВ ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	websites := []struct {
		url  string
		name string
	}{
		{"https://oplata.info", "Oplata.info"},
		{"https://funpay.com", "FunPay.com"},
	}

	fmt.Printf("  %s[i]%s Открываем сайты для проверки доступности...%s\n\n", ui.ColorBlue, ui.ColorReset, ui.ColorReset)

	for _, site := range websites {
		if RunCommand(site.url, site.name) {
			ui.Log(fmt.Sprintf("✓ Открыт: %s", site.name), true)
		} else {
			ui.Log(fmt.Sprintf("✗ Ошибка открытия: %s", site.name), false)
		}
	}

	fmt.Printf("\n%sЧТО ПРОВЕРИТЬ:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s Доступность сайтов (открываются ли страницы)\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Нет ли редиректов на подозрительные домены\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Корректность отображения сайтов\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Нет ли предупреждений браузера\n", ui.ColorRed, ui.ColorReset)

	ui.Pause()
}

// OpenRegistry открывает regedit с копированием пути в буфер обмена
func OpenRegistry(path string) bool {
	// Используем stdin pipe вместо echo для безопасности
	cmd := exec.Command("clip")
	stdin, err := cmd.StdinPipe()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания pipe: %v", err), false)
		return false
	}

	if err := cmd.Start(); err != nil {
		ui.Log(fmt.Sprintf("Ошибка запуска clip: %v", err), false)
		return false
	}

	_, err = stdin.Write([]byte(path))
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка записи: %v", err), false)
		stdin.Close()
		cmd.Wait()
		return false
	}

	stdin.Close()
	err = cmd.Wait()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка копирования: %v", err), false)
		return false
	}

	cmdReg := exec.Command("regedit.exe")
	err = cmdReg.Start()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка: %v", err), false)
		return false
	}

	winapi.TrackProcess(cmdReg)
	go func() {
		cmdReg.Wait()
		winapi.UntrackProcess(cmdReg)
	}()

	ui.Log(fmt.Sprintf("Путь скопирован: %s", path), true)
	fmt.Printf("%sВставьте путь в regedit (Ctrl+V)%s\n", ui.ColorYellow, ui.ColorReset)
	return true
}

// CheckTelegram проверяет Telegram ботов и папку загрузок
func CheckTelegram() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ПРОВЕРКА TELEGRAM ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	// Список ботов для проверки
	bots := []struct {
		username string
		name     string
	}{
		{"@MelonySolutionBot", "Melony Solution Bot"},
		{"@UndeadSellerBot", "Undead Seller Bot"},
	}

	fmt.Printf("  %s[i]%s Открываем Telegram ботов для проверки...%s\n\n", ui.ColorBlue, ui.ColorReset, ui.ColorReset)

	// Открываем ботов в Telegram
	for _, bot := range bots {
		telegramURL := fmt.Sprintf("tg://resolve?domain=%s", strings.TrimPrefix(bot.username, "@"))
		if RunCommand(telegramURL, bot.name) {
			ui.Log(fmt.Sprintf("✓ Открыт: %s (%s)", bot.name, bot.username), true)
		} else {
			ui.Log(fmt.Sprintf("✗ Ошибка открытия: %s", bot.name), false)
		}
	}

	fmt.Printf("\n%s%s%s\n", ui.ColorCyan, "─────────────────────────────────────────", ui.ColorReset)

	// Поиск папки загрузок Telegram
	fmt.Printf("\n  %s[i]%s Поиск папки загрузок Telegram...%s\n\n", ui.ColorBlue, ui.ColorReset, ui.ColorReset)

	userprofile := os.Getenv("USERPROFILE")
	possiblePaths := []string{
		filepath.Join(userprofile, "Downloads", "Telegram Desktop"),
		filepath.Join(userprofile, "Downloads"),
		filepath.Join(userprofile, "Documents", "Telegram Desktop"),
		filepath.Join(userprofile, "OneDrive", "Downloads", "Telegram Desktop"),
	}

	// Также проверяем AppData для настроек Telegram
	appdata := os.Getenv("APPDATA")
	telegramDataPath := filepath.Join(appdata, "Telegram Desktop")
	if _, err := os.Stat(telegramDataPath); err == nil {
		ui.Log(fmt.Sprintf("✓ Найдена папка данных Telegram: %s", telegramDataPath), true)

		// Пытаемся найти папку загрузок из конфигурации
		possiblePaths = append([]string{
			filepath.Join(telegramDataPath, "tdata", "user_data"),
		}, possiblePaths...)
	}

	foundDownloads := false
	for _, downloadPath := range possiblePaths {
		if _, err := os.Stat(downloadPath); err == nil {
			foundDownloads = true
			ui.Log(fmt.Sprintf("✓ Найдена папка загрузок: %s", downloadPath), true)

			// Открываем папку
			OpenFolder(downloadPath, "Папка загрузок Telegram")
			break
		}
	}

	if !foundDownloads {
		ui.Log("✗ Папка загрузок Telegram не найдена", false)
		fmt.Printf("\n%s⚠ Возможные причины:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
		fmt.Printf("  %s►%s Telegram не установлен\n", ui.ColorYellow, ui.ColorReset)
		fmt.Printf("  %s►%s Папка загрузок находится в другом месте\n", ui.ColorYellow, ui.ColorReset)
		fmt.Printf("  %s►%s Файлы не загружались через Telegram\n", ui.ColorYellow, ui.ColorReset)
	}

	fmt.Printf("\n%sЧТО ПРОВЕРИТЬ В TELEGRAM:%s\n", ui.ColorYellow+ui.ColorBold, ui.ColorReset)
	fmt.Printf("  %s►%s Историю переписки с ботами @MelonySolutionBot и @UndeadSellerBot\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Загруженные файлы (.exe, .dll, .bat, .zip)\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Подозрительные архивы и установщики\n", ui.ColorRed, ui.ColorReset)
	fmt.Printf("  %s►%s Переданные платежи или транзакции\n", ui.ColorRed, ui.ColorReset)

	ui.Pause()
}

const (
	// maxWorkers - максимальное количество одновременных горутин для сканирования
	maxWorkers = 50
)

type scanTask struct {
	path         string
	currentDepth int
}

// ScanFolderOptimized оптимизированное сканирование папки с worker pool
func ScanFolderOptimized(path string, extensions []string, maxDepth int, currentDepth int, resultsChan chan<- string, wg *sync.WaitGroup) {
	defer wg.Done()

	if currentDepth > maxDepth {
		return
	}

	excludeDirs := map[string]bool{
		"windows.old":               true,
		"$recycle.bin":              true,
		"system volume information": true,
		"recovery":                  true,
		"perflogs":                  true,
		"windowsapps":               true,
		"winsxs":                    true,
		".git":                      true,
		"node_modules":              true,
	}

	files, err := os.ReadDir(path)
	if err != nil {
		return
	}

	// Собираем директории для обработки
	var subDirs []string
	for _, file := range files {
		fileName := file.Name()
		fileNameLower := strings.ToLower(fileName)
		fullPath := filepath.Join(path, fileName)

		if file.IsDir() && excludeDirs[fileNameLower] {
			continue
		}

		if keywords.ContainsKeyword(fileName) {
			if file.IsDir() {
				resultsChan <- fullPath
			} else {
				if len(extensions) > 0 {
					ext := strings.ToLower(filepath.Ext(fullPath))
					for _, allowedExt := range extensions {
						if ext == allowedExt {
							resultsChan <- fullPath
							break
						}
					}
				} else {
					resultsChan <- fullPath
				}
			}
		}

		if file.IsDir() {
			subDirs = append(subDirs, fullPath)
		}
	}

	// Обрабатываем поддиректории через семафор для ограничения горутин
	semaphore := make(chan struct{}, maxWorkers)
	for _, subDir := range subDirs {
		semaphore <- struct{}{} // Захватываем слот
		wg.Add(1)
		go func(dir string) {
			defer func() { <-semaphore }() // Освобождаем слот
			ScanFolderOptimized(dir, extensions, maxDepth, currentDepth+1, resultsChan, wg)
		}(subDir)
	}
}
