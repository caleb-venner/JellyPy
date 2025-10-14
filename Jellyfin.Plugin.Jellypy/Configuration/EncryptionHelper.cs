using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.Jellypy.Configuration;

/// <summary>
/// Helper class for encrypting and decrypting sensitive configuration data.
/// </summary>
public static class EncryptionHelper
{
    private static readonly byte[] _salt =
    [
        0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
        0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
    ];

    private static IServerApplicationHost? _applicationHost;

    /// <summary>
    /// Initializes the EncryptionHelper with the Jellyfin server application host.
    /// </summary>
    /// <param name="applicationHost">The Jellyfin server application host.</param>
    public static void Initialize(IServerApplicationHost applicationHost)
    {
        _applicationHost = applicationHost;
    }

    /// <summary>
    /// Encrypts a plaintext string using AES encryption.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <param name="passPhrase">The encryption passphrase.</param>
    /// <returns>Encrypted string in Base64 format, or empty string if input is null/empty.</returns>
    public static string Encrypt(string plainText, string passPhrase)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            using var password = new Rfc2898DeriveBytes(passPhrase, _salt, 10000, HashAlgorithmName.SHA256);
            byte[] keyBytes = password.GetBytes(256 / 8);

            using var symmetricKey = Aes.Create();
            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            using var encryptor = symmetricKey.CreateEncryptor(keyBytes, symmetricKey.IV);
            using var memoryStream = new MemoryStream();

            // Prepend IV to the encrypted data
            memoryStream.Write(symmetricKey.IV, 0, symmetricKey.IV.Length);

            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();

            byte[] cipherTextBytes = memoryStream.ToArray();
            return Convert.ToBase64String(cipherTextBytes);
        }
        catch (Exception)
        {
            // If encryption fails, return empty string
            return string.Empty;
        }
    }

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="cipherText">The encrypted text in Base64 format.</param>
    /// <param name="passPhrase">The decryption passphrase.</param>
    /// <returns>Decrypted plaintext string, or empty string if decryption fails.</returns>
    public static string Decrypt(string cipherText, string passPhrase)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            byte[] cipherTextBytesWithIV = Convert.FromBase64String(cipherText);

            using var password = new Rfc2898DeriveBytes(passPhrase, _salt, 10000, HashAlgorithmName.SHA256);
            byte[] keyBytes = password.GetBytes(256 / 8);

            using var symmetricKey = Aes.Create();
            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the cipher text
            byte[] ivBytes = new byte[16];
            byte[] cipherTextBytes = new byte[cipherTextBytesWithIV.Length - 16];
            Array.Copy(cipherTextBytesWithIV, 0, ivBytes, 0, ivBytes.Length);
            Array.Copy(cipherTextBytesWithIV, 16, cipherTextBytes, 0, cipherTextBytes.Length);

            using var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivBytes);
            using var memoryStream = new MemoryStream(cipherTextBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();

            cryptoStream.CopyTo(resultStream);
            byte[] plainTextBytes = resultStream.ToArray();

            return Encoding.UTF8.GetString(plainTextBytes);
        }
        catch (Exception)
        {
            // If decryption fails, return empty string (could be unencrypted legacy data)
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates a Jellyfin server-specific encryption key using Plugin GUID + Server ID + Static Salt.
    /// This method creates a stable key that survives OS updates, machine renames, and user changes
    /// but is unique to each Jellyfin installation.
    /// </summary>
    /// <returns>A Jellyfin server-specific encryption key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the EncryptionHelper has not been initialized.</exception>
    public static string GenerateMachineKey()
    {
        if (_applicationHost == null)
        {
            throw new InvalidOperationException("EncryptionHelper not initialized. Call Initialize() first.");
        }

        try
        {
            // Three stable components: Plugin GUID + Server ID + Static Salt
            string pluginGuid = "a5bd541f-38dc-467e-9a9a-15fe3f3bcf5c";
            string serverId = _applicationHost.SystemId ?? "unknown-server";
            string salt = "JellyPy-Server-Key-2024";

            // Simple combination
            string serverKey = $"{pluginGuid}:{serverId}:{salt}";

            // Hash for consistency
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(serverKey));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate server-based encryption key: {ex.Message}", ex);
        }
    }
}
