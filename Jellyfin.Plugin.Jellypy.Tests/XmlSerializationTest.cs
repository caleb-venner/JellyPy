using Xunit;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Jellyfin.Plugin.Jellypy.Configuration;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Test XML serialization behavior to understand migration issues.
/// </summary>
public class XmlSerializationTest
{
    /// <summary>
    /// Test what XML serialization produces and consumes.
    /// </summary>
    [Fact]
    public void XmlSerialization_ShowsPropertyNames()
    {
        var config = new PluginConfiguration
        {
            SonarrApiKey = "test-api-key-123"
        };

        // Serialize to XML
        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, config);
        string xmlContent = stringWriter.ToString();

        // Output for debugging
        System.Console.WriteLine("Generated XML:");
        System.Console.WriteLine(xmlContent);

        // Deserialize back
        using var stringReader = new StringReader(xmlContent);
        var deserializedConfig = (PluginConfiguration)serializer.Deserialize(stringReader)!;

        Assert.NotNull(deserializedConfig);
        Assert.NotEmpty(deserializedConfig.SonarrApiKeyEncrypted); // Should be encrypted
        
        // The SonarrApiKey property should decrypt properly
        Assert.Equal("test-api-key-123", deserializedConfig.SonarrApiKey);
    }
    
    /// <summary>
    /// Test loading legacy XML with plaintext API key.
    /// </summary>
    [Fact]
    public void XmlDeserialization_LegacyFormat_ShouldMigrate()
    {
        // Simulate old XML format with plaintext API key
        string legacyXml = @"<?xml version=""1.0""?>
<PluginConfiguration xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <SonarrApiKey>legacy-plaintext-key</SonarrApiKey>
  <RadarrApiKey>legacy-radarr-key</RadarrApiKey>
  <SonarrUrl>http://localhost:8989</SonarrUrl>
  <RadarrUrl>http://localhost:7878</RadarrUrl>
</PluginConfiguration>";

        // Deserialize
        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var stringReader = new StringReader(legacyXml);
        var config = (PluginConfiguration)serializer.Deserialize(stringReader);

        // Check that keys were migrated to encrypted storage
        Assert.NotNull(config);
        Assert.NotEmpty(config.SonarrApiKeyEncrypted);
        Assert.NotEmpty(config.RadarrApiKeyEncrypted);
        
        // Check that we can retrieve the original values through the properties
        Assert.Equal("legacy-plaintext-key", config.SonarrApiKey);
        Assert.Equal("legacy-radarr-key", config.RadarrApiKey);
    }
}