using Xunit;
using Jellyfin.Plugin.Jellypy.Configuration;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Tests for automatic API key encryption in configuration properties.
/// </summary>
public class AutoEncryptionTests
{
    /// <summary>
    /// Test that setting a plaintext API key automatically encrypts it.
    /// </summary>
    [Fact]
    public void RadarrApiKeyEncrypted_SetPlaintext_ShouldAutoEncrypt()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string plaintextKey = "0d1c58ceb4d34ad992c1fd016ce60a6a"; // The test key from user

        // Act
        config.RadarrApiKeyEncrypted = plaintextKey;

        // Assert
        Assert.NotEqual(plaintextKey, config.RadarrApiKeyEncrypted);
        Assert.NotEmpty(config.RadarrApiKeyEncrypted);
        Assert.True(config.RadarrApiKeyEncrypted.Length > plaintextKey.Length);
    }

    /// <summary>
    /// Test that the encrypted key can be decrypted back to the original.
    /// </summary>
    [Fact]
    public void RadarrApiKey_SetAndGet_ShouldRoundTrip()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string originalKey = "0d1c58ceb4d34ad992c1fd016ce60a6a";

        // Act - Set through encrypted property (should auto-encrypt)
        config.RadarrApiKeyEncrypted = originalKey;
        
        // Get through compatibility property (should auto-decrypt)
        string retrievedKey = config.RadarrApiKey;

        // Assert
        Assert.Equal(originalKey, retrievedKey);
    }

    /// <summary>
    /// Test that already encrypted values are not double-encrypted.
    /// </summary>
    [Fact]
    public void RadarrApiKeyEncrypted_SetEncrypted_ShouldNotDoubleEncrypt()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string plaintextKey = "test-key-12345";
        
        // First encrypt manually
        string machineKey = EncryptionHelper.GenerateMachineKey();
        string encryptedKey = EncryptionHelper.Encrypt(plaintextKey, machineKey);

        // Act - Set the already encrypted value
        config.RadarrApiKeyEncrypted = encryptedKey;

        // Assert - Should not change the encrypted value
        Assert.Equal(encryptedKey, config.RadarrApiKeyEncrypted);
    }
}