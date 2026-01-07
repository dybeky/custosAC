namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для работы с файловой системой
/// </summary>
public interface IFileSystemService
{
    /// <summary>Проверить существование директории</summary>
    bool DirectoryExists(string path);

    /// <summary>Проверить существование файла</summary>
    bool FileExists(string path);

    /// <summary>Перечислить элементы файловой системы</summary>
    IEnumerable<string> EnumerateFileSystemEntries(string path);

    /// <summary>Перечислить файлы</summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*");

    /// <summary>Перечислить директории</summary>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <summary>Прочитать весь текст из файла</summary>
    string ReadAllText(string path);

    /// <summary>Прочитать все строки из файла</summary>
    string[] ReadAllLines(string path);

    /// <summary>Получить имя файла из пути</summary>
    string GetFileName(string path);

    /// <summary>Получить расширение файла</summary>
    string GetExtension(string path);

    /// <summary>Получить имя директории</summary>
    string GetDirectoryName(string path);

    /// <summary>Проверить, является ли путь директорией</summary>
    bool IsDirectory(string path);

    /// <summary>Получить информацию о файле</summary>
    FileInfo GetFileInfo(string path);

    /// <summary>Записать текст в файл</summary>
    void WriteAllText(string path, string contents);

    /// <summary>Удалить файл</summary>
    void DeleteFile(string path);
}
