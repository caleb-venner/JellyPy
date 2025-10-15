using Xunit;
using Jellyfin.Plugin.JellyPy.Configuration;

namespace Jellyfin.Plugin.JellyPy.Tests;

/// <summary>
/// Tests for API key encryption functionality.
/// </summary>
public class EncryptionTests : TestFixtureBase
{
    /// <summary>
    /// Test basic encryption and decryption functionality.
    /// </summary>
    [Fact]
    public void EncryptDecrypt_BasicFunctionality_ShouldWorkCorrectly()
    {
        // Arrange
        const string originalKey = "test-api-key-12345";
        string machineKey = EncryptionHelper.GenerateMachineKey();

        // Act
        string encrypted = EncryptionHelper.Encrypt(originalKey, machineKey);
        string decrypted = EncryptionHelper.Decrypt(encrypted, machineKey);

        // Assert
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(originalKey, encrypted);
        Assert.Equal(originalKey, decrypted);
    }

    /// <summary>
    /// Test that empty strings are handled correctly.
    /// </summary>
    [Fact]
    public void EncryptDecrypt_EmptyString_ShouldReturnEmpty()
    {
        // Arrange
        string machineKey = EncryptionHelper.GenerateMachineKey();

        // Act & Assert
        Assert.Equal(string.Empty, EncryptionHelper.Encrypt(string.Empty, machineKey));
        Assert.Equal(string.Empty, EncryptionHelper.Decrypt(string.Empty, machineKey));
        Assert.Equal(string.Empty, EncryptionHelper.Encrypt(null, machineKey));
        Assert.Equal(string.Empty, EncryptionHelper.Decrypt(null, machineKey));
    }

    /// <summary>
    /// Test that configuration properties work correctly.
    /// </summary>
    [Fact]
    public void PluginConfiguration_ApiKeyProperties_ShouldEncryptAndDecrypt()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string testApiKey = "sonarr-test-key-456";

        // Act
        config.SonarrApiKey = testApiKey;

        // Assert
        Assert.Equal(testApiKey, config.SonarrApiKey);
        Assert.NotEmpty(config.SonarrApiKeyEncrypted);
        Assert.NotEqual(testApiKey, config.SonarrApiKeyEncrypted);
    }

    /// <summary>
    /// Test that different inputs produce different encrypted outputs.
    /// </summary>
    [Fact]
    public void Encrypt_DifferentInputs_ShouldProduceDifferentOutputs()
    {
        // Arrange
        string machineKey = EncryptionHelper.GenerateMachineKey();
        const string key1 = "api-key-1";
        const string key2 = "api-key-2";

        // Act
        string encrypted1 = EncryptionHelper.Encrypt(key1, machineKey);
        string encrypted2 = EncryptionHelper.Encrypt(key2, machineKey);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    /// <summary>
    /// Test that same input with different keys produces different outputs.
    /// </summary>
    [Fact]
    public void Encrypt_SameInputDifferentKeys_ShouldProduceDifferentOutputs()
    {
        // Arrange
        const string apiKey = "same-api-key";
        string key1 = "machine-key-1";
        string key2 = "machine-key-2";

        // Act
        string encrypted1 = EncryptionHelper.Encrypt(apiKey, key1);
        string encrypted2 = EncryptionHelper.Encrypt(apiKey, key2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    /// <summary>
    /// Test that machine key generation is consistent.
    /// </summary>
    [Fact]
    public void GenerateMachineKey_MultipleCalls_ShouldReturnSameKey()
    {
        // Act
        string key1 = EncryptionHelper.GenerateMachineKey();
        string key2 = EncryptionHelper.GenerateMachineKey();

        // Assert
        Assert.Equal(key1, key2);
        Assert.NotEmpty(key1);
    }
}