// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Reasons>", Scope = "member", Target = "~M:Jellyfin.Plugin.Jellypy.ExecuteScript.RunScript(MediaBrowser.Controller.Library.PlaybackProgressEventArgs)~System.Threading.Tasks.Task{System.String}")]

// ========================================
// Level 1 Warning Suppressions (Safe)
// ========================================

// CA2007: ConfigureAwait warnings (28 warnings)
// Justification: ConfigureAwait(false) is not needed in ASP.NET Core/plugin contexts
// as there is no synchronization context to return to. This is standard practice for
// server-side code and Jellyfin plugins.
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Not needed in plugin context - no synchronization context", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy")]

// SA1402: Multiple types in file (13 warnings)
// Justification: Model/DTO files intentionally group related types for better organization
// and maintainability. This is a common pattern for API models, configuration classes,
// and data transfer objects that are closely related.
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Intentional grouping of related models and DTOs", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy.Services.Arr")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Intentional grouping of related configuration classes", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy.Configuration")]

// SA1200: Using directives should appear within namespace (10 warnings)
// Justification: Project uses file-scoped namespaces (C# 10+ feature) and global using directives.
// Modern C# style places using directives at the top of the file outside the namespace for better
// readability and consistency with the language evolution. This is the recommended pattern for new projects.
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should appear within a namespace declaration", Justification = "Project uses file-scoped namespaces and modern C# using directive placement", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy")]

// SA1633: File header required (2 warnings)
// Justification: File headers add clutter without providing value in this context. The project
// uses standard licensing via LICENSE file and copyright information is maintained at the
// repository level. Individual file headers would be redundant and reduce readability.
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "File headers not required - licensing handled at repository level", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy")]

// CS8632: Nullable reference type annotations (7 warnings)
// Justification: This codebase does not use nullable reference types globally.
// The warnings appear on nullable return types which are required for optional values
// in API responses from Sonarr/Radarr.
[assembly: SuppressMessage("Compiler", "CS8632:The annotation for nullable reference types should only be used in code within a '#nullable' annotations context", Justification = "Project doesn't use nullable reference types globally", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy")]

// ========================================
// Level 2 Warning Suppressions (Intentional Design)
// ========================================

// CA1819: Properties returning arrays (4 warnings)
// Justification: These are JSON DTO properties that directly deserialize from Sonarr/Radarr APIs.
// Changing to List<T> would break the JSON contract with external APIs. Arrays are appropriate
// for read-only data transfer objects that match external API schemas.
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "JSON DTOs matching external API contracts - arrays are appropriate", Scope = "namespaceanddescendants", Target = "~N:Jellyfin.Plugin.Jellypy.Services.Arr")]

// SA1649: File name should match first type name (2 warnings)
// Justification: RadarrModels.cs and SonarrModels.cs intentionally group multiple related
// model classes for better organization. The file names describe the content semantically
// rather than matching a single type name. This improves maintainability.
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1649:File name should match first type name", Justification = "Intentional grouping of related models - semantic file naming preferred", Scope = "type", Target = "~T:Jellyfin.Plugin.Jellypy.Services.Arr.RadarrMovie")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1649:File name should match first type name", Justification = "Intentional grouping of related models - semantic file naming preferred", Scope = "type", Target = "~T:Jellyfin.Plugin.Jellypy.Services.Arr.SonarrSeries")]

// SA1201: Enum should not follow class (2 warnings)
// Justification: Enums are placed after their related classes in ScriptSetting.cs for logical
// grouping and readability. The enums (ConditionOperator, DataAttributeFormat) are tightly
// coupled to the classes they follow and placing them together improves code comprehension.
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Enums placed after related classes for logical grouping and readability", Scope = "type", Target = "~T:Jellyfin.Plugin.Jellypy.Configuration.ConditionOperator")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Enums placed after related classes for logical grouping and readability", Scope = "type", Target = "~T:Jellyfin.Plugin.Jellypy.Configuration.DataAttributeFormat")]
