package winapi

import (
	"os"
	"syscall"
	"unsafe"
)

const (
	// SW_NORMAL - показать окно в нормальном состоянии
	swNormal = 1
	// shellExecuteSuccess - минимальное значение для успешного ShellExecute
	shellExecuteSuccess = 32
)

var (
	shell32          = syscall.NewLazyDLL("shell32.dll")
	procShellExecute = shell32.NewProc("ShellExecuteW")
)

// IsAdmin проверяет, запущена ли программа с правами администратора
func IsAdmin() bool {
	f, err := os.Open("\\\\.\\PHYSICALDRIVE0")
	if err != nil {
		return false
	}
	f.Close()
	return true
}

// RunAsAdmin перезапускает программу с правами администратора
func RunAsAdmin() {
	if IsAdmin() {
		return
	}

	verb := "runas"
	exe, _ := os.Executable()
	cwd, _ := os.Getwd()

	verbPtr, _ := syscall.UTF16PtrFromString(verb)
	exePtr, _ := syscall.UTF16PtrFromString(exe)
	cwdPtr, _ := syscall.UTF16PtrFromString(cwd)

	ret, _, _ := procShellExecute.Call(
		0,
		uintptr(unsafe.Pointer(verbPtr)),
		uintptr(unsafe.Pointer(exePtr)),
		0,
		uintptr(unsafe.Pointer(cwdPtr)),
		uintptr(swNormal),
	)

	if ret > shellExecuteSuccess {
		os.Exit(0)
	}
}
