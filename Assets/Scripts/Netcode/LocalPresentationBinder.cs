using System.Linq;
using PinePie.SimpleJoystick;
using Unity.Netcode;
using UnityEngine;

public class LocalPresentationBinder : MonoBehaviour
{
    [Header("B5 本地相机/HUD/摇杆归属")]
    [SerializeField] private bool enableB5Binding = true;
    [SerializeField] private float refreshInterval = 0.25f;
    [SerializeField] private float ownerLossGraceSeconds = 2.0f;

    private float nextRefreshTime;
    private NetworkObject currentLocalPlayerObject;
    private bool loggedWaitingOwner;
    private float nextMissingReferenceLogTime;
    private float localPlayerMissingSince = -1f;
    private static LocalPresentationBinder activeInstance;

    private void Awake()
    {
        Debug.Log("[B5] LocalPresentationBinder active.");
    }

    private void OnEnable()
    {
        activeInstance = this;
    }

    private void OnDisable()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Update()
    {
        if (!enableB5Binding || Application.isBatchMode) return;
        if (Time.unscaledTime < nextRefreshTime) return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        RefreshBinding();
    }

    private void RefreshBinding()
    {
        NetworkObject[] players = FindObjectsOfType<NetworkObject>(true);
        NetworkObject localPlayerObject = ResolveLocalPlayerObject(players);

        // 兜底：如果查询链路瞬时拿不到本地玩家，优先沿用当前已绑定对象，避免相机/摇杆闪断。
        if (localPlayerObject == null && currentLocalPlayerObject != null && currentLocalPlayerObject && currentLocalPlayerObject.IsSpawned)
        {
            localPlayerObject = currentLocalPlayerObject;
        }

        NetworkPlayerController localPlayerController = localPlayerObject != null ? localPlayerObject.GetComponent<NetworkPlayerController>() : null;
        Player localStatsPlayer = localPlayerObject != null ? localPlayerObject.GetComponent<Player>() : null;

        if (localPlayerObject == null)
        {
            if (localPlayerMissingSince < 0f)
            {
                localPlayerMissingSince = Time.unscaledTime;
            }

            if (!loggedWaitingOwner)
            {
                loggedWaitingOwner = true;
                LogWaitingReason(players);
            }

            // 给一个短暂宽限窗口，避免网络对象重建/场景切换瞬时抖动导致解绑。
            if (currentLocalPlayerObject != null && Time.unscaledTime - localPlayerMissingSince < Mathf.Max(0.1f, ownerLossGraceSeconds))
            {
                return;
            }

            if (currentLocalPlayerObject != null)
            {
                currentLocalPlayerObject = null;
                ApplyCameraBinding(null);
                ApplyHudBinding(null);
                int joystickCount = ApplyJoystickBinding(false);
                Debug.Log($"[B5] Local presentation unbound (owner lost timeout). joystickCount={joystickCount}");
            }
            return;
        }

        localPlayerMissingSince = -1f;
        BindLocalPresentation(localPlayerObject, localPlayerController, localStatsPlayer, source: "poll");
    }

    public static void NotifyLocalOwnerSpawned(NetworkObject localPlayerObject)
    {
        if (activeInstance == null || localPlayerObject == null) return;
        activeInstance.TryBindFromSpawnEvent(localPlayerObject);
    }

    private void TryBindFromSpawnEvent(NetworkObject localPlayerObject)
    {
        if (!enableB5Binding || Application.isBatchMode) return;
        if (!localPlayerObject.IsSpawned || !localPlayerObject.IsPlayerObject) return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsClient && localPlayerObject.OwnerClientId != networkManager.LocalClientId)
        {
            return;
        }

        NetworkPlayerController localPlayerController = localPlayerObject.GetComponent<NetworkPlayerController>();
        Player localStatsPlayer = localPlayerObject.GetComponent<Player>();
        BindLocalPresentation(localPlayerObject, localPlayerController, localStatsPlayer, source: "owner-spawn-event");
    }

    private void BindLocalPresentation(NetworkObject localPlayerObject, NetworkPlayerController localPlayerController, Player localStatsPlayer, string source)
    {
        loggedWaitingOwner = false;
        bool isNewOwnerBinding = localPlayerObject != currentLocalPlayerObject;
        currentLocalPlayerObject = localPlayerObject;

        int cameraBoundCount = ApplyCameraBinding(localPlayerObject.transform);
        ApplyHudBinding(localStatsPlayer);
        int joystickCount = ApplyJoystickBinding(true);
        ApplyGameManagerBinding(localPlayerObject.transform);

        if ((cameraBoundCount <= 0 || joystickCount <= 0) && Time.unscaledTime >= nextMissingReferenceLogTime)
        {
            nextMissingReferenceLogTime = Time.unscaledTime + 2f;
            Debug.LogWarning($"[B5] Missing presentation references => cameraBoundCount={cameraBoundCount}, joystickCount={joystickCount}");
        }

        if (!isNewOwnerBinding) return;

        string owner = localPlayerObject.OwnerClientId.ToString();
        bool hasController = localPlayerController != null;
        bool hasPlayerStats = localStatsPlayer != null;
        string targetName = localPlayerObject != null ? localPlayerObject.name : "None";
        int disabledLegacyPlayers = DisableLegacyScenePlayers(localPlayerObject);
        Debug.Log($"[B5] Local presentation bind => owner={owner}, source={source}, target={targetName}, hasController={hasController}, cameraBoundCount={cameraBoundCount}, hudPlayerBound={hasPlayerStats}, joystickCount={joystickCount}, legacyDisabled={disabledLegacyPlayers}, joystickVisible=True");
    }

    private static NetworkObject ResolveLocalPlayerObject(NetworkObject[] players)
    {
        if (players == null || players.Length == 0) return null;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening && networkManager.IsClient)
        {
            NetworkObject localByApi = networkManager.SpawnManager != null ? networkManager.SpawnManager.GetLocalPlayerObject() : null;
            if (localByApi != null && localByApi.IsSpawned) return localByApi;

            ulong localClientId = networkManager.LocalClientId;
            NetworkObject byOwner = players.FirstOrDefault(p =>
                p != null && p.IsSpawned && p.IsPlayerObject && p.OwnerClientId == localClientId);
            if (byOwner != null) return byOwner;
        }

        return players.FirstOrDefault(p => p != null && p.IsSpawned && p.IsPlayerObject && p.IsOwner);
    }

    private static void LogWaitingReason(NetworkObject[] players)
    {
        int total = players?.Length ?? 0;
        int spawned = players?.Count(p => p != null && p.IsSpawned) ?? 0;
        int ownerTrue = players?.Count(p => p != null && p.IsOwner) ?? 0;
        int playerObjects = players?.Count(p => p != null && p.IsSpawned && p.IsPlayerObject) ?? 0;

        string localClient = "N/A";
        string listening = "false";
        string connected = "false";
        string role = "Unknown";
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            localClient = networkManager.LocalClientId.ToString();
            listening = networkManager.IsListening.ToString();
            connected = networkManager.IsConnectedClient.ToString();
            role = networkManager.IsServer ? "Server" : (networkManager.IsClient ? "Client" : "None");
        }

        Debug.Log(
            $"[B5] Waiting local owner player... role={role}, listening={listening}, connected={connected}, localClient={localClient}, candidates={total}, spawned={spawned}, playerObjects={playerObjects}, isOwnerTrue={ownerTrue}");
    }

    private static int ApplyCameraBinding(Transform target)
    {
        int bound = 0;
        CameraFollow[] follows = FindObjectsOfType<CameraFollow>(true);
        foreach (CameraFollow follow in follows)
        {
            if (follow == null) continue;
            follow.SetTarget(target);
            bound++;
        }

        if (bound > 0) return bound;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            CameraFollow fallbackFollow = mainCamera.GetComponent<CameraFollow>();
            if (fallbackFollow == null)
            {
                fallbackFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
                fallbackFollow.offset = new Vector3(-20f, 50f, -20f);
                fallbackFollow.smoothSpeed = 0.125f;
                Debug.Log("[B5] Fallback CameraFollow added to MainCamera.");
            }

            fallbackFollow.SetTarget(target);
            bound = 1;
        }

        return bound;
    }

    private static void ApplyHudBinding(Player localStatsPlayer)
    {
        GameHUD[] huds = FindObjectsOfType<GameHUD>(true);
        foreach (GameHUD hud in huds)
        {
            if (hud == null) continue;
            hud.BindPlayer(localStatsPlayer);
        }

        PlayerHealthBar[] bars = FindObjectsOfType<PlayerHealthBar>(true);
        foreach (PlayerHealthBar bar in bars)
        {
            if (bar == null) continue;
            bar.BindPlayer(localStatsPlayer);
        }
    }

    private static int ApplyJoystickBinding(bool visible)
    {
        int count = 0;
        JoystickController[] joysticks = FindObjectsOfType<JoystickController>(true);
        foreach (JoystickController joystick in joysticks)
        {
            if (joystick == null) continue;
            joystick.gameObject.SetActive(visible);
            joystick.enabled = visible;
            count++;
        }

        return count;
    }

    private static void ApplyGameManagerBinding(Transform target)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.playerTransform = target;
    }

    private static int DisableLegacyScenePlayers(NetworkObject localPlayerObject)
    {
        if (localPlayerObject == null) return 0;

        Player[] players = FindObjectsOfType<Player>(true);
        int disabled = 0;
        foreach (Player player in players)
        {
            if (player == null) continue;

            GameObject go = player.gameObject;
            if (go == null) continue;
            if (go == localPlayerObject.gameObject) continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            if (go.hideFlags != HideFlags.None) continue;

            NetworkObject maybeNetworkObject = go.GetComponent<NetworkObject>();
            if (maybeNetworkObject != null && maybeNetworkObject.IsSpawned) continue;
            if (!go.activeSelf) continue;

            go.SetActive(false);
            disabled++;
        }

        return disabled;
    }
}
