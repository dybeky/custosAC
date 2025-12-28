package ui

import (
	"bufio"
	"fmt"
	"os"
	"strconv"
	"strings"
)

// GetChoice получает выбор пользователя
func GetChoice(maxOpt int) int {
	scanner := bufio.NewScanner(os.Stdin)
	for {
		fmt.Printf("\n%s►%s Выберите опцию [0-%d]: ", ColorGreen+ColorBold, ColorReset, maxOpt)

		if !scanner.Scan() {
			// Если ошибка чтения, возвращаем 0 (выход/назад)
			return 0
		}

		input := strings.TrimSpace(scanner.Text())
		choice, err := strconv.Atoi(input)

		if err == nil && choice >= 0 && choice <= maxOpt {
			return choice
		}

		fmt.Printf("\n%s⚠ Ошибка: Введите число от 0 до %d%s\n", ColorRed+ColorBold, maxOpt, ColorReset)
	}
}
