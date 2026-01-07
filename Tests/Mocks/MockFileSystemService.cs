using CustosAC.Abstractions;

namespace CustosAC.Tests.Mocks;

/// <summary>
/// Mock реализация IFileSystemService для тестирования
/// </summary>
public class MockFileSystemService : IFileSystemService
{
    private readonly HashSet<string> _existingDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _existingFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _directoryContents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _fileContents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string[]> _fileLines = new(StringComparer.OrdinalIgnoreCase);

    public MockFileSystemService AddDirectory(string path)
    {
        _existingDirectories.Add(path);
        return this;
    }

    public MockFileSystemService AddFile(string path, string content = "")
    {
        _existingFiles.Add(path);
        _fileContents[path] = content;
        return this;
    }

    public MockFileSystemService AddFileWithLines(string path, string[] lines)
    {
        _existingFiles.Add(path);
        _fileLines[path] = lines;
        _fileContents[path] = string.Join(Environment.NewLine, lines);
        return this;
    }

    public MockFileSystemService SetDirectoryContents(string path, params string[] contents)
    {
        _existingDirectories.Add(path);
        _directoryContents[path] = contents.ToList();
        return this;
    }

    public bool DirectoryExists(string path) => _existingDirectories.Contains(path);

    public bool FileExists(string path) => _existingFiles.Contains(path);

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        if (_directoryContents.TryGetValue(path, out var contents))
        {
            return contents.Where(c => _existingDirectories.Contains(c));
        }
        return Enumerable.Empty<string>();
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*")
    {
        if (_directoryContents.TryGetValue(path, out var contents))
        {
            var files = contents.Where(c => _existingFiles.Contains(c));

            if (searchPattern != "*" && searchPattern != "*.*")
            {
                var extension = searchPattern.Replace("*", "");
                files = files.Where(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
            }

            return files;
        }
        return Enumerable.Empty<string>();
    }

    public IEnumerable<string> EnumerateFileSystemEntries(string path)
    {
        if (_directoryContents.TryGetValue(path, out var contents))
        {
            return contents;
        }
        return Enumerable.Empty<string>();
    }

    public string ReadAllText(string path)
    {
        if (_fileContents.TryGetValue(path, out var content))
        {
            return content;
        }
        throw new FileNotFoundException($"Mock file not found: {path}");
    }

    public string[] ReadAllLines(string path)
    {
        if (_fileLines.TryGetValue(path, out var lines))
        {
            return lines;
        }
        if (_fileContents.TryGetValue(path, out var content))
        {
            return content.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
        }
        throw new FileNotFoundException($"Mock file not found: {path}");
    }

    public string GetFileName(string path) => Path.GetFileName(path);

    public string GetExtension(string path) => Path.GetExtension(path);

    public string GetDirectoryName(string path) => Path.GetDirectoryName(path) ?? string.Empty;

    public bool IsDirectory(string path) => _existingDirectories.Contains(path);

    public FileInfo GetFileInfo(string path) => new FileInfo(path);

    public void WriteAllText(string path, string contents)
    {
        _existingFiles.Add(path);
        _fileContents[path] = contents;
    }

    public void DeleteFile(string path)
    {
        _existingFiles.Remove(path);
        _fileContents.Remove(path);
        _fileLines.Remove(path);
    }
}
