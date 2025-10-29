#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Jellyfin.Plugin.JellyPy.Events;

namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Represents a script setting configuration.
/// </summary>
public class ScriptSetting
{
    /// <summary>
    /// Gets or sets the unique identifier for this script setting.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of this script setting.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of this script setting.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this script setting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the list of event types that trigger this script.
    /// </summary>
    public Collection<EventType> Triggers { get; } = new();

    /// <summary>
    /// Gets the execution conditions for this script.
    /// </summary>
    public Collection<ExecutionCondition> Conditions { get; } = new();

    /// <summary>
    /// Gets or sets the script execution configuration.
    /// </summary>
    public ScriptExecution Execution { get; set; } = new();

    /// <summary>
    /// Gets the data attributes to pass to the script.
    /// </summary>
    public Collection<ScriptDataElement> DataAttributes { get; } = new();

    /// <summary>
    /// Gets or sets the priority of this script (lower numbers = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;
}

/// <summary>
/// Represents an execution condition for a script.
/// </summary>
public class ExecutionCondition
{
    /// <summary>
    /// Gets or sets the field to evaluate.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator for the condition.
    /// </summary>
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this condition should be case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Represents the execution configuration for a script.
/// </summary>
public class ScriptExecution
{
    /// <summary>
    /// Gets or sets the executor type (Python, PowerShell, etc.).
    /// </summary>
    public ScriptExecutorType ExecutorType { get; set; } = ScriptExecutorType.Python;

    /// <summary>
    /// Gets or sets the path to the script executable (python, powershell, etc.).
    /// </summary>
    public string ExecutablePath { get; set; } = "/usr/bin/python3";

    /// <summary>
    /// Gets or sets the name of the script file (relative to the scripts directory).
    /// The full path is resolved from AppContext.BaseDirectory/scripts.
    /// </summary>
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full path to the script file.
    /// Scripts are stored in AppContext.BaseDirectory/scripts.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ScriptPath => string.IsNullOrEmpty(ScriptName)
        ? string.Empty
        : Path.Join(PluginConfiguration.ScriptsDirectory, ScriptName);

    /// <summary>
    /// Gets or sets additional command line arguments.
    /// </summary>
    public string AdditionalArguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for script execution.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Represents a data element to pass to a script.
/// </summary>
public class ScriptDataElement
{
    /// <summary>
    /// Gets or sets the name of the attribute.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source field from the event data.
    /// </summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format for the attribute value.
    /// </summary>
    public DataAttributeFormat Format { get; set; } = DataAttributeFormat.String;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether this attribute is required.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets the default value if the source field is empty.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration of condition operators.
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// Equals comparison.
    /// </summary>
    Equals,

    /// <summary>
    /// Not equals comparison.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Contains comparison.
    /// </summary>
    Contains,

    /// <summary>
    /// Does not contain comparison.
    /// </summary>
    NotContains,

    /// <summary>
    /// Starts with comparison.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with comparison.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    Regex,

    /// <summary>
    /// Greater than comparison (for numeric values).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Less than comparison (for numeric values).
    /// </summary>
    LessThan,

    /// <summary>
    /// In list comparison.
    /// </summary>
    In,

    /// <summary>
    /// Not in list comparison.
    /// </summary>
    NotIn
}

/// <summary>
/// Enumeration of script executor types.
/// </summary>
public enum ScriptExecutorType
{
    /// <summary>
    /// Python script execution.
    /// </summary>
    Python,

    /// <summary>
    /// PowerShell script execution.
    /// </summary>
    PowerShell,

    /// <summary>
    /// Bash/shell script execution.
    /// </summary>
    Bash,

    /// <summary>
    /// Node.js script execution.
    /// </summary>
    NodeJs,

    /// <summary>
    /// Direct binary execution.
    /// </summary>
    Binary
}

/// <summary>
/// Enumeration of data attribute formats.
/// </summary>
public enum DataAttributeFormat
{
    /// <summary>
    /// Pass as string value.
    /// </summary>
    String,

    /// <summary>
    /// Pass as JSON object.
    /// </summary>
    Json,

    /// <summary>
    /// Pass as environment variable.
    /// </summary>
    Environment,

    /// <summary>
    /// Pass as command line argument.
    /// </summary>
    Argument
}
