using System.Diagnostics;
using System.Runtime.InteropServices;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.SystemAnalysis;

/// <summary>
/// Детектор user-mode хуков в системных DLL
/// </summary>
public static class HookDetector
{
    #region Result Classes

    public class HookInfo
    {
        public string ModuleName { get; set; } = "";
        public string FunctionName { get; set; } = "";
        public IntPtr Address { get; set; }
        public byte[] OriginalBytes { get; set; } = Array.Empty<byte>();
        public byte[] CurrentBytes { get; set; } = Array.Empty<byte>();
        public string HookType { get; set; } = "";
    }

    public class HookScanResult
    {
        public List<HookInfo> DetectedHooks { get; set; } = new();
        public int FunctionsChecked { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion

    // Критические функции для проверки на хуки
    private static readonly Dictionary<string, string[]> CriticalFunctions = new()
    {
        ["ntdll.dll"] = new[]
        {
            "NtReadVirtualMemory",
            "NtWriteVirtualMemory",
            "NtOpenProcess",
            "NtProtectVirtualMemory",
            "NtQuerySystemInformation",
            "NtQueryInformationProcess",
            "NtCreateThreadEx",
            "NtAllocateVirtualMemory",
            "LdrLoadDll"
        },
        ["kernel32.dll"] = new[]
        {
            "ReadProcessMemory",
            "WriteProcessMemory",
            "OpenProcess",
            "VirtualProtectEx",
            "CreateRemoteThread",
            "LoadLibraryA",
            "LoadLibraryW",
            "GetProcAddress"
        },
        ["user32.dll"] = new[]
        {
            "GetAsyncKeyState",
            "GetKeyState",
            "SetWindowsHookExA",
            "SetWindowsHookExW"
        }
    };

    // Известные паттерны хуков (первые байты)
    private static readonly byte[][] HookPatterns = new byte[][]
    {
        new byte[] { 0xE9 },           // JMP rel32
        new byte[] { 0xFF, 0x25 },     // JMP [addr]
        new byte[] { 0x68 },           // PUSH addr; RET (push-ret)
        new byte[] { 0x48, 0xB8 },     // MOV RAX, imm64 (x64)
        new byte[] { 0xCC },           // INT3 (breakpoint)
    };

    // Оригинальные первые байты системных функций (примерные)
    // В реальности нужно читать с диска
    private static readonly Dictionary<string, byte[]> ExpectedPrologues = new()
    {
        // x64 syscall стаб
        ["NtReadVirtualMemory"] = new byte[] { 0x4C, 0x8B, 0xD1, 0xB8 },
        ["NtWriteVirtualMemory"] = new byte[] { 0x4C, 0x8B, 0xD1, 0xB8 },
        ["NtOpenProcess"] = new byte[] { 0x4C, 0x8B, 0xD1, 0xB8 },

        // kernel32 обычные прологи
        ["ReadProcessMemory"] = new byte[] { 0x48, 0x89, 0x5C, 0x24 }, // x64
        ["WriteProcessMemory"] = new byte[] { 0x48, 0x89, 0x5C, 0x24 }, // x64
    };

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    /// <summary>
    /// Проверка функции на наличие хука
    /// </summary>
    public static HookInfo? CheckFunctionHook(string moduleName, string functionName)
    {
        try
        {
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
                return null;

            IntPtr funcAddress = GetProcAddress(moduleHandle, functionName);
            if (funcAddress == IntPtr.Zero)
                return null;

            // Читаем первые байты функции
            byte[] currentBytes = new byte[16];
            Marshal.Copy(funcAddress, currentBytes, 0, currentBytes.Length);

            // Проверяем на известные паттерны хуков
            foreach (var pattern in HookPatterns)
            {
                bool match = true;
                for (int i = 0; i < pattern.Length && i < currentBytes.Length; i++)
                {
                    if (currentBytes[i] != pattern[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    // Дополнительная проверка - JMP и PUSH могут быть легитимными
                    // Проверяем, не является ли это форвардингом в ntdll
                    if (currentBytes[0] == 0xE9 || (currentBytes[0] == 0xFF && currentBytes[1] == 0x25))
                    {
                        // Это может быть API forwarding - проверяем цель
                        // Для упрощения считаем подозрительным
                        return new HookInfo
                        {
                            ModuleName = moduleName,
                            FunctionName = functionName,
                            Address = funcAddress,
                            CurrentBytes = currentBytes,
                            HookType = GetHookTypeName(pattern)
                        };
                    }
                }
            }

            // Проверяем против известных прологов (если есть)
            if (ExpectedPrologues.TryGetValue(functionName, out var expected))
            {
                bool mismatch = false;
                for (int i = 0; i < expected.Length && i < currentBytes.Length; i++)
                {
                    if (expected[i] != currentBytes[i])
                    {
                        mismatch = true;
                        break;
                    }
                }

                if (mismatch)
                {
                    return new HookInfo
                    {
                        ModuleName = moduleName,
                        FunctionName = functionName,
                        Address = funcAddress,
                        OriginalBytes = expected,
                        CurrentBytes = currentBytes,
                        HookType = "Modified prologue"
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetHookTypeName(byte[] pattern)
    {
        if (pattern.Length >= 1)
        {
            return pattern[0] switch
            {
                0xE9 => "JMP (relative)",
                0xFF => "JMP (absolute)",
                0x68 => "PUSH-RET",
                0x48 => "MOV RAX (x64)",
                0xCC => "INT3 (breakpoint)",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    /// <summary>
    /// Полное сканирование на хуки
    /// </summary>
    public static HookScanResult ScanForHooks()
    {
        var result = new HookScanResult();

        foreach (var module in CriticalFunctions)
        {
            foreach (var function in module.Value)
            {
                result.FunctionsChecked++;

                var hookInfo = CheckFunctionHook(module.Key, function);
                if (hookInfo != null)
                {
                    result.DetectedHooks.Add(hookInfo);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Сканирование и отображение результатов
    /// </summary>
    public static void ScanHooks()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ДЕТЕКЦИЯ USER-MODE ХУКОВ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Проверка критических функций...", true);
        Console.WriteLine($"  (Проверяются функции в ntdll.dll, kernel32.dll, user32.dll)\n");

        var result = ScanForHooks();

        Console.WriteLine($"{ConsoleUI.ColorCyan}Результаты:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Проверено функций: {result.FunctionsChecked}");
        Console.WriteLine($"  Обнаружено хуков: {result.DetectedHooks.Count}");

        if (result.DetectedHooks.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ОБНАРУЖЕНЫ ХУКИ ══{ConsoleUI.ColorReset}\n");

            Console.WriteLine($"{ConsoleUI.ColorYellow}ВНИМАНИЕ: Обнаружение хуков может означать:{ConsoleUI.ColorReset}");
            Console.WriteLine($"  - Установленный античит (EAC, BattlEye, etc.)");
            Console.WriteLine($"  - Антивирусное ПО");
            Console.WriteLine($"  - Или вредоносное/читерское ПО\n");

            foreach (var hook in result.DetectedHooks)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {hook.ModuleName}!{hook.FunctionName}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Адрес: 0x{hook.Address.ToInt64():X}");
                Console.WriteLine($"  Тип хука: {hook.HookType}");
                Console.WriteLine($"  Текущие байты: {BitConverter.ToString(hook.CurrentBytes.Take(8).ToArray())}");

                if (hook.OriginalBytes.Length > 0)
                {
                    Console.WriteLine($"  Ожидаемые байты: {BitConverter.ToString(hook.OriginalBytes)}");
                }

                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Хуки критических функций не обнаружены{ConsoleUI.ColorReset}");
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}Примечание: Это базовая проверка. Продвинутые хуки{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorYellow}(inline hooks в середине функций, page remapping){ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorYellow}могут не детектироваться на user-mode уровне.{ConsoleUI.ColorReset}");
        }

        // Дополнительная информация
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Проверенные модули:{ConsoleUI.ColorReset}");
        foreach (var module in CriticalFunctions)
        {
            Console.WriteLine($"  • {module.Key}: {module.Value.Length} функций");
        }

        ConsoleUI.Pause();
    }
}
