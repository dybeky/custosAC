package ui

import (
	"os"
	"os/exec"
	"syscall"
	"unsafe"
)

var (
	kernel32           = syscall.NewLazyDLL("kernel32.dll")
	user32             = syscall.NewLazyDLL("user32.dll")
	procSetConsoleMode = kernel32.NewProc("SetConsoleMode")
	procGetConsoleMode = kernel32.NewProc("GetConsoleMode")
	procGetSystemMenu  = user32.NewProc("GetSystemMenu")
	procGetConsoleWindow = kernel32.NewProc("GetConsoleWindow")
)

// Цвета для консоли
const (
	ColorReset   = "\033[0m"
	ColorRed     = "\033[31m"
	ColorGreen   = "\033[32m"
	ColorYellow  = "\033[33m"
	ColorBlue    = "\033[34m"
	ColorCyan    = "\033[36m"
	ColorWhite   = "\033[37m"
	ColorBold    = "\033[1m"
	ColorMagenta = "\033[35m"

	// ENABLE_VIRTUAL_TERMINAL_PROCESSING - включает обработку ANSI escape последовательностей
	enableVirtualTerminalProcessing = 0x0004
)

// SetupConsole настраивает консоль (размер и цвета)
func SetupConsole() {
	// Устанавливаем увеличенный размер окна и большой буфер для прокрутки
	cmd := exec.Command("cmd", "/c", "mode", "con:", "cols=140", "lines=9999")
	cmd.Run()

	// Настраиваем режим консоли для поддержки ANSI цветов
	handle := uintptr(syscall.Stdout)
	var mode uint32
	procGetConsoleMode.Call(handle, uintptr(unsafe.Pointer(&mode)))
	mode |= enableVirtualTerminalProcessing
	procSetConsoleMode.Call(handle, uintptr(mode))
}

// ClearScreen очищает экран консоли
func ClearScreen() {
	cmd := exec.Command("cmd", "/c", "cls")
	cmd.Stdout = os.Stdout
	cmd.Run()
}
