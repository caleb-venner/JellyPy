using Xunit;
using Jellyfin.Plugin.Jellypy.Configuration;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Tests for automatic API key encryption in configuration properties.
/// </summary>
public class AutoEncryptionTests : TestFixtureBase
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
        // Use a longer plaintext to ensure the encrypted value is long enough to be detected
        const string plaintextKey = "test-api-key-that-is-long-enough-to-produce-encrypted-value";
        
        // First encrypt manually
        string machineKey = EncryptionHelper.GenerateMachineKey();
        string encryptedKey = EncryptionHelper.Encrypt(plaintextKey, machineKey);

        // Verify the encrypted key is detected as encrypted
        Assert.True(encryptedKey.Length >= 64, "Encrypted value should be at least 64 characters");

        // Act - Set the already encrypted value
        config.RadarrApiKeyEncrypted = encryptedKey;

        // Assert - Should not change the encrypted value (no double encryption)
        Assert.Equal(encryptedKey, config.RadarrApiKeyEncrypted);
        
        // Verify we can still decrypt it correctly
        Assert.Equal(plaintextKey, config.RadarrApiKey);
    }
}