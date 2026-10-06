# CrossplayJoinCodeFix

A **server-side** fix for Valheim **crossplay** dedicated servers that run fine but show as
**offline**: nobody can find or join them, although the world is up.

## The problem

When a crossplay server starts, it registers a join code with PlayFab and asks PlayFab whether the
code is unique. Sometimes PlayFab answers with part of the data missing (no lobby list, or a lobby
without its owner). The game does not expect that: it throws a `NullReferenceException` in
`ZPlayFabMatchmaking.OnCheckJoinCodeSuccess` and never activates the lobby. The usual workaround is
to stop the server for a while and try again.

If your server log shows that exception right after start and the server stays invisible, this
plugin is for you.

## What it does

It checks PlayFab's answer before the game reads it:

- **A complete answer**: nothing changes, the game's own code runs.
- **The only lobby returned is the one this server just created**: the lobby is activated, as the
  game would have done.
- **Otherwise**: the check is repeated every few seconds until PlayFab answers properly. If it still
  has not after the timeout, the server registers a new join code and waits again.

The log shows `CrossplayJoinCodeFix 1.0.0: join-code check guarded (...)` once it runs.

## Install

On the **dedicated server only**, with BepInEx: install it with a mod manager's server profile, or
copy `CrossplayJoinCodeFix.dll` into the server's `BepInEx/plugins`. Players need nothing.

## Settings

`BepInEx/config/local.crossplayjoincodefix.cfg`, section `[JoinCode]`:

| Setting | Default | Meaning |
|---|---|---|
| `TimeoutSeconds` | `30` | How long to keep re-checking an incomplete answer before registering a new join code. |
| `RetrySeconds` | `2` | Time between two checks while waiting. |

## Notes

- Tested on a crossplay test server with simulated incomplete PlayFab answers. The last-resort
  path (a new join code after the timeout) has not yet been seen in a real outage: reports welcome.
- It only touches the join-code check; servers without crossplay are not affected.

Source and issues: [github.com/Shuiei/CrossplayJoinCodeFix](https://github.com/Shuiei/CrossplayJoinCodeFix)
