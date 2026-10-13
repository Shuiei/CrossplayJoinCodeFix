# Changelog

## 1.1.1

- Mod page only: a Buy Me a Coffee button. No change to the mod.

## 1.1.0

- The plugin's id is now `Tie.CrossplayJoinCodeFix`, so its settings are in
  `BepInEx/config/Tie.CrossplayJoinCodeFix.cfg`. An existing `local.crossplayjoincodefix.cfg` is
  moved to the new name on the first start, with its settings.

## 1.0.0

- First release: guards the PlayFab join-code check of crossplay servers (complete answer: vanilla;
  only this server's lobby returned: activated; otherwise retried every `RetrySeconds` until
  `TimeoutSeconds`, then a new join code is registered).
