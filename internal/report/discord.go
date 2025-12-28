package report

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"mime/multipart"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"time"

	"manual-cobra/internal/ui"
)

// SendDiscordReport отправляет отчет в Discord
func SendDiscordReport() {
	ui.PrintHeader()
	fmt.Printf("\n%s═══ ОТПРАВКА ОТЧЕТА В DISCORD ═══%s\n\n", ui.ColorCyan+ui.ColorBold, ui.ColorReset)

	ui.Log("Подготовка отчета...", true)

	logFilePath, err := CreateLogFile()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания лог-файла: %v", err), false)
	} else {
		ui.Log(fmt.Sprintf("Лог-файл создан: %s", logFilePath), true)
	}

	hostname, _ := os.Hostname()
	username := os.Getenv("USERNAME")

	Results.Timestamp = time.Now().Format("02.01.2006 15:04:05")
	Results.Username = username
	Results.ComputerName = hostname

	embedColor := 16711680 // Красный
	totalFindings := len(Results.AppDataFiles) + len(Results.SystemFiles) +
		len(Results.PrefetchFiles) + len(Results.RegistryFindings) + len(Results.BrowserHistory)

	statusEmoji := "⚠️"
	statusText := fmt.Sprintf("Обнаружено подозрительных элементов: %d", totalFindings)

	if totalFindings == 0 {
		embedColor = 65280 // Зеленый
		statusEmoji = "✅"
		statusText = "Система чиста"
	}

	var embedDesc strings.Builder
	embedDesc.WriteString(fmt.Sprintf("**Дата:** %s\n", Results.Timestamp))
	embedDesc.WriteString(fmt.Sprintf("**Пользователь:** `%s`\n", Results.Username))
	embedDesc.WriteString(fmt.Sprintf("**Компьютер:** `%s`\n\n", Results.ComputerName))

	embedDesc.WriteString("**📊 ИТОГИ СКАНИРОВАНИЯ**\n")
	embedDesc.WriteString("```\n")
	embedDesc.WriteString(fmt.Sprintf("📁 AppData:        %d файлов\n", len(Results.AppDataFiles)))
	embedDesc.WriteString(fmt.Sprintf("💻 Системные:      %d файлов\n", len(Results.SystemFiles)))
	embedDesc.WriteString(fmt.Sprintf("📋 Prefetch:       %d файлов\n", len(Results.PrefetchFiles)))
	embedDesc.WriteString(fmt.Sprintf("📝 Реестр:         %d записей\n", len(Results.RegistryFindings)))
	embedDesc.WriteString(fmt.Sprintf("🌐 Браузеры:       %d записей\n", len(Results.BrowserHistory)))
	embedDesc.WriteString(fmt.Sprintf("🎮 Steam:          %d аккаунтов\n", len(Results.SteamAccounts)))
	embedDesc.WriteString("```\n")

	embedDesc.WriteString(fmt.Sprintf("\n%s **%s**\n\n", statusEmoji, statusText))
	embedDesc.WriteString("📎 **Полный отчет во вложенном файле**")

	embeds := []map[string]interface{}{
		{
			"title":       "🛡️ CUSTOSAC ANTI-CHEAT CHECKER",
			"description": embedDesc.String(),
			"color":       embedColor,
			"timestamp":   time.Now().Format(time.RFC3339),
			"footer": map[string]interface{}{
				"text": "CUSTOSAC • Отчет о проверке системы",
			},
		},
	}

	fileData, err := os.ReadFile(logFilePath)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка чтения лог-файла: %v", err), false)
		ui.Pause()
		return
	}

	var requestBody bytes.Buffer
	multipartWriter := multipart.NewWriter(&requestBody)

	payloadJSON, err := json.Marshal(map[string]interface{}{
		"embeds": embeds,
	})
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания JSON: %v", err), false)
		ui.Pause()
		return
	}

	payloadPart, err := multipartWriter.CreateFormField("payload_json")
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания payload: %v", err), false)
		ui.Pause()
		return
	}
	_, err = payloadPart.Write(payloadJSON)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка записи payload: %v", err), false)
		ui.Pause()
		return
	}

	filePart, err := multipartWriter.CreateFormFile("files[0]", filepath.Base(logFilePath))
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка создания file part: %v", err), false)
		ui.Pause()
		return
	}
	_, err = filePart.Write(fileData)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка записи файла: %v", err), false)
		ui.Pause()
		return
	}

	err = multipartWriter.Close()
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка закрытия multipart: %v", err), false)
		ui.Pause()
		return
	}

	ui.Log("Отправка в Discord...", true)

	// Создаем HTTP клиент с таймаутом
	client := &http.Client{
		Timeout: 30 * time.Second,
	}

	resp, err := client.Post(DiscordWebhook, multipartWriter.FormDataContentType(), &requestBody)
	if err != nil {
		ui.Log(fmt.Sprintf("Ошибка отправки: %v", err), false)
		ui.Pause()
		return
	}
	defer func() {
		if closeErr := resp.Body.Close(); closeErr != nil {
			ui.Log(fmt.Sprintf("Предупреждение: ошибка закрытия ответа: %v", closeErr), false)
		}
	}()

	if resp.StatusCode >= 200 && resp.StatusCode < 300 {
		ui.Log("✓ Отчет успешно отправлен в Discord!", true)
		ui.Log(fmt.Sprintf("✓ Файл отправлен: %s", filepath.Base(logFilePath)), true)

		fmt.Println()
		ui.Log("Удаление отчета и папки...", true)

		err := os.Remove(logFilePath)
		if err != nil {
			ui.Log(fmt.Sprintf("Ошибка удаления файла: %v", err), false)
		} else {
			ui.Log(fmt.Sprintf("✓ Файл удален: %s", filepath.Base(logFilePath)), true)
		}

		logDir := filepath.Join(os.Getenv("USERPROFILE"), "Desktop", "CustosAC_Logs")
		err = os.Remove(logDir)
		if err != nil {
			ui.Log("Папка CustosAC_Logs не удалена (возможно не пуста)", false)
		} else {
			ui.Log("✓ Папка CustosAC_Logs удалена", true)
		}
	} else {
		body, _ := io.ReadAll(resp.Body)
		ui.Log(fmt.Sprintf("Ошибка Discord API (код %d): %s", resp.StatusCode, string(body)), false)
	}

	ui.Pause()
}
