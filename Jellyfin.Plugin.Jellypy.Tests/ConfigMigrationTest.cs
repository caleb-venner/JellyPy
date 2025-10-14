using Xunit;
using Jellyfin.Plugin.Jellypy.Configuration;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Test to validate configuration migration behavior.
/// </summary>
public class ConfigMigrationTest
{
    /// <summary>
    /// Test that simulates loading plaintext API key from old XML configuration.
    /// </summary>
    [Fact]
    public void LegacyMigration_PlaintextInEncryptedField_ShouldMigrateCorrectly()
    {
        var config = new PluginConfiguration();
        
        // Simulate loading plaintext API key from old XML configuration
        // (This simulates what would happen when XML deserializer loads an old config file)
        config.SonarrApiKeyEncrypted = "legacy-plaintext-api-key";
        
        // When we access the property, it should detect this is plaintext and migrate it
        string retrievedKey = config.SonarrApiKey;
        
        // Assert the migration worked correctly
        Assert.Equal("legacy-plaintext-api-key", retrievedKey);
        Assert.NotEqual("legacy-plaintext-api-key", config.SonarrApiKeyEncrypted);
        Assert.NotEmpty(config.SonarrApiKeyEncrypted);
        
        // Verify it can decrypt properly now
        Assert.Equal("legacy-plaintext-api-key", config.SonarrApiKey);
    }

    /// <summary>
    /// Test that new API key setting works correctly.
    /// </summary>
    [Fact]
    public void NewApiKey_SetValue_ShouldEncryptCorrectly()
    {
        var config = new PluginConfiguration();
        
        // Set a new API key
        config.SonarrApiKey = "new-test-api-key";
        
        // Should be encrypted in storage
        Assert.NotEqual("new-test-api-key", config.SonarrApiKeyEncrypted);
        Assert.NotEmpty(config.SonarrApiKeyEncrypted);
        
        // Should decrypt correctly when accessed
        Assert.Equal("new-test-api-key", config.SonarrApiKey);
    }
}