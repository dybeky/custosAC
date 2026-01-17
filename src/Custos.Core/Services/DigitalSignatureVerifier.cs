using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Custos.Core.Services;

/// <summary>
/// Verifies digital signatures of files to reduce false positives
/// </summary>
public class DigitalSignatureVerifier
{
    private readonly EnhancedLogService? _logService;
    private readonly Dictionary<string, bool> _signatureCache = new();

    public DigitalSignatureVerifier(EnhancedLogService? logService = null)
    {
        _logService = logService;
    }

    /// <summary>
    /// Checks if a file has a valid Authenticode signature
    /// </summary>
    public bool HasValidSignature(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        // Check cache first
        if (_signatureCache.TryGetValue(filePath, out var cachedResult))
        {
            return cachedResult;
        }

        bool hasValidSig = false;

        try
        {
            // Use X509Certificate2 to verify signature
            var certBase = X509Certificate2.CreateFromSignedFile(filePath);

            if (certBase != null)
            {
                // Convert to X509Certificate2 if needed
                using var cert = certBase as X509Certificate2 ?? new X509Certificate2(certBase);

                // Check if certificate is valid
                using var chain = new X509Chain
                {
                    ChainPolicy =
                    {
                        RevocationMode = X509RevocationMode.Online,
                        RevocationFlag = X509RevocationFlag.ExcludeRoot,
                        VerificationFlags = X509VerificationFlags.NoFlag,
                        VerificationTime = DateTime.Now
                    }
                };

                hasValidSig = chain.Build(cert);

                if (hasValidSig)
                {
                    _logService?.LogTrace(EnhancedLogService.LogCategory.Validation,
                        $"Valid signature: {Path.GetFileName(filePath)} | Issuer: {cert.Issuer}",
                        "SignatureVerifier");
                }
                else
                {
                    _logService?.LogTrace(EnhancedLogService.LogCategory.Validation,
                        $"Invalid signature chain: {Path.GetFileName(filePath)}",
                        "SignatureVerifier");
                }
            }
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // File is not signed or signature is invalid
            hasValidSig = false;
        }
        catch (Exception ex)
        {
            _logService?.LogDebug(EnhancedLogService.LogCategory.Validation,
                $"Error checking signature for {Path.GetFileName(filePath)}: {ex.Message}",
                "SignatureVerifier");
            hasValidSig = false;
        }

        // Cache the result
        _signatureCache[filePath] = hasValidSig;

        return hasValidSig;
    }

    /// <summary>
    /// Gets detailed signature information
    /// </summary>
    public SignatureInfo? GetSignatureInfo(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            var certBase = X509Certificate2.CreateFromSignedFile(filePath);

            if (certBase == null)
                return null;

            using var cert = certBase as X509Certificate2 ?? new X509Certificate2(certBase);

            return new SignatureInfo
            {
                Subject = cert.Subject,
                Issuer = cert.Issuer,
                ValidFrom = cert.NotBefore,
                ValidTo = cert.NotAfter,
                Thumbprint = cert.Thumbprint,
                SerialNumber = cert.SerialNumber,
                IsTrusted = HasValidSignature(filePath)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if signature is from a trusted publisher
    /// </summary>
    public bool IsFromTrustedPublisher(string filePath)
    {
        var info = GetSignatureInfo(filePath);
        if (info == null || !info.IsTrusted)
            return false;

        // List of trusted publishers
        var trustedPublishers = new[]
        {
            "CN=Microsoft Corporation",
            "CN=Microsoft Windows",
            "O=Microsoft Corporation",
            "CN=NVIDIA Corporation",
            "CN=Intel Corporation",
            "CN=AMD Inc",
            "CN=Google LLC",
            "CN=Mozilla Corporation",
            "CN=Adobe Inc",
            "CN=Apple Inc"
        };

        return trustedPublishers.Any(tp =>
            info.Subject.Contains(tp, StringComparison.OrdinalIgnoreCase) ||
            info.Issuer.Contains(tp, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears the signature verification cache
    /// </summary>
    public void ClearCache()
    {
        _signatureCache.Clear();
        _logService?.LogDebug(EnhancedLogService.LogCategory.Performance,
            "Signature verification cache cleared", "SignatureVerifier");
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public (int TotalEntries, int ValidSignatures) GetCacheStats()
    {
        var validCount = _signatureCache.Count(kvp => kvp.Value);
        return (_signatureCache.Count, validCount);
    }
}

/// <summary>
/// Detailed information about a file's digital signature
/// </summary>
public class SignatureInfo
{
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string Thumbprint { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }

    public bool IsExpired => DateTime.Now > ValidTo;
    public bool IsNotYetValid => DateTime.Now < ValidFrom;
    public bool IsCurrentlyValid => !IsExpired && !IsNotYetValid && IsTrusted;

    public override string ToString()
    {
        return $"{Subject} | Valid: {ValidFrom:yyyy-MM-dd} to {ValidTo:yyyy-MM-dd} | Trusted: {IsTrusted}";
    }
}
