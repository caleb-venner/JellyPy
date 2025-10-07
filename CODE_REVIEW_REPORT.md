# Code Review Report - Jellypy Plugin
**Date:** October 7, 2025
**Reviewer:** AI Assistant

## Executive Summary
✅ **Overall Status:** Good - No critical issues found
⚠️ **Action Items:** 5 new configuration properties added but not yet implemented in business logic
📝 **Recommendations:** Implement business logic for new toggles, remove legacy file

---

## 1. Configuration Properties Status

### ✅ Completed & Implemented
All properties are properly defined in `PluginConfiguration.cs`, have UI controls in `configPage.html`, and are implemented in business logic:

- `EnableNativeSonarrIntegration` ✅
- `EnableNativeRadarrIntegration` ✅
- `SonarrUrl` / `SonarrApiKey` ✅
- `RadarrUrl` / `RadarrApiKey` ✅
- `EpisodeDownloadBuffer` ✅ (Used in ArrIntegrationService line 139)
- `AutoSearchEpisodes` ✅ (Used in ArrIntegrationService line 180)
- `MonitorFutureEpisodes` ✅ (Used in ArrIntegrationService line 186)
- `UnmonitorWatchedMovies` ✅ (Used in ArrIntegrationService line 207)
- `SkipEpisodesWithFiles` ✅ (Used in ArrIntegrationService line 153)

### ⚠️ Defined But NOT Yet Implemented in Business Logic
These 5 properties were just added and need business logic implementation:

#### **Sonarr Properties:**
1. **`UnmonitorWatchedEpisodes`** (bool, default: true)
   - **Location:** PluginConfiguration.cs line 154
   - **Purpose:** Control whether to unmonitor episodes after watching
   - **Current Issue:** Not referenced in `ArrIntegrationService.cs`
   - **Implementation Needed:** Line 130 of ArrIntegrationService should check this before unmonitoring current episode

2. **`MonitorOnlyCurrentSeason`** (bool, default: false)
   - **Location:** PluginConfiguration.cs line 162
   - **Purpose:** Only monitor episodes in the same season being watched
   - **Current Issue:** Not referenced in `ArrIntegrationService.cs`
   - **Implementation Needed:** Filter `nextEpisodes` around line 140 to only include episodes matching `currentSeasonNumber`

3. **`MinimumEpisodeBuffer`** (int, default: 2)
   - **Location:** PluginConfiguration.cs line 169
   - **Purpose:** Minimum unwatched episodes to maintain; increase buffer if below threshold
   - **Current Issue:** Not referenced in `ArrIntegrationService.cs`
   - **Implementation Needed:** Query Sonarr for unwatched episode count, dynamically adjust buffer if below minimum

#### **Radarr Properties:**
4. **`UnmonitorOnlyIfWatched`** (bool, default: false)
   - **Location:** PluginConfiguration.cs line 177
   - **Purpose:** Only unmonitor movies if watched percentage exceeds threshold
   - **Current Issue:** Not referenced in `ArrIntegrationService.cs` or `PlaybackStopHandler.cs`
   - **Implementation Needed:** Requires PlaybackStop event handling with percentage check

5. **`UnmonitorAfterUpgrade`** (bool, default: false)
   - **Location:** PluginConfiguration.cs line 191
   - **Purpose:** Only unmonitor movies that have reached their quality cutoff
   - **Current Issue:** Not referenced in `ArrIntegrationService.cs`
   - **Implementation Needed:** Query Radarr for quality profile cutoff, compare to current file quality

**Related Property:**
- **`MinimumWatchPercentage`** (int, default: 90)
  - **Location:** PluginConfiguration.cs line 183
  - **Purpose:** Used with `UnmonitorOnlyIfWatched`
  - **Current Issue:** Not referenced anywhere in code

---

## 2. Code Quality Assessment

### ✅ Strengths
- **Clean Architecture:** Proper dependency injection throughout
- **Event Handling:** No duplicate event registrations found
- **Service Layer:** Well-structured with clear interfaces (ISonarrService, IRadarrService, IArrIntegrationService)
- **Error Handling:** Comprehensive try-catch blocks with logging
- **Naming Consistency:** Property names match across C#, HTML, and JavaScript
- **Recent Fix:** `MonitorOnlyNextSeason` renamed to `MonitorOnlyCurrentSeason` for clarity ✅

### ⚠️ Minor Issues

#### 1. Legacy File
**File:** `Jellyfin.Plugin.Jellypy/download.py`
- **Issue:** Legacy Python script still present in project root
- **Impact:** Low (not referenced anywhere)
- **Recommendation:** Remove or move to `examples/` directory if keeping for reference

#### 2. Incomplete Feature Implementation
**Files:** `ArrIntegrationService.cs`, `PlaybackStopHandler.cs`
- **Issue:** 5 new configuration properties are not used in business logic
- **Impact:** Medium - Features won't work until implemented
- **Status:** This is expected - UI and configuration are done, awaiting logic implementation

#### 3. PlaybackStopHandler Missing Radarr Integration
**File:** `PlaybackStopHandler.cs`
- **Issue:** Only calls `_scriptExecutionService`, does not call `_arrIntegrationService`
- **Impact:** Medium - Radarr movies can only be unmonitored on PlaybackStart, not PlaybackStop
- **Required For:** `UnmonitorOnlyIfWatched` feature (needs watch percentage from PlaybackStop)
- **Recommendation:** Add `IArrIntegrationService` dependency and call it for movie processing

---

## 3. Architecture Validation

### Event Flow ✅
```
PlaybackStart Event:
  EnhancedEntryPoint.OnPlaybackStart
  └─> PlaybackStartHandler.HandleAsync
      ├─> ArrIntegrationService.ProcessPlaybackStartAsync ✅
      │   ├─> ProcessEpisodeAsync (TV shows)
      │   └─> ProcessMovieAsync (Movies)
      └─> ScriptExecutionService.ExecuteScriptsAsync ✅

PlaybackStop Event:
  EnhancedEntryPoint.OnPlaybackStopped
  └─> PlaybackStopHandler.HandleAsync
      └─> ScriptExecutionService.ExecuteScriptsAsync ✅
          (ArrIntegrationService NOT called) ⚠️
```

### Service Registration ✅
```csharp
// PluginServiceRegistrator.cs - All properly registered
serviceCollection.AddHostedService<EnhancedEntryPoint>();
serviceCollection.AddSingleton<IScriptExecutionService, ScriptExecutionService>();
serviceCollection.AddSingleton<ISonarrService, SonarrService>();
serviceCollection.AddSingleton<IRadarrService, RadarrService>();
serviceCollection.AddSingleton<IArrIntegrationService, ArrIntegrationService>();
serviceCollection.AddTransient<PlaybackStartHandler>();
serviceCollection.AddTransient<PlaybackStopHandler>();
```

### No Duplicate Registrations ✅
- EnhancedEntryPoint: Single IHostedService registration
- Event Handlers: Clean transient registrations
- No legacy IEventConsumer registrations (removed in previous fix)

---

## 4. Consistency Checks

### Property Naming ✅
All properties match exactly across:
- C# property names in `PluginConfiguration.cs`
- HTML element IDs in `configPage.html`
- JavaScript load/save functions
- No typos or case mismatches found

### Configuration Defaults ✅
All default values match between:
- Constructor initialization
- JavaScript fallback values (`|| default`)
- UI descriptions

### API Surface ✅
- `ISonarrService`: 9 methods defined and implemented
- `IRadarrService`: 5 methods defined and implemented
- `IArrIntegrationService`: 1 method defined and implemented
- No missing implementations found

---

## 5. Recommendations

### Priority 1: Implement Business Logic for New Properties
**Time Estimate:** 2-3 hours

1. **UnmonitorWatchedEpisodes** (Simple)
   ```csharp
   // In ArrIntegrationService.ProcessEpisodeAsync around line 130
   if (config.UnmonitorWatchedEpisodes)
   {
       await _sonarrService.SetEpisodeMonitoredAsync(currentEp.Id, false);
   }
   ```

2. **MonitorOnlyCurrentSeason** (Simple)
   ```csharp
   // In ArrIntegrationService.ProcessEpisodeAsync around line 140
   var nextEpisodes = orderedEpisodes
       .Skip(currentEpisodeIndex + 1)
       .Where(e => !config.MonitorOnlyCurrentSeason || e.SeasonNumber == currentSeasonNumber)
       .Take(episodeBuffer)
       .ToList();
   ```

3. **MinimumEpisodeBuffer** (Moderate)
   ```csharp
   // In ArrIntegrationService.ProcessEpisodeAsync before line 139
   var unwatchedCount = orderedEpisodes
       .Skip(currentEpisodeIndex + 1)
       .Count(e => !e.HasFile);

   var adjustedBuffer = Math.Max(episodeBuffer, config.MinimumEpisodeBuffer - unwatchedCount);
   ```

4. **UnmonitorOnlyIfWatched** (Moderate - requires PlaybackStopHandler changes)
   - Add `IArrIntegrationService` to `PlaybackStopHandler` constructor
   - Calculate watch percentage from `eventArgs.PlaybackPositionTicks` and item runtime
   - Only call `ProcessMovieAsync` if percentage >= `MinimumWatchPercentage`

5. **UnmonitorAfterUpgrade** (Complex - requires new Radarr API methods)
   - Add `GetMovieQualityProfileAsync` method to `IRadarrService`
   - Add `GetMovieFileQualityAsync` method to `IRadarrService`
   - Compare current file quality to profile cutoff before unmonitoring

### Priority 2: Remove Legacy File
**Time Estimate:** 5 minutes

```bash
# Option 1: Delete entirely
rm /Users/calebvenner/projects/jellypy/Jellyfin.Plugin.Jellypy/download.py

# Option 2: Move to examples
mv /Users/calebvenner/projects/jellypy/Jellyfin.Plugin.Jellypy/download.py \
   /Users/calebvenner/projects/jellypy/examples/legacy_download.py
```

### Priority 3: Add PlaybackStop Integration for Radarr
**Time Estimate:** 30 minutes

Update `PlaybackStopHandler.cs` to support percentage-based movie unmonitoring:
```csharp
private readonly IArrIntegrationService _arrIntegrationService;

public async Task HandleAsync(PlaybackStopEventArgs eventArgs)
{
    // Calculate watch percentage
    var percentage = CalculateWatchPercentage(eventArgs);

    // Call Arr integration for movies
    if (eventArgs.Item is Movie)
    {
        await _arrIntegrationService.ProcessPlaybackStopAsync(
            eventArgs.Item, percentage);
    }

    // Execute scripts
    await _scriptExecutionService.ExecuteScriptsAsync(eventData);
}
```

---

## 6. Testing Checklist

### Configuration Persistence ✅
- [x] Properties save to XML correctly
- [x] Properties load from XML correctly
- [x] UI checkboxes bind properly
- [x] Default values applied on first run

### Current Features (Working) ✅
- [x] Sonarr episode monitoring
- [x] Sonarr automatic episode search
- [x] Sonarr future episode monitoring
- [x] Radarr movie unmonitoring (on PlaybackStart)
- [x] Skip episodes with existing files

### New Features (Need Implementation) ⚠️
- [ ] Unmonitor watched episodes toggle
- [ ] Monitor only current season
- [ ] Minimum episode buffer enforcement
- [ ] Unmonitor only if watched (percentage-based)
- [ ] Unmonitor after quality cutoff reached

---

## 7. Summary

### Critical Issues
**None** ✅

### High Priority
1. Implement business logic for 5 new configuration properties

### Medium Priority
1. Add Radarr integration to PlaybackStopHandler for percentage-based unmonitoring
2. Remove legacy `download.py` file

### Low Priority
1. Add unit tests for new configuration logic (once implemented)

### Code Quality Score
**8.5/10** - Well-structured, clean architecture, minor incompleteness expected during feature development

---

## Appendix: File Reference

### Core Files
- `PluginConfiguration.cs` - All configuration properties defined ✅
- `ArrIntegrationService.cs` - Business logic (needs 5 new implementations) ⚠️
- `PlaybackStartHandler.cs` - Handles TV/movie on play start ✅
- `PlaybackStopHandler.cs` - Handles stop event (needs Radarr integration) ⚠️
- `EnhancedEntryPoint.cs` - Event subscription management ✅
- `PluginServiceRegistrator.cs` - Dependency injection setup ✅
- `configPage.html` - UI configuration page ✅

### Service Interfaces
- `ISonarrService` - Sonarr API operations ✅
- `IRadarrService` - Radarr API operations ✅
- `IArrIntegrationService` - Orchestration layer ✅
- `IScriptExecutionService` - Script execution ✅

### Build Status
- ✅ 0 compilation errors
- ⚠️ 50 style/analysis warnings (non-blocking)
