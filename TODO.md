# TODO

## Implement Native Discord Notifications

- Group episodes.
- Cut out middleman in Notifiarr type setups.

## Strange directory creation attempt

[20:17:50] [ERR] [9] Jellyfin.Plugin.JellyPy.EnhancedEntryPoint: Failed to create scripts directory
System.UnauthorizedAccessException: Access to the path '/usr/lib/jellyfin/bin/scripts' is denied.
 ---> System.IO.IOException: Permission denied
   --- End of inner exception stack trace ---
   at System.IO.FileSystem.CreateDirectory(String fullPath, UnixFileMode unixCreateMode)
   at System.IO.Directory.CreateDirectory(String path)
   at Jellyfin.Plugin.JellyPy.EnhancedEntryPoint.StartAsync(CancellationToken cancellationToken)