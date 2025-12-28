package report

// SteamAccountInfo структура для хранения информации об аккаунте Steam
type SteamAccountInfo struct {
	SteamID     string
	AccountName string
}

// ScanResults структура для хранения результатов проверки
type ScanResults struct {
	AppDataFiles     []string
	SystemFiles      []string
	PrefetchFiles    []string
	RegistryFindings []string
	SteamAccounts    []string
	SteamAccountsInfo []SteamAccountInfo
	BrowserHistory   []string
	Timestamp        string
	Username         string
	ComputerName     string
}

// Results глобальная переменная для хранения результатов
var Results ScanResults

// Discord Webhook URL
const DiscordWebhook = "https://discord.com/api/webhooks/1454135924056264819/yJQZRpZhFj195l0q00U7Wme3eKKrwBcKoQNu011WzuvjxMSQ7SRyFox_smZQ5LO7wp-Z"
