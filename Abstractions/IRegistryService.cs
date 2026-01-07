namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для работы с реестром Windows
/// </summary>
public interface IRegistryService
{
    /// <summary>Экспортировать ключ реестра в файл</summary>
    Task<bool> ExportKeyAsync(string keyPath, string outputFile, CancellationToken ct = default);

    /// <summary>Удалить значение реестра</summary>
    Task<bool> DeleteValueAsync(string keyPath, string valueName, CancellationToken ct = default);

    /// <summary>Удалить ключ реестра</summary>
    Task<bool> DeleteKeyAsync(string keyPath, CancellationToken ct = default);

    /// <summary>Открыть редактор реестра с указанным путём</summary>
    Task OpenRegistryEditorAsync(string keyPath);

    /// <summary>Проверить существование ключа</summary>
    Task<bool> KeyExistsAsync(string keyPath, CancellationToken ct = default);
}
