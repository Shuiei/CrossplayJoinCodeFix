# CrossplayJoinCodeFix

Server-only BepInEx plugin for Valheim crossplay servers that show as offline.

After registering its join code, the server asks PlayFab whether the code is unique
(`ZPlayFabMatchmaking.OnCheckJoinCodeSuccess`). When PlayFab answers with part of the data missing
(no lobby list, or a lobby without owner), vanilla throws a `NullReferenceException` there and never
activates the lobby: the world runs, but nobody can join. This plugin checks the answer first:

- complete answer: the game's own code runs, nothing changes;
- the only lobby returned is the one this server just created: it is activated, as vanilla would;
- otherwise the check is repeated every `RetrySeconds` (default 2) for up to `TimeoutSeconds`
  (default 30); after that a new join code is registered and the wait starts again.

Settings: `BepInEx/config/local.crossplayjoincodefix.cfg`. Clients do not need the plugin.

Build: `dotnet build -c Release` (references the game's managed DLLs and BepInEx from the paths in
the .csproj), then copy `CrossplayJoinCodeFix.dll` to the server's `BepInEx/plugins/`.
