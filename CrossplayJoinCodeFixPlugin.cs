using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlayFab.MultiplayerModels;
using UnityEngine;

namespace CrossplayJoinCodeFix;

// Server-only fix for crossplay servers that appear offline. After registering its join code, the
// server asks PlayFab whether the code is unique (ZPlayFabMatchmaking.OnCheckJoinCodeSuccess). When
// PlayFab answers with part of the data missing (no lobby list, no lobby owner, or no identity for the
// server yet), vanilla throws a NullReferenceException there and never activates the lobby: the world
// runs, but nobody can join. This plugin checks the answer first:
//   - complete answer: the game's own code runs, nothing changes;
//   - the only lobby returned is the one this server just created: it is activated, as vanilla would;
//   - otherwise the check is repeated every RetrySeconds, for up to TimeoutSeconds; after that a new
//     join code is registered (the game's own path for a taken code) and the wait starts again.
[BepInPlugin(Guid, "CrossplayJoinCodeFix", Version)]
public sealed class CrossplayJoinCodeFixPlugin : BaseUnityPlugin
{
	public const string Guid = "local.crossplayjoincodefix";

	public const string Version = "1.0.0";

	internal static ConfigEntry<float> TimeoutSeconds;

	internal static ConfigEntry<float> RetrySeconds;

	internal static ManualLogSource Log;

	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		TimeoutSeconds = Config.Bind("JoinCode", "TimeoutSeconds", 30f, "How long to keep re-checking an incomplete PlayFab answer before registering a new join code.");
		RetrySeconds = Config.Bind("JoinCode", "RetrySeconds", 2f, "Delay between two join-code checks while waiting.");
		_harmony = new Harmony(Guid);
		_harmony.PatchAll(typeof(CheckJoinCodePatch));
		Logger.LogInfo($"CrossplayJoinCodeFix {Version}: join-code check guarded (timeout {TimeoutSeconds.Value:0} s, retry every {RetrySeconds.Value:0.#} s)");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}

[HarmonyPatch(typeof(ZPlayFabMatchmaking), "OnCheckJoinCodeSuccess")]
internal static class CheckJoinCodePatch
{
	private static readonly FieldInfo ServerData = AccessTools.Field(typeof(ZPlayFabMatchmaking), "m_serverData");

	private static readonly FieldInfo RetryIn = AccessTools.Field(typeof(ZPlayFabMatchmaking), "m_retryIn");

	private static readonly MethodInfo ActivateSession = AccessTools.Method(typeof(ZPlayFabMatchmaking), "ActivateSession");

	private static readonly MethodInfo OnSessionUpdated = AccessTools.Method(typeof(ZPlayFabMatchmaking), "OnSessionUpdated");

	private static readonly Type StateType = AccessTools.Inner(typeof(ZPlayFabMatchmaking), "State");

	// Realtime at which the current wait started; negative when not waiting.
	private static float _waitingSince = -1f;

	private static bool Prefix(ZPlayFabMatchmaking __instance, FindLobbiesResult result)
	{
		try
		{
			string myEntity = PlayFabManager.instance?.Entity?.Id;
			bool complete = result?.Lobbies != null && myEntity != null && result.Lobbies.TrueForAll(l => l != null && l.Owner?.Id != null);
			if (complete)
			{
				if (_waitingSince >= 0f)
				{
					CrossplayJoinCodeFixPlugin.Log.LogInfo($"PlayFab answered properly after {Time.realtimeSinceStartup - _waitingSince:0} s");
					_waitingSince = -1f;
				}
				return true;
			}
			// Exactly our own lobby: the join code is unique and ours, owner field or not.
			string myLobby = (ServerData.GetValue(__instance) as PlayFabMatchmakingServerData)?.lobbyId;
			if (result?.Lobbies != null && result.Lobbies.Count == 1 && myLobby != null && result.Lobbies[0]?.LobbyId == myLobby)
			{
				Warn("PlayFab answer had no owner, but the lobby is this server's own: activating it");
				_waitingSince = -1f;
				ActivateSession.Invoke(__instance, null);
				return false;
			}
			if (_waitingSince < 0f)
			{
				_waitingSince = Time.realtimeSinceStartup;
			}
			float waited = Time.realtimeSinceStartup - _waitingSince;
			string what = result?.Lobbies == null ? "no lobby list" : myEntity == null ? "no server identity yet" : "a lobby without owner";
			if (waited < CrossplayJoinCodeFixPlugin.TimeoutSeconds.Value)
			{
				Warn($"PlayFab join-code check came back incomplete ({what}); checking again ({waited:0}/{CrossplayJoinCodeFixPlugin.TimeoutSeconds.Value:0} s)");
				// The game's own retry: ZPlayFabMatchmaking.Update calls CheckJoinCodeIsUnique when it runs out.
				RetryIn.SetValue(__instance, Mathf.Max(0.5f, CrossplayJoinCodeFixPlugin.RetrySeconds.Value));
				return false;
			}
			CrossplayJoinCodeFixPlugin.Log.LogWarning($"No usable PlayFab answer after {waited:0} s; registering a new join code");
			_waitingSince = -1f;
			OnSessionUpdated.Invoke(__instance, new[] { Enum.Parse(StateType, "RegenerateJoinCode") });
			return false;
		}
		catch (Exception ex)
		{
			// Never break the game's own handling because of this patch.
			CrossplayJoinCodeFixPlugin.Log.LogError($"CrossplayJoinCodeFix: {ex}");
			return true;
		}
	}

	private static void Warn(string message) => CrossplayJoinCodeFixPlugin.Log.LogWarning(message);
}
