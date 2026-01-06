using System.Runtime.InteropServices;
using System.Text;
using CustosAC.UI;

namespace CustosAC.Scanner;

/// <summary>
/// Анализатор PE-файлов (Portable Executable)
/// </summary>
public static class PEAnalyzer
{
    #region PE Constants

    private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
    private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE\0\0

    // Machine Types
    private const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;
    private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;

    // Section Characteristics
    private const uint IMAGE_SCN_MEM_EXECUTE = 0x20000000;
    private const uint IMAGE_SCN_MEM_READ = 0x40000000;
    private const uint IMAGE_SCN_MEM_WRITE = 0x80000000;
    private const uint IMAGE_SCN_CNT_CODE = 0x00000020;

    // Optional Header Magic
    private const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b;
    private const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_SECTION_HEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Name;
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;

        public string GetName()
        {
            int len = Array.IndexOf(Name, (byte)0);
            if (len < 0) len = 8;
            return Encoding.ASCII.GetString(Name, 0, len);
        }
    }

    #endregion

    #region Result Classes

    public class PEAnalysisResult
    {
        public string FilePath { get; set; } = "";
        public bool IsValidPE { get; set; }
        public bool Is64Bit { get; set; }
        public DateTime CompileTime { get; set; }
        public List<SectionInfo> Sections { get; set; } = new();
        public List<string> Imports { get; set; } = new();
        public List<string> Exports { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public PackerInfo? DetectedPacker { get; set; }
        public bool HasSuspiciousImports { get; set; }
        public bool HasSuspiciousSections { get; set; }
        public bool HasRWXSection { get; set; }
        public int SuspiciousScore { get; set; }
    }

    public class SectionInfo
    {
        public string Name { get; set; } = "";
        public uint VirtualSize { get; set; }
        public uint RawSize { get; set; }
        public uint Characteristics { get; set; }
        public bool IsExecutable { get; set; }
        public bool IsWritable { get; set; }
        public bool IsReadable { get; set; }
        public bool IsRWX => IsExecutable && IsWritable && IsReadable;
    }

    public class PackerInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> MatchedSignatures { get; set; } = new();
    }

    #endregion

    /// <summary>
    /// Полный анализ PE-файла
    /// </summary>
    public static PEAnalysisResult AnalyzeFile(string filePath)
    {
        var result = new PEAnalysisResult { FilePath = filePath };

        if (!File.Exists(filePath))
        {
            result.Warnings.Add("Файл не существует");
            return result;
        }

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // Читаем DOS Header
            if (stream.Length < Marshal.SizeOf<IMAGE_DOS_HEADER>())
            {
                result.Warnings.Add("Файл слишком мал для PE");
                return result;
            }

            var dosHeader = ReadStruct<IMAGE_DOS_HEADER>(reader);
            if (dosHeader.e_magic != IMAGE_DOS_SIGNATURE)
            {
                result.Warnings.Add("Неверная DOS сигнатура");
                return result;
            }

            // Переходим к NT Headers
            stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

            uint ntSignature = reader.ReadUInt32();
            if (ntSignature != IMAGE_NT_SIGNATURE)
            {
                result.Warnings.Add("Неверная NT сигнатура");
                return result;
            }

            result.IsValidPE = true;

            // Читаем File Header
            var fileHeader = ReadStruct<IMAGE_FILE_HEADER>(reader);

            result.Is64Bit = fileHeader.Machine == IMAGE_FILE_MACHINE_AMD64;

            // Время компиляции
            result.CompileTime = DateTimeOffset.FromUnixTimeSeconds(fileHeader.TimeDateStamp).DateTime;

            // Проверяем время компиляции (подозрительно если в будущем или очень старое)
            if (result.CompileTime > DateTime.Now.AddDays(1))
            {
                result.Warnings.Add("Время компиляции в будущем (возможная манипуляция)");
                result.SuspiciousScore += 10;
            }

            // Читаем Optional Header Magic для определения разрядности
            ushort optionalMagic = reader.ReadUInt16();
            int optionalHeaderOffset = result.Is64Bit ? 108 : 92; // Размер до DataDirectory

            // Пропускаем остальную часть Optional Header
            stream.Seek(dosHeader.e_lfanew + 4 + Marshal.SizeOf<IMAGE_FILE_HEADER>() + fileHeader.SizeOfOptionalHeader, SeekOrigin.Begin);

            // Читаем секции
            var suspiciousSections = HashScanner.GetSuspiciousSections();

            for (int i = 0; i < fileHeader.NumberOfSections; i++)
            {
                var sectionHeader = ReadStruct<IMAGE_SECTION_HEADER>(reader);
                var section = new SectionInfo
                {
                    Name = sectionHeader.GetName(),
                    VirtualSize = sectionHeader.VirtualSize,
                    RawSize = sectionHeader.SizeOfRawData,
                    Characteristics = sectionHeader.Characteristics,
                    IsExecutable = (sectionHeader.Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0,
                    IsWritable = (sectionHeader.Characteristics & IMAGE_SCN_MEM_WRITE) != 0,
                    IsReadable = (sectionHeader.Characteristics & IMAGE_SCN_MEM_READ) != 0
                };

                result.Sections.Add(section);

                // Проверка на RWX секции
                if (section.IsRWX)
                {
                    result.HasRWXSection = true;
                    result.Warnings.Add($"RWX секция: {section.Name} (подозрительно)");
                    result.SuspiciousScore += 20;
                }

                // Проверка на подозрительные имена секций
                if (suspiciousSections.Any(s => section.Name.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    result.HasSuspiciousSections = true;
                    result.Warnings.Add($"Подозрительная секция: {section.Name}");
                    result.SuspiciousScore += 15;
                }

                // Проверка на аномальный размер
                if (section.VirtualSize > 0 && section.RawSize == 0 && section.IsExecutable)
                {
                    result.Warnings.Add($"Секция {section.Name} без raw данных (возможный shellcode)");
                    result.SuspiciousScore += 10;
                }
            }

            // Проверяем на пакеры
            result.DetectedPacker = DetectPacker(result.Sections);
            if (result.DetectedPacker != null)
            {
                result.Warnings.Add($"Обнаружен пакер: {result.DetectedPacker.Name}");
                result.SuspiciousScore += 15;
            }

            // Анализ импортов (упрощенный)
            result.Imports = ExtractImports(filePath);
            var suspiciousImports = HashScanner.GetSuspiciousImports();

            foreach (var import in result.Imports)
            {
                if (suspiciousImports.Any(si => import.Contains(si, StringComparison.OrdinalIgnoreCase)))
                {
                    result.HasSuspiciousImports = true;
                    result.Warnings.Add($"Подозрительный импорт: {import}");
                    result.SuspiciousScore += 5;
                }
            }

            // Проверка на оверлей (данные после PE)
            long peEnd = CalculatePEEnd(stream, dosHeader, fileHeader);
            if (stream.Length > peEnd + 1024) // Более 1KB оверлея
            {
                result.Warnings.Add($"Обнаружен оверлей: {stream.Length - peEnd} байт");
                result.SuspiciousScore += 5;
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Ошибка анализа: {ex.Message}");
        }

        return result;
    }

    private static T ReadStruct<T>(BinaryReader reader) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] bytes = reader.ReadBytes(size);

        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private static PackerInfo? DetectPacker(List<SectionInfo> sections)
    {
        var sectionNames = sections.Select(s => s.Name).ToList();

        // UPX
        if (sectionNames.Any(n => n.StartsWith("UPX", StringComparison.OrdinalIgnoreCase)))
        {
            return new PackerInfo
            {
                Name = "UPX",
                Description = "Ultimate Packer for Executables",
                MatchedSignatures = sectionNames.Where(n => n.StartsWith("UPX")).ToList()
            };
        }

        // VMProtect
        if (sectionNames.Any(n => n.StartsWith(".vmp", StringComparison.OrdinalIgnoreCase)))
        {
            return new PackerInfo
            {
                Name = "VMProtect",
                Description = "VMProtect Software Protection",
                MatchedSignatures = sectionNames.Where(n => n.StartsWith(".vmp")).ToList()
            };
        }

        // Themida
        if (sectionNames.Any(n => n.Contains("themida", StringComparison.OrdinalIgnoreCase)))
        {
            return new PackerInfo
            {
                Name = "Themida",
                Description = "Themida/WinLicense Protection",
                MatchedSignatures = sectionNames.Where(n => n.Contains("themida")).ToList()
            };
        }

        // ASPack
        if (sectionNames.Any(n => n.Contains("aspack", StringComparison.OrdinalIgnoreCase) ||
                                  n.Contains("adata", StringComparison.OrdinalIgnoreCase)))
        {
            return new PackerInfo
            {
                Name = "ASPack",
                Description = "ASPack Packer",
                MatchedSignatures = sectionNames.Where(n => n.Contains("aspack") || n.Contains("adata")).ToList()
            };
        }

        return null;
    }

    private static List<string> ExtractImports(string filePath)
    {
        var imports = new List<string>();

        try
        {
            // Используем простой метод - ищем строки в бинарном файле
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileContent = Encoding.ASCII.GetString(fileBytes);

            // Ищем известные DLL имена
            var knownDlls = new[] { "kernel32", "ntdll", "user32", "advapi32", "ws2_32" };

            foreach (var dll in knownDlls)
            {
                if (fileContent.Contains(dll, StringComparison.OrdinalIgnoreCase))
                {
                    imports.Add(dll + ".dll");
                }
            }

            // Ищем подозрительные функции
            var suspiciousFunctions = HashScanner.GetSuspiciousImports();
            foreach (var func in suspiciousFunctions)
            {
                if (fileContent.Contains(func, StringComparison.OrdinalIgnoreCase))
                {
                    imports.Add(func);
                }
            }
        }
        catch
        {
            // Игнорируем ошибки
        }

        return imports.Distinct().ToList();
    }

    private static long CalculatePEEnd(FileStream stream, IMAGE_DOS_HEADER dosHeader, IMAGE_FILE_HEADER fileHeader)
    {
        // Приблизительный расчет конца PE
        long peEnd = dosHeader.e_lfanew + 4 + Marshal.SizeOf<IMAGE_FILE_HEADER>() + fileHeader.SizeOfOptionalHeader;
        peEnd += fileHeader.NumberOfSections * Marshal.SizeOf<IMAGE_SECTION_HEADER>();

        return peEnd;
    }

    /// <summary>
    /// Сканирование директории с анализом PE
    /// </summary>
    public static void ScanDirectory()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ PE-ФАЙЛОВ ═══{ConsoleUI.ColorReset}\n");

        // Загружаем базу сигнатур
        HashScanner.LoadDatabase();

        var suspiciousFiles = new List<PEAnalysisResult>();
        int scannedCount = 0;

        var directories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;

            ConsoleUI.Log($"+ Сканирование: {dir}", true);

            try
            {
                var files = Directory.EnumerateFiles(dir, "*.exe", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MaxRecursionDepth = 3
                });

                foreach (var file in files)
                {
                    scannedCount++;
                    Console.Write($"\r  Проанализировано: {scannedCount}");

                    var result = AnalyzeFile(file);

                    if (result.SuspiciousScore >= 10)
                    {
                        suspiciousFiles.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.Log($"  - Ошибка: {ex.Message}", false);
            }
        }

        Console.WriteLine($"\r  Проанализировано: {scannedCount}          ");
        Console.WriteLine();

        // Сортируем по подозрительности
        suspiciousFiles = suspiciousFiles.OrderByDescending(f => f.SuspiciousScore).ToList();

        if (suspiciousFiles.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ PE-ФАЙЛЫ: {suspiciousFiles.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var file in suspiciousFiles)
            {
                string scoreColor = file.SuspiciousScore >= 30 ? ConsoleUI.ColorRed :
                                   file.SuspiciousScore >= 20 ? ConsoleUI.ColorOrange :
                                   ConsoleUI.ColorYellow;

                Console.WriteLine($"{ConsoleUI.ColorCyan}► {Path.GetFileName(file.FilePath)}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Путь: {file.FilePath}");
                Console.WriteLine($"  {scoreColor}Уровень подозрительности: {file.SuspiciousScore}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Разрядность: {(file.Is64Bit ? "64-bit" : "32-bit")}");
                Console.WriteLine($"  Скомпилирован: {file.CompileTime:yyyy-MM-dd HH:mm}");

                if (file.DetectedPacker != null)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}Пакер: {file.DetectedPacker.Name}{ConsoleUI.ColorReset}");
                }

                if (file.Warnings.Count > 0)
                {
                    Console.WriteLine($"  Предупреждения:");
                    foreach (var warning in file.Warnings.Take(5))
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }

                Console.WriteLine();
            }
        }
        else
        {
            ConsoleUI.Log("+ Подозрительных PE-файлов не обнаружено", true);
        }

        ConsoleUI.Pause();
    }
}
