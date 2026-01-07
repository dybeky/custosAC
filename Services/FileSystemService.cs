using CustosAC.Abstractions;

namespace CustosAC.Services;

/// <summary>
/// Реализация сервиса файловой системы
/// </summary>
public class FileSystemService : IFileSystemService
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public IEnumerable<string> EnumerateFileSystemEntries(string path)
    {
        return Directory.EnumerateFileSystemEntries(path);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*")
    {
        return Directory.EnumerateFiles(path, searchPattern);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return Directory.EnumerateDirectories(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public string GetExtension(string path)
    {
        return Path.GetExtension(path);
    }

    public string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path) ?? string.Empty;
    }

    public bool IsDirectory(string path)
    {
        return Directory.Exists(path);
    }

    public FileInfo GetFileInfo(string path)
    {
        return new FileInfo(path);
    }
}
