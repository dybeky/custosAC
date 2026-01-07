namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для работы с административными привилегиями
/// </summary>
public interface IAdminService
{
    /// <summary>Проверить наличие прав администратора</summary>
    bool IsAdmin();

    /// <summary>Перезапустить с правами администратора если нужно</summary>
    void RunAsAdmin();

    /// <summary>Установить обработчик закрытия приложения</summary>
    void SetupCloseHandler(Action cleanupAction);
}
