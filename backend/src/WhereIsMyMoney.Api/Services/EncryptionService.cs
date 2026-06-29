using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.IO.Compression;

namespace WhereIsMyMoney.Api.Services;

public sealed class EncryptionService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private const string Purpose = "EnableBankingCertificate";

    public EncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            // Compress the plaintext to reduce size
            byte[] compressedData = CompressString(plainText);

            // Encrypt the compressed data
            IDataProtector protector = _dataProtectionProvider.CreateProtector(Purpose);
            byte[] encryptedData = protector.Protect(compressedData);

            // Return as Base64 for storage
            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to encrypt sensitive data.", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        try
        {
            // Convert from Base64
            byte[] encryptedData = Convert.FromBase64String(encryptedText);

            // Decrypt the data
            IDataProtector protector = _dataProtectionProvider.CreateProtector(Purpose);
            byte[] compressedData = protector.Unprotect(encryptedData);

            // Decompress to get original plaintext
            return DecompressBytes(compressedData);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Failed to decrypt sensitive data because a required Data Protection key is missing. " +
                "Ensure the API uses a persistent Data Protection key ring and re-save the integration secret if keys were lost.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt sensitive data.", ex);
        }
    }

    private static byte[] CompressString(string text)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
        using (MemoryStream ms = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, leaveOpen: false))
            {
                gzip.Write(buffer, 0, buffer.Length);
            }
            return ms.ToArray();
        }
    }

    private static string DecompressBytes(byte[] buffer)
    {
        using (MemoryStream ms = new MemoryStream(buffer))
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(gzip, System.Text.Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
