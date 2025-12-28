package keywords

import "strings"

// Ключевые слова для поиска
var Keywords = []string{
	"undead", "melony", "fecurity", "ancient", "hack", "cheat", "чит",
	"софт", "loader", "inject",
	"bypass", "overlay", "esp", "speedhack",
	"лоадер",
	"hwid", "medusa", "mason", "mas", "smg",
	"midnight", "fatality", "memesense",
}

// ContainsKeyword проверяет, содержит ли строка ключевое слово
func ContainsKeyword(name string) bool {
	nameLower := strings.ToLower(name)
	for _, keyword := range Keywords {
		if strings.Contains(nameLower, strings.ToLower(keyword)) {
			return true
		}
	}
	return false
}

// GetKeywordsString возвращает все ключевые слова в виде строки
func GetKeywordsString() string {
	return strings.Join(Keywords, " ")
}
