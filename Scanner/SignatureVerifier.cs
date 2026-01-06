using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Scanner;

/// <summary>
/// Проверка цифровых подписей исполняемых файлов
/// </summary>
public static class SignatureVerifier
{
    #region Structures

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    #endregion

    #region Constants

    private const uint WTD_UI_NONE = 2;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_STATEACTION_VERIFY = 1;
    private const uint WTD_STATEACTION_CLOSE = 2;
    private const uint WTD_REVOKE_WHOLECHAIN = 1;

    private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    #endregion

    #region P/Invoke

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, ref WINTRUST_DATA pWVTData);

    #endregion

    #region Result Classes

    public enum SignatureStatus
    {
        Valid,              // Подпись валидна
        Invalid,            // Подпись недействительна
        NotSigned,          // Файл не подписан
        UntrustedRoot,      // Корневой сертификат не доверен
        Expired,            // Сертификат истёк
        Error               // Ошибка проверки
    }

    public class SignatureResult
    {
        public string FilePath { get; set; } = "";
        public SignatureStatus Status { get; set; }
        public string StatusMessage { get; set; } = "";
        public string? SignerName { get; set; }
        public string? IssuerName { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? SerialNumber { get; set; }
        public string? Thumbprint { get; set; }
        public bool IsTrustedPublisher { get; set; }
    }

    #endregion

    /// <summary>
    /// Проверка цифровой подписи файла
    /// </summary>
    public static SignatureResult VerifySignature(string filePath)
    {
        var result = new SignatureResult { FilePath = filePath };

        if (!File.Exists(filePath))
        {
            result.Status = SignatureStatus.Error;
            result.StatusMessage = "Файл не существует";
            return result;
        }

        try
        {
            // Метод 1: WinVerifyTrust
            var wintrustResult = VerifyWithWinTrust(filePath);
            result.Status = wintrustResult.status;
            result.StatusMessage = wintrustResult.message;

            // Метод 2: Получаем информацию о сертификате
            var certInfo = GetCertificateInfo(filePath);
            if (certInfo != null)
            {
                result.SignerName = certInfo.SignerName;
                result.IssuerName = certInfo.IssuerName;
                result.ValidFrom = certInfo.ValidFrom;
                result.ValidTo = certInfo.ValidTo;
                result.SerialNumber = certInfo.SerialNumber;
                result.Thumbprint = certInfo.Thumbprint;
            }

            // Проверяем, является ли издатель доверенным
            if (!string.IsNullOrEmpty(result.SignerName))
            {
                result.IsTrustedPublisher = IsTrustedPublisher(result.SignerName);
            }
        }
        catch (Exception ex)
        {
            result.Status = SignatureStatus.Error;
            result.StatusMessage = ex.Message;
        }

        return result;
    }

    private static (SignatureStatus status, string message) VerifyWithWinTrust(string filePath)
    {
        var fileInfo = new WINTRUST_FILE_INFO
        {
            cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        IntPtr pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());

        try
        {
            Marshal.StructureToPtr(fileInfo, pFileInfo, false);

            var wintrustData = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = pFileInfo,
                dwStateAction = WTD_STATEACTION_VERIFY,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwProvFlags = 0,
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            Guid actionId = WINTRUST_ACTION_GENERIC_VERIFY_V2;
            int hr = WinVerifyTrust(IntPtr.Zero, ref actionId, ref wintrustData);

            // Закрываем состояние
            wintrustData.dwStateAction = WTD_STATEACTION_CLOSE;
            WinVerifyTrust(IntPtr.Zero, ref actionId, ref wintrustData);

            return hr switch
            {
                0 => (SignatureStatus.Valid, "Подпись действительна"),
                unchecked((int)0x800B0100) => (SignatureStatus.NotSigned, "Файл не подписан"),
                unchecked((int)0x800B0109) => (SignatureStatus.UntrustedRoot, "Корневой сертификат не доверен"),
                unchecked((int)0x800B0101) => (SignatureStatus.Expired, "Сертификат истёк"),
                unchecked((int)0x80096010) => (SignatureStatus.Invalid, "Подпись недействительна"),
                _ => (SignatureStatus.Invalid, $"Ошибка проверки: 0x{hr:X8}")
            };
        }
        finally
        {
            Marshal.FreeHGlobal(pFileInfo);
        }
    }

    private static CertificateInfo? GetCertificateInfo(string filePath)
    {
        try
        {
            var cert = X509Certificate.CreateFromSignedFile(filePath);
            var cert2 = new X509Certificate2(cert);

            return new CertificateInfo
            {
                SignerName = cert2.GetNameInfo(X509NameType.SimpleName, false),
                IssuerName = cert2.GetNameInfo(X509NameType.SimpleName, true),
                ValidFrom = cert2.NotBefore,
                ValidTo = cert2.NotAfter,
                SerialNumber = cert2.SerialNumber,
                Thumbprint = cert2.Thumbprint
            };
        }
        catch
        {
            return null;
        }
    }

    private class CertificateInfo
    {
        public string? SignerName { get; set; }
        public string? IssuerName { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string? SerialNumber { get; set; }
        public string? Thumbprint { get; set; }
    }

    private static bool IsTrustedPublisher(string publisherName)
    {
        var trustedPublishers = new[]
        {
            "Microsoft",
            "NVIDIA",
            "AMD",
            "Intel",
            "Valve",
            "Steam",
            "Epic Games",
            "Google",
            "Mozilla",
            "Adobe"
        };

        return trustedPublishers.Any(tp =>
            publisherName.Contains(tp, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Сканирование директории на неподписанные файлы
    /// </summary>
    public static void ScanUnsignedFiles()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА ЦИФРОВЫХ ПОДПИСЕЙ ═══{ConsoleUI.ColorReset}\n");

        var unsignedFiles = new List<SignatureResult>();
        var invalidFiles = new List<SignatureResult>();
        int scannedCount = 0;

        var directories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;

            ConsoleUI.Log($"+ Проверка: {dir}", true);

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
                    Console.Write($"\r  Проверено: {scannedCount}");

                    var result = VerifySignature(file);

                    switch (result.Status)
                    {
                        case SignatureStatus.NotSigned:
                            unsignedFiles.Add(result);
                            break;
                        case SignatureStatus.Invalid:
                        case SignatureStatus.UntrustedRoot:
                        case SignatureStatus.Expired:
                            invalidFiles.Add(result);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.Log($"  - Ошибка: {ex.Message}", false);
            }
        }

        Console.WriteLine($"\r  Проверено: {scannedCount}          ");
        Console.WriteLine();

        // Выводим файлы с недействительной подписью (наиболее подозрительные)
        if (invalidFiles.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ФАЙЛЫ С НЕДЕЙСТВИТЕЛЬНОЙ ПОДПИСЬЮ: {invalidFiles.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var file in invalidFiles)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {Path.GetFileName(file.FilePath)}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Путь: {file.FilePath}");
                Console.WriteLine($"  Статус: {file.StatusMessage}");

                if (!string.IsNullOrEmpty(file.SignerName))
                {
                    Console.WriteLine($"  Издатель: {file.SignerName}");
                }

                Console.WriteLine();
            }
        }

        // Выводим неподписанные файлы
        if (unsignedFiles.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorOrange}{ConsoleUI.ColorBold}══ НЕПОДПИСАННЫЕ ФАЙЛЫ: {unsignedFiles.Count} ══{ConsoleUI.ColorReset}\n");

            // Показываем только первые 20
            foreach (var file in unsignedFiles.Take(20))
            {
                Console.WriteLine($"{ConsoleUI.ColorOrange}► {Path.GetFileName(file.FilePath)}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  {file.FilePath}");
            }

            if (unsignedFiles.Count > 20)
            {
                Console.WriteLine($"\n  ... и ещё {unsignedFiles.Count - 20} файлов");
            }

            Console.WriteLine();
        }

        if (invalidFiles.Count == 0 && unsignedFiles.Count == 0)
        {
            ConsoleUI.Log("+ Все проверенные файлы имеют действительную подпись", true);
        }

        // Итоговая статистика
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Итого:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Проверено файлов: {scannedCount}");
        Console.WriteLine($"  С недействительной подписью: {invalidFiles.Count}");
        Console.WriteLine($"  Без подписи: {unsignedFiles.Count}");

        ConsoleUI.Pause();
    }
}
