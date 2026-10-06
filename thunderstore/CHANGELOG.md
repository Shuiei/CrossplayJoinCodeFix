# Changelog

## 1.0.0

- First release: guards the PlayFab join-code check of crossplay servers (complete answer: vanilla;
  only this server's lobby returned: activated; otherwise retried every `RetrySeconds` until
  `TimeoutSeconds`, then a new join code is registered).
