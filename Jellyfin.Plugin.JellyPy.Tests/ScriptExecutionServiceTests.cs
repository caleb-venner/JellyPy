using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JellyPy.Tests;

public class ScriptExecutionServiceTests : TestFixtureBase, IDisposable
{
    private readonly string _tempRoot;
    private readonly string _scriptsDir;
    private readonly Plugin _plugin;
    private readonly PluginConfiguration _config;
    private readonly ScriptExecutionService _service;
    private bool _disposed;

    public ScriptExecutionServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "jellypy-tests-" + Guid.NewGuid());
        _scriptsDir = Path.Combine(_tempRoot, "scripts");
        Directory.CreateDirectory(_scriptsDir);

        var appPaths = new FakeApplicationPaths(_tempRoot);
        var xmlSerializer = new Mock<IXmlSerializer>();
        xmlSerializer
            .Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(new PluginConfiguration());
        xmlSerializer
            .Setup(x => x.DeserializeFromBytes(typeof(PluginConfiguration), It.IsAny<byte[]>()))
            .Returns(new PluginConfiguration());
        xmlSerializer
            .Setup(x => x.SerializeToFile(It.IsAny<object>(), It.IsAny<string>()));

        _config = new PluginConfiguration();
        _config.ScriptSettings.Clear();
        _config.GlobalSettings.CustomScriptsDirectory = _scriptsDir;
        _config.GlobalSettings.MaxConcurrentExecutions = 2;
        _config.GlobalSettings.DefaultTimeoutSeconds = 5;
        PluginConfiguration.ScriptsDirectory = _scriptsDir;

        _plugin = new Plugin(appPaths, xmlSerializer.Object);

        var baseType = typeof(Plugin).BaseType;
        var configurationField = baseType?.GetField("_configuration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        configurationField?.SetValue(_plugin, _config);

        var conditionEvaluator = new ConditionEvaluator(NullLogger<ConditionEvaluator>.Instance);
        var dataAttributeProcessor = new DataAttributeProcessor(NullLogger<DataAttributeProcessor>.Instance);
        _service = new ScriptExecutionService(
            NullLogger<ScriptExecutionService>.Instance,
            conditionEvaluator,
            dataAttributeProcessor);
    }

    [Fact]
    public async Task ExecuteScriptsAsync_RunsOnlyEnabledMatchingTriggers()
    {
        _config.ScriptSettings.Clear();
        var logPath = Path.Combine(_tempRoot, "trigger.log");
        WriteScript("trigger.sh", $"#!/bin/bash\necho \"$EVENT_TYPE\" >> \"{logPath}\"\n");

        var eventAttr = new ScriptDataElement { Name = "EVENT_TYPE", SourceField = "EventType", Format = DataAttributeFormat.Environment };

        var disabled = CreateSetting("disabled", false, EventType.PlaybackStart, "trigger.sh", ExecutionMode.Compatibility);
        disabled.DataAttributes.Add(eventAttr);

        var matching = CreateSetting("matching", true, EventType.PlaybackStart, "trigger.sh", ExecutionMode.Compatibility);
        matching.DataAttributes.Add(eventAttr);

        var other = CreateSetting("otherEvent", true, EventType.ItemAdded, "trigger.sh", ExecutionMode.Compatibility);
        other.DataAttributes.Add(eventAttr);

        _config.ScriptSettings.Add(disabled);
        _config.ScriptSettings.Add(matching);
        _config.ScriptSettings.Add(other);

        var applicable = _config.ScriptSettings
            .Where(s => s.Enabled && s.Triggers.Contains(EventType.PlaybackStart))
            .ToList();

        Assert.Single(applicable);

        await _service.ExecuteScriptsAsync(new EventData { EventType = EventType.PlaybackStart });

        Assert.True(File.Exists(logPath));
        var lines = File.ReadAllLines(logPath);
        Assert.Single(lines);
        Assert.Equal("PlaybackStart", lines[0]);
    }

    [Fact]
    public async Task ExecuteScriptsAsync_UsesJsonPayloadWhenNoDataAttributes()
    {
        _config.ScriptSettings.Clear();
        var logPath = Path.Combine(_tempRoot, "json.log");
        File.WriteAllText(logPath, "not-run");
        WriteScript("json.sh", $"#!/bin/bash\nset -e\necho \"$1\" > \"{logPath}\"\n");

        _config.ScriptSettings.Add(CreateSetting("json", true, EventType.PlaybackStart, "json.sh"));

        await _service.ExecuteScriptsAsync(new EventData
        {
            EventType = EventType.PlaybackStart,
            UserName = "alice",
            ItemName = "Example"
        });

        var content = File.ReadAllText(logPath);
        Assert.NotEqual("not-run", content);
        using var json = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal((int)EventType.PlaybackStart, json.RootElement.GetProperty("EventType").GetInt32());
        Assert.Equal("alice", json.RootElement.GetProperty("UserName").GetString());
    }

    [Fact]
    public async Task ExecuteScriptsAsync_UsesCompatibilityDataAttributes()
    {
        _config.ScriptSettings.Clear();
        var logPath = Path.Combine(_tempRoot, "compat.log");
        WriteScript(
            "compat.sh",
            $"#!/bin/bash\necho \"ENV:$EVENT_TYPE\" > \"{logPath}\"\necho \"ARGS:$@\" >> \"{logPath}\"\n");

        var attributes = new[]
        {
            new ScriptDataElement { Name = "EVENT_TYPE", SourceField = "EventType", Format = DataAttributeFormat.Environment },
            new ScriptDataElement { Name = "user", SourceField = "UserName", Format = DataAttributeFormat.Argument },
            new ScriptDataElement { Name = "item", SourceField = "ItemName", Format = DataAttributeFormat.Argument }
        };

        var setting = CreateSetting("compat", true, EventType.PlaybackStart, "compat.sh", ExecutionMode.Compatibility);
        foreach (var attribute in attributes)
        {
            setting.DataAttributes.Add(attribute);
        }

        setting.Execution.AdditionalArguments = "--flag \"two words\"";

        _config.ScriptSettings.Add(setting);

        await _service.ExecuteScriptsAsync(new EventData
        {
            EventType = EventType.PlaybackStart,
            UserName = "bob",
            ItemName = "Sample"
        });

        var lines = File.ReadAllLines(logPath);
        Assert.Contains("ENV:PlaybackStart", lines[0]);
        Assert.Contains("--user", lines[1]);
        Assert.Contains("bob", lines[1]);
        Assert.Contains("--item", lines[1]);
        Assert.Contains("Sample", lines[1]);
        Assert.Contains("--flag two words", lines[1]);
    }

    [Fact]
    public async Task ExecuteScriptsAsync_StopsOnTimeout()
    {
        _config.ScriptSettings.Clear();
        var logPath = Path.Combine(_tempRoot, "timeout.log");
        WriteScript("timeout.sh", $"#!/bin/bash\nsleep 2\necho done > \"{logPath}\"\n");

        var setting = CreateSetting("timeout", true, EventType.PlaybackStart, "timeout.sh");
        setting.Execution.TimeoutSeconds = 1;
        _config.ScriptSettings.Add(setting);

        await _service.ExecuteScriptsAsync(new EventData { EventType = EventType.PlaybackStart });

        Assert.False(File.Exists(logPath));
    }

    [Fact]
    public async Task ExecuteScriptsAsync_RespectsMaxConcurrentExecutions()
    {
        _config.ScriptSettings.Clear();
        _config.GlobalSettings.MaxConcurrentExecutions = 1;

        var lockPath = Path.Combine(_tempRoot, "concurrency.lock");
        var logPath = Path.Combine(_tempRoot, "concurrency.log");
        File.WriteAllText(logPath, string.Empty);

        WriteScript(
            "concurrency.sh",
            $"#!/bin/bash\nif [ -f \"{lockPath}\" ]; then echo overlap >> \"{logPath}\"; exit 0; fi\ntouch \"{lockPath}\"\nsleep 1\nrm -f \"{lockPath}\"\necho done >> \"{logPath}\"\n");

        _config.ScriptSettings.Add(CreateSetting("first", true, EventType.PlaybackStart, "concurrency.sh"));
        _config.ScriptSettings.Add(CreateSetting("second", true, EventType.PlaybackStart, "concurrency.sh"));

        await _service.ExecuteScriptsAsync(new EventData { EventType = EventType.PlaybackStart });

        Assert.True(File.Exists(logPath));
        var content = File.ReadAllText(logPath);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain("overlap", content);
        Assert.Equal(2, lines.Length);
    }

    private ScriptSetting CreateSetting(string name, bool enabled, EventType trigger, string scriptName, ExecutionMode mode = ExecutionMode.JsonPayload)
    {
        var setting = new ScriptSetting
        {
            Name = name,
            Enabled = enabled,
            ExecutionMode = mode,
            Priority = 100,
            Execution =
            {
                ScriptName = scriptName,
                ExecutorType = ScriptExecutorType.Bash,
                ExecutablePath = "/bin/bash",
                TimeoutSeconds = 5
            }
        };

        setting.Triggers.Add(trigger);
        return setting;
    }

    private void WriteScript(string fileName, string content)
    {
        var path = Path.Combine(_scriptsDir, fileName);
        File.WriteAllText(path, content);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch
            {
                // Best-effort on non-Unix platforms
            }
        }
    }

    private sealed class FakeApplicationPaths : IApplicationPaths
    {
        public FakeApplicationPaths(string root)
        {
            ProgramDataPath = root;
            WebPath = Path.Combine(root, "web");
            ProgramSystemPath = Path.Combine(root, "system");
            DataPath = Path.Combine(root, "data");
            ImageCachePath = Path.Combine(root, "images");
            PluginsPath = Path.Combine(root, "plugins");
            PluginConfigurationsPath = Path.Combine(root, "plugin-config");
            LogDirectoryPath = Path.Combine(root, "logs");
            ConfigurationDirectoryPath = Path.Combine(root, "config-root");
            SystemConfigurationFilePath = Path.Combine(root, "system.xml");
            CachePath = Path.Combine(root, "cache");
            TempDirectory = Path.Combine(root, "cache", "temp");
            VirtualDataPath = "/data";

            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(PluginsPath);
            Directory.CreateDirectory(PluginConfigurationsPath);
            Directory.CreateDirectory(Path.Combine(root, "scripts"));
        }

        public string ProgramDataPath { get; }
        public string WebPath { get; }
        public string ProgramSystemPath { get; }
        public string DataPath { get; }
        public string ImageCachePath { get; }
        public string PluginsPath { get; }
        public string PluginConfigurationsPath { get; }
        public string LogDirectoryPath { get; }
        public string ConfigurationDirectoryPath { get; }
        public string SystemConfigurationFilePath { get; }
        public string CachePath { get; }
        public string TempDirectory { get; }
        public string VirtualDataPath { get; }
    }

    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }
        catch
        {
            // Best-effort cleanup
        }

        _disposed = true;

        base.Dispose();
    }
}
