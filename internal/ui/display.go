package ui

import (
	"fmt"
	"strings"
	"time"
)

var isAdminGlobal bool

// SetAdminStatus устанавливает глобальный статус администратора
func SetAdminStatus(isAdmin bool) {
	isAdminGlobal = isAdmin
}

// PrintHeader выводит заголовок программы
func PrintHeader() {
	ClearScreen()
	fmt.Println(ColorCyan + ColorBold)
	fmt.Println()
	fmt.Println("                  █▀▀ █ █ █▀ ▀█▀ █▀█ █▀")
	fmt.Println("                  █▄▄ █▄█ ▄█  █  █▄█ ▄█")
	fmt.Println()
	fmt.Println("                   sdelano s lubovyu")
	fmt.Println()
	fmt.Println(ColorReset)

	if isAdminGlobal {
		fmt.Printf("  %s[✓]%s Статус: %sАдминистратор%s\n", ColorGreen, ColorReset, ColorGreen+ColorBold, ColorReset)
	} else {
		fmt.Printf("  %s[✗]%s Статус: %sОтсутствуют права администратора!%s\n", ColorRed, ColorReset, ColorRed+ColorBold, ColorReset)
	}
	fmt.Printf("  %s[i]%s Дата: %s\n", ColorBlue, ColorReset, time.Now().Format("02.01.2006 15:04:05"))
	fmt.Println()
}

// PrintMenu выводит меню с опциями
func PrintMenu(title string, options []string, showBack bool) {
	fmt.Printf("\n%s%s%s\n\n", ColorYellow+ColorBold, title, ColorReset)

	for i, opt := range options {
		fmt.Printf("  %s[%d]%s ➤ %s\n", ColorCyan+ColorBold, i+1, ColorReset, opt)
	}

	if showBack {
		fmt.Printf("\n  %s[0]%s ← %sНазад%s\n", ColorMagenta+ColorBold, ColorReset, ColorMagenta, ColorReset)
	} else {
		fmt.Printf("\n  %s[0]%s ✖ %sВыход%s\n", ColorRed+ColorBold, ColorReset, ColorRed, ColorReset)
	}
	fmt.Println()
}

// Log выводит сообщение с временной меткой
func Log(msg string, ok bool) {
	t := time.Now().Format("15:04:05")
	if ok {
		fmt.Printf("  %s[%s]%s %s✓%s %s\n", ColorBlue, t, ColorReset, ColorGreen+ColorBold, ColorReset, msg)
	} else {
		fmt.Printf("  %s[%s]%s %s✗%s %s\n", ColorBlue, t, ColorReset, ColorRed+ColorBold, ColorReset, msg)
	}
}

// Pause ожидает нажатия Enter
func Pause() {
	fmt.Printf("\n%s►%s Нажмите Enter для продолжения...\n", ColorGreen+ColorBold, ColorReset)
	fmt.Scanln()
}

// DisplayFilesWithPagination показывает файлы с постраничной навигацией
func DisplayFilesWithPagination(files []string, itemsPerPage int) {
	if len(files) == 0 {
		fmt.Printf("\n%s  Нет файлов для отображения%s\n", ColorYellow, ColorReset)
		return
	}

	totalPages := (len(files) + itemsPerPage - 1) / itemsPerPage
	currentPage := 0

	for {
		ClearScreen()

		fmt.Printf("\n%s═══ ПРОСМОТР ФАЙЛОВ (Страница %d из %d) ═══%s\n", ColorCyan+ColorBold, currentPage+1, totalPages, ColorReset)
		fmt.Printf("%sВсего файлов: %d%s\n\n", ColorYellow, len(files), ColorReset)

		start := currentPage * itemsPerPage
		end := start + itemsPerPage
		if end > len(files) {
			end = len(files)
		}

		for i := start; i < end; i++ {
			fmt.Printf("  %s[%d]%s %s\n", ColorCyan, i+1, ColorReset, files[i])
		}

		fmt.Printf("\n%s%s%s\n", ColorCyan, strings.Repeat("─", 100), ColorReset)
		fmt.Printf("\n%sНавигация:%s\n", ColorYellow+ColorBold, ColorReset)

		if currentPage > 0 {
			fmt.Printf("  %s[P]%s - Предыдущая страница\n", ColorGreen, ColorReset)
		}
		if currentPage < totalPages-1 {
			fmt.Printf("  %s[N]%s - Следующая страница\n", ColorGreen, ColorReset)
		}
		fmt.Printf("  %s[0]%s - Вернуться назад\n", ColorRed, ColorReset)

		fmt.Printf("\n%s►%s Выберите действие: ", ColorGreen+ColorBold, ColorReset)
		var input string
		fmt.Scanln(&input)
		input = strings.ToLower(strings.TrimSpace(input))

		switch input {
		case "n":
			if currentPage < totalPages-1 {
				currentPage++
			} else {
				fmt.Printf("%s⚠ Это последняя страница%s\n", ColorYellow, ColorReset)
				time.Sleep(500 * time.Millisecond)
			}
		case "p":
			if currentPage > 0 {
				currentPage--
			} else {
				fmt.Printf("%s⚠ Это первая страница%s\n", ColorYellow, ColorReset)
				time.Sleep(500 * time.Millisecond)
			}
		case "0", "":
			return
		default:
			fmt.Printf("%s⚠ Неверная команда%s\n", ColorRed, ColorReset)
			time.Sleep(500 * time.Millisecond)
		}
	}
}
