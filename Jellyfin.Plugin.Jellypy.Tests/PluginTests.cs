using Jellyfin.Plugin.Jellypy.Configuration;
using Jellyfin.Plugin.Jellypy.Events;
using Xunit;

namespace Jellyfin.Plugin.Jellypy.Tests;

/// <summary>
/// Basic unit tests for plugin configuration models.
/// </summary>
public class ConfigurationModelTests
{
    [Fact]
    public void ScriptSetting_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var scriptSetting = new ScriptSetting();

        // Assert
        Assert.True(scriptSetting.Enabled);
        Assert.NotNull(scriptSetting.Triggers);
        Assert.NotNull(scriptSetting.Conditions);
        Assert.NotNull(scriptSetting.DataAttributes);
        Assert.Equal(ScriptExecutorType.Python, scriptSetting.Execution.ExecutorType);
    }

    [Fact]
    public void ScriptSetting_WithName_ReturnsCorrectDisplayName()
    {
        // Arrange
        var scriptSetting = new ScriptSetting
        {
            Name = "Test Script"
        };

        // Act & Assert
        Assert.Equal("Test Script", scriptSetting.Name);
    }

    [Fact]
    public void ExecutionCondition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var condition = new ExecutionCondition();

        // Assert
        Assert.Equal(ConditionOperator.Equals, condition.Operator);
        Assert.False(condition.CaseSensitive);
        Assert.Equal(string.Empty, condition.Field);
        Assert.Equal(string.Empty, condition.Value);
    }

    [Fact]
    public void ScriptDataElement_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dataAttribute = new ScriptDataElement();

        // Assert
        Assert.Equal(DataAttributeFormat.String, dataAttribute.Format);
        Assert.False(dataAttribute.Required);
        Assert.Equal(string.Empty, dataAttribute.Name);
        Assert.Equal(string.Empty, dataAttribute.SourceField);
        Assert.Equal(string.Empty, dataAttribute.DefaultValue);
    }

    [Fact]
    public void ScriptExecution_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var execution = new ScriptExecution();

        // Assert
        Assert.Equal(ScriptExecutorType.Python, execution.ExecutorType);
        Assert.Equal("/usr/bin/python3", execution.ExecutablePath);
        Assert.Equal(300, execution.TimeoutSeconds);
    }

    [Fact]
    public void ScriptSetting_CanAddTriggers()
    {
        // Arrange
        var scriptSetting = new ScriptSetting();

        // Act
        scriptSetting.Triggers.Add(EventType.PlaybackStart);
        scriptSetting.Triggers.Add(EventType.PlaybackStop);

        // Assert
        Assert.Equal(2, scriptSetting.Triggers.Count);
        Assert.Contains(EventType.PlaybackStart, scriptSetting.Triggers);
        Assert.Contains(EventType.PlaybackStop, scriptSetting.Triggers);
    }

    [Fact]
    public void ScriptSetting_CanAddConditions()
    {
        // Arrange
        var scriptSetting = new ScriptSetting();
        var condition = new ExecutionCondition
        {
            Field = "ItemName",
            Operator = ConditionOperator.Contains,
            Value = "Movie"
        };

        // Act
        scriptSetting.Conditions.Add(condition);

        // Assert
        Assert.Single(scriptSetting.Conditions);
        Assert.Equal("ItemName", scriptSetting.Conditions[0].Field);
        Assert.Equal(ConditionOperator.Contains, scriptSetting.Conditions[0].Operator);
        Assert.Equal("Movie", scriptSetting.Conditions[0].Value);
    }
}

/// <summary>
/// Basic tests for enums and constants.
/// </summary>
public class EnumTests
{
    [Fact]
    public void EventType_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(EventType), EventType.PlaybackStart));
        Assert.True(Enum.IsDefined(typeof(EventType), EventType.PlaybackStop));
        Assert.True(Enum.IsDefined(typeof(EventType), EventType.ItemAdded));
    }

    [Fact]
    public void ConditionOperator_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(ConditionOperator), ConditionOperator.Equals));
        Assert.True(Enum.IsDefined(typeof(ConditionOperator), ConditionOperator.NotEquals));
        Assert.True(Enum.IsDefined(typeof(ConditionOperator), ConditionOperator.Contains));
        Assert.True(Enum.IsDefined(typeof(ConditionOperator), ConditionOperator.Regex));
    }

    [Fact]
    public void ScriptExecutorType_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(ScriptExecutorType), ScriptExecutorType.Python));
        Assert.True(Enum.IsDefined(typeof(ScriptExecutorType), ScriptExecutorType.PowerShell));
        Assert.True(Enum.IsDefined(typeof(ScriptExecutorType), ScriptExecutorType.Bash));
        Assert.True(Enum.IsDefined(typeof(ScriptExecutorType), ScriptExecutorType.NodeJs));
        Assert.True(Enum.IsDefined(typeof(ScriptExecutorType), ScriptExecutorType.Binary));
    }

    [Fact]
    public void DataAttributeFormat_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(DataAttributeFormat), DataAttributeFormat.String));
        Assert.True(Enum.IsDefined(typeof(DataAttributeFormat), DataAttributeFormat.Json));
    }
}
