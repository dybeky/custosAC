using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Custos.Core.Services;

/// <summary>
/// Security service: anti-debug, integrity verification
/// </summary>
public static class SecurityService
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, out bool isDebuggerPresent);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle, int processInformationClass,
        out IntPtr processInformation, int processInformationLength, out int returnLength);

    private const int ProcessDebugPort = 7;

    public static bool IsBeingDebugged()
    {
        try
        {
            if (Debugger.IsAttached) return true;
            if (IsDebuggerPresent()) return true;

            using var currentProcess = Process.GetCurrentProcess();

            if (CheckRemoteDebuggerPresent(currentProcess.Handle, out bool isRemoteDebugger) && isRemoteDebugger)
                return true;

            if (NtQueryInformationProcess(currentProcess.Handle, ProcessDebugPort, out IntPtr debugPort, IntPtr.Size, out _) == 0)
            {
                if (debugPort != IntPtr.Zero) return true;
            }

            var sw = Stopwatch.StartNew();
            Thread.Sleep(1);
            sw.Stop();
            if (sw.ElapsedMilliseconds > 100) return true;

            return false;
        }
        catch { return false; }
    }

    public static string ComputeFileHash(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        catch { return string.Empty; }
    }

    public static List<string> DetectSuspiciousTools()
    {
        var suspicious = new List<string>();
        var exactMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ce", "ida", "ida64", "hxd" };
        var partialMatch = new[]
        {
            "cheatengine", "x64dbg", "x32dbg", "ollydbg", "dnspy", "ilspy",
            "dotpeek", "processhacker", "procmon", "procexp", "wireshark",
            "fiddler", "ghidra", "radare2"
        };

        Process[]? processes = null;
        try
        {
            processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    var name = process.ProcessName;
                    var nameLower = name.ToLowerInvariant();

                    if (exactMatch.Contains(name))
                    {
                        suspicious.Add($"{name} (PID: {process.Id})");
                        continue;
                    }

                    if (partialMatch.Any(d => nameLower.Contains(d)))
                        suspicious.Add($"{name} (PID: {process.Id})");
                }
                catch { }
            }
        }
        catch { }
        finally
        {
            // Dispose all Process objects to prevent handle leak
            if (processes != null)
            {
                foreach (var proc in processes)
                {
                    try { proc.Dispose(); } catch { }
                }
            }
        }

        return suspicious;
    }

    public static bool IsRunningInVM()
    {
        try
        {
            var vmIndicators = new[] { "vmware", "virtual", "xen", "qemu", "kvm", "parallels", "vbox", "hyper-v" };
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (var item in searcher.Get())
            {
                var manufacturer = item["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? "";
                var model = item["Model"]?.ToString()?.ToLowerInvariant() ?? "";

                if (vmIndicators.Any(ind => manufacturer.Contains(ind) || model.Contains(ind)))
                    return true;
            }
        }
        catch { }

        return false;
    }
}
