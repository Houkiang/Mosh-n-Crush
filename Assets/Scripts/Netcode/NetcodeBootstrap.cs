using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class NetcodeBootstrap : MonoBehaviour
{
    private const string BootstrapObjectName = "[B1] NetcodeBootstrap";
    private const string NetworkManagerObjectName = "[B1] NetworkManager";
    private const uint RuntimeB2PlayerStableHash = 3202202601u;

    [Header("B1 启动配置")]
    [SerializeField] private bool autoStartFromCommandLine = true;
    [SerializeField] private bool showRuntimePanel = true;
    [SerializeField] private string defaultConnectAddress = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("B2 玩家原型配置")]
    [SerializeField] private bool enableB2PlayerPrototype = true;
    [SerializeField] private bool preferExistingPlayerPrefab = true;
    [SerializeField] private Vector3 b2SpawnOrigin = new Vector3(0f, 1f, 0f);
    [SerializeField] private float b2SpawnSpacing = 3f;

    [Header("运行时状态(只读)")]
    [SerializeField] private bool netcodeAvailable;
    [SerializeField] private string currentStatus = "Not Initialized";

    private Type networkManagerType;
    private Type unityTransportType;
    private Type networkConfigType;
    private Type networkObjectType;
    private Type networkTransformType;
    private Type b2NetworkPlayerType;

    private Component networkManagerComponent;
    private Component unityTransportComponent;
    private bool callbacksSubscribed;
    private GameObject runtimeB2PlayerPrefab;
    private uint runtimeB2TemplateHash;
    private LaunchMode pendingLaunchMode = LaunchMode.None;
    private string pendingLaunchAddress;
    private ushort pendingLaunchPort;

    private string panelAddress;
    private string panelPort;

    private void Awake()
    {
        EnsureSingleton();
        DontDestroyOnLoad(gameObject);
        gameObject.name = BootstrapObjectName;
        EnsureLocalPresentationBinder();

        panelAddress = defaultConnectAddress;
        panelPort = defaultPort.ToString(CultureInfo.InvariantCulture);

        ResolveNetcodeTypes();
        if (!netcodeAvailable)
        {
            currentStatus = "Netcode package missing";
            Debug.LogWarning("NetcodeBootstrap: 未检测到 NGO/Transport 运行时类型，请确认 Unity 已完成包导入。\n" +
                             "需要包: com.unity.netcode.gameobjects, com.unity.transport");
            return;
        }

        if (!EnsureNetworkObjects())
        {
            currentStatus = "Network object setup failed";
            return;
        }

        SubscribeNetworkCallbacks();

        if (autoStartFromCommandLine)
        {
            LaunchArgs args = LaunchArgs.Parse(defaultConnectAddress, defaultPort);
            Debug.Log($"[B1] Launch args parsed => mode={args.Mode}, ip={args.Address}, port={args.Port}");
            if (args.Mode != LaunchMode.None)
            {
                pendingLaunchMode = args.Mode;
                pendingLaunchAddress = args.Address;
                pendingLaunchPort = args.Port;
                currentStatus = "Pending auto start (wait scene)";
            }
            else
            {
                currentStatus = "Idle (awaiting manual start)";
            }
        }
        else
        {
            currentStatus = "Idle (manual mode)";
        }
    }

    private void Start()
    {
        if (pendingLaunchMode == LaunchMode.None) return;

        LaunchMode mode = pendingLaunchMode;
        string address = pendingLaunchAddress;
        ushort port = pendingLaunchPort;

        pendingLaunchMode = LaunchMode.None;
        pendingLaunchAddress = null;
        pendingLaunchPort = 0;

        StartByMode(mode, address, port);
    }

    private void EnsureLocalPresentationBinder()
    {
        if (GetComponent<LocalPresentationBinder>() != null) return;

        gameObject.AddComponent<LocalPresentationBinder>();
        Debug.Log("[B5] LocalPresentationBinder attached by bootstrap.");
    }

    public bool StartDedicatedServer(string address, ushort port)
    {
        Debug.Log($"[B1] StartDedicatedServer requested: {address}:{port}");
        if (!PrepareForStart(address, port)) return false;

        ShutdownIfListening();
        bool started = InvokeStartMethod("StartServer");
        if (started)
        {
            DisableLegacyScenePlayersInCurrentScene();
        }
        currentStatus = started ? $"Server running @ {address}:{port}" : "StartServer failed";
        Debug.Log($"[B1] StartServer result => {started}, status={currentStatus}");
        return started;
    }

    public bool StartClient(string address, ushort port)
    {
        Debug.Log($"[B1] StartClient requested: {address}:{port}");
        if (!PrepareForStart(address, port)) return false;

        ShutdownIfListening();
        bool started = InvokeStartMethod("StartClient");
        currentStatus = started ? $"Client connecting -> {address}:{port}" : "StartClient failed";
        Debug.Log($"[B1] StartClient result => {started}, status={currentStatus}");
        return started;
    }

    public void ShutdownNetworking()
    {
        if (!netcodeAvailable || networkManagerComponent == null) return;

        MethodInfo shutdownMethod = networkManagerType.GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.Public);
        shutdownMethod?.Invoke(networkManagerComponent, null);
        currentStatus = "Network shutdown";
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkCallbacks();
    }

    private void OnGUI()
    {
        if (!showRuntimePanel || Application.isBatchMode) return;

        GUILayout.BeginArea(new Rect(12, 12, 420, 220), GUI.skin.box);
        GUILayout.Label("B1 Netcode Bootstrap", GUI.skin.label);
        GUILayout.Label($"Status: {GetStatusText()}");

        if (!netcodeAvailable)
        {
            GUILayout.Space(8);
            GUILayout.Label("未检测到 NGO/Transport，请先让 Unity 完成包导入。", GUI.skin.label);
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Address", GUILayout.Width(70));
        panelAddress = GUILayout.TextField(panelAddress ?? defaultConnectAddress, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Port", GUILayout.Width(70));
        panelPort = GUILayout.TextField(panelPort ?? defaultPort.ToString(CultureInfo.InvariantCulture), GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Dedicated Server", GUILayout.Height(28)))
        {
            StartDedicatedServer(panelAddress, ParsePortOrDefault(panelPort, defaultPort));
        }

        if (GUILayout.Button("Start Client", GUILayout.Height(28)))
        {
            StartClient(panelAddress, ParsePortOrDefault(panelPort, defaultPort));
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Shutdown", GUILayout.Height(24)))
        {
            ShutdownNetworking();
        }

        GUILayout.EndArea();
    }

    private void StartByMode(LaunchMode mode, string address, ushort port)
    {
        switch (mode)
        {
            case LaunchMode.DedicatedServer:
                StartDedicatedServer(address, port);
                break;
            case LaunchMode.Client:
                StartClient(address, port);
                break;
            default:
                currentStatus = "Idle (unknown mode)";
                break;
        }
    }

    private bool PrepareForStart(string address, ushort port)
    {
        if (!netcodeAvailable)
        {
            currentStatus = "Netcode unavailable";
            return false;
        }

        if (!EnsureNetworkObjects())
        {
            currentStatus = "Network object setup failed";
            return false;
        }

        if (!PrepareB2PlayerPrefab())
        {
            currentStatus = "B2 player prefab setup failed";
            return false;
        }

        ConfigureNetworkConfigForBPhase();
        ConfigureTransport(address, port);
        BindTransportToNetworkConfig();
        return true;
    }

    private bool EnsureNetworkObjects()
    {
        if (!netcodeAvailable) return false;

        if (networkManagerComponent == null)
        {
            UnityEngine.Object existing = FindObjectOfType(networkManagerType);
            if (existing != null)
            {
                networkManagerComponent = existing as Component;
            }
            else
            {
                GameObject managerObj = new GameObject(NetworkManagerObjectName);
                DontDestroyOnLoad(managerObj);
                networkManagerComponent = managerObj.AddComponent(networkManagerType) as Component;
            }
        }

        if (networkManagerComponent == null)
        {
            Debug.LogError("NetcodeBootstrap: 创建 NetworkManager 失败。");
            return false;
        }

        GameObject managerGo = networkManagerComponent.gameObject;
        if (unityTransportComponent == null)
        {
            unityTransportComponent = managerGo.GetComponent(unityTransportType);
            if (unityTransportComponent == null)
            {
                unityTransportComponent = managerGo.AddComponent(unityTransportType) as Component;
            }
        }

        if (unityTransportComponent == null)
        {
            Debug.LogError("NetcodeBootstrap: 创建 UnityTransport 失败。");
            return false;
        }

        if (!EnsureNetworkConfig())
        {
            Debug.LogError("NetcodeBootstrap: 创建/获取 NetworkConfig 失败。");
            return false;
        }

        BindTransportToNetworkConfig();
        SubscribeNetworkCallbacks();
        return true;
    }

    private void BindTransportToNetworkConfig()
    {
        if (networkManagerComponent == null || unityTransportComponent == null) return;

        object configObj = GetOrCreateNetworkConfig();
        if (configObj == null)
        {
            Debug.LogError("NetcodeBootstrap: NetworkConfig 为空，无法绑定 NetworkTransport。");
            return;
        }

        if (!TrySetMember(configObj, "NetworkTransport", unityTransportComponent))
        {
            Debug.LogWarning("NetcodeBootstrap: 未能将 UnityTransport 绑定到 NetworkConfig.NetworkTransport。");
        }
    }

    private void ConfigureTransport(string address, ushort port)
    {
        if (unityTransportComponent == null) return;

        MethodInfo[] methods = unityTransportType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "SetConnectionData")
            .ToArray();

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] p = method.GetParameters();
            if (p.Length == 3 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(ushort))
            {
                method.Invoke(unityTransportComponent, new object[] { address, port, "0.0.0.0" });
                return;
            }

            if (p.Length == 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(ushort))
            {
                method.Invoke(unityTransportComponent, new object[] { address, port });
                return;
            }
        }

        Debug.LogWarning("NetcodeBootstrap: 未找到可用的 UnityTransport.SetConnectionData 重载。");
    }

    private bool InvokeStartMethod(string methodName)
    {
        if (networkManagerComponent == null) return false;

        MethodInfo method = networkManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            Debug.LogError($"NetcodeBootstrap: 未找到方法 {methodName}。");
            return false;
        }

        try
        {
            object result = method.Invoke(networkManagerComponent, null);
            return result is bool started && started;
        }
        catch (TargetInvocationException ex)
        {
            Exception root = ex.InnerException ?? ex;
            currentStatus = $"{methodName} threw: {root.GetType().Name}";
            Debug.LogError($"NetcodeBootstrap.{methodName} 异常: {root}");
            return false;
        }
    }

    private void ShutdownIfListening()
    {
        if (!ReadBoolProperty(networkManagerComponent, networkManagerType, "IsListening")) return;

        MethodInfo shutdownMethod = networkManagerType.GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.Public);
        shutdownMethod?.Invoke(networkManagerComponent, null);
    }

    private string GetStatusText()
    {
        if (!netcodeAvailable) return currentStatus;
        if (networkManagerComponent == null) return currentStatus;

        bool isListening = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsListening");
        bool isServer = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsServer");
        bool isClient = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsClient");

        if (isListening)
        {
            if (isServer && isClient) return "Host mode";
            if (isServer) return "Dedicated Server mode";
            if (isClient) return "Client mode";
        }

        return currentStatus;
    }

    private static ushort ParsePortOrDefault(string raw, ushort fallback)
    {
        return ushort.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort port)
            ? port
            : fallback;
    }

    private void EnsureSingleton()
    {
        NetcodeBootstrap[] all = FindObjectsOfType<NetcodeBootstrap>(true);
        if (all.Length <= 1) return;

        foreach (NetcodeBootstrap item in all)
        {
            if (item != this)
            {
                Destroy(item.gameObject);
            }
        }
    }

    private void ResolveNetcodeTypes()
    {
        networkManagerType = ResolveTypeFromLoadedAssemblies("Unity.Netcode.NetworkManager");
        unityTransportType = ResolveTypeFromLoadedAssemblies("Unity.Netcode.Transports.UTP.UnityTransport");
        networkConfigType = ResolveTypeFromLoadedAssemblies("Unity.Netcode.NetworkConfig");
        networkObjectType = ResolveTypeFromLoadedAssemblies("Unity.Netcode.NetworkObject");
        networkTransformType = ResolveTypeFromLoadedAssemblies("Unity.Netcode.Components.NetworkTransform");
        b2NetworkPlayerType = ResolveTypeFromLoadedAssemblies("NetworkPlayerController");
        netcodeAvailable = networkManagerType != null && unityTransportType != null && networkConfigType != null;
    }

    private bool EnsureNetworkConfig()
    {
        return GetOrCreateNetworkConfig() != null;
    }

    private object GetOrCreateNetworkConfig()
    {
        if (networkManagerComponent == null || networkManagerType == null || networkConfigType == null) return null;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        FieldInfo configField = networkManagerType.GetField("NetworkConfig", flags);
        if (configField != null)
        {
            object configObj = configField.GetValue(networkManagerComponent);
            if (configObj == null)
            {
                configObj = Activator.CreateInstance(networkConfigType);
                configField.SetValue(networkManagerComponent, configObj);
            }

            return configObj;
        }

        PropertyInfo configProp = networkManagerType.GetProperty("NetworkConfig", flags);
        if (configProp != null && configProp.CanRead)
        {
            object configObj = configProp.GetValue(networkManagerComponent);
            if (configObj == null && configProp.CanWrite)
            {
                configObj = Activator.CreateInstance(networkConfigType);
                configProp.SetValue(networkManagerComponent, configObj);
            }

            return configObj;
        }

        return null;
    }

    private static Type ResolveTypeFromLoadedAssemblies(string fullTypeName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = asm.GetType(fullTypeName);
            if (type != null) return type;
        }

        return null;
    }

    private static bool ReadBoolProperty(object target, Type targetType, string propertyName)
    {
        if (target == null || targetType == null) return false;

        PropertyInfo prop = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (prop == null || prop.PropertyType != typeof(bool)) return false;

        object value = prop.GetValue(target);
        return value is bool b && b;
    }

    private void SubscribeNetworkCallbacks()
    {
        if (callbacksSubscribed || networkManagerComponent == null || networkManagerType == null) return;

        TryBindEvent("OnServerStarted", nameof(HandleServerStarted), typeof(Action), subscribe: true);
        TryBindEvent("OnClientConnectedCallback", nameof(HandleClientConnected), typeof(Action<ulong>), subscribe: true);
        TryBindEvent("OnClientDisconnectCallback", nameof(HandleClientDisconnected), typeof(Action<ulong>), subscribe: true);
        TryBindEvent("OnTransportFailure", nameof(HandleTransportFailure), typeof(Action), subscribe: true);

        callbacksSubscribed = true;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        if (!callbacksSubscribed || networkManagerComponent == null || networkManagerType == null) return;

        TryBindEvent("OnServerStarted", nameof(HandleServerStarted), typeof(Action), subscribe: false);
        TryBindEvent("OnClientConnectedCallback", nameof(HandleClientConnected), typeof(Action<ulong>), subscribe: false);
        TryBindEvent("OnClientDisconnectCallback", nameof(HandleClientDisconnected), typeof(Action<ulong>), subscribe: false);
        TryBindEvent("OnTransportFailure", nameof(HandleTransportFailure), typeof(Action), subscribe: false);

        callbacksSubscribed = false;
    }

    private void TryBindEvent(string eventName, string handlerMethodName, Type delegateType, bool subscribe)
    {
        EventInfo evt = networkManagerType.GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);
        if (evt == null) return;

        MethodInfo method = GetType().GetMethod(handlerMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return;

        Delegate del = Delegate.CreateDelegate(delegateType, this, method, throwOnBindFailure: false);
        if (del == null) return;

        if (subscribe) evt.AddEventHandler(networkManagerComponent, del);
        else evt.RemoveEventHandler(networkManagerComponent, del);
    }

    private void HandleServerStarted()
    {
        Debug.Log("[B1] Event => OnServerStarted");
    }

    private void HandleClientConnected(ulong clientId)
    {
        bool isServer = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsServer");
        bool isClient = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsClient");

        string role = isServer && !isClient ? "Server" : (isClient && !isServer ? "Client" : "Host/Unknown");
        Debug.Log($"[B1] Event => OnClientConnectedCallback, role={role}, clientId={clientId}");

        // B2 兜底：如果 NGO 未自动创建 PlayerObject，则在服务端补一次 SpawnAsPlayerObject。
        if (isServer)
        {
            EnsureServerPlayerObject(clientId);
            TryAssignServerTargetPlayer(clientId);
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        bool isServer = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsServer");
        bool isClient = ReadBoolProperty(networkManagerComponent, networkManagerType, "IsClient");
        string disconnectReason = ReadStringProperty(networkManagerComponent, networkManagerType, "DisconnectReason");

        string role = isServer && !isClient ? "Server" : (isClient && !isServer ? "Client" : "Host/Unknown");
        if (string.IsNullOrEmpty(disconnectReason))
        {
            Debug.Log($"[B1] Event => OnClientDisconnectCallback, role={role}, clientId={clientId}");
        }
        else
        {
            Debug.Log($"[B1] Event => OnClientDisconnectCallback, role={role}, clientId={clientId}, reason={disconnectReason}");
        }
    }

    private void HandleTransportFailure()
    {
        Debug.LogError("[B1] Event => OnTransportFailure");
    }

    private static string ReadStringProperty(object target, Type targetType, string propertyName)
    {
        if (target == null || targetType == null) return null;

        PropertyInfo prop = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (prop == null || prop.PropertyType != typeof(string)) return null;

        return prop.GetValue(target) as string;
    }

    private static bool TrySetMember(object target, string memberName, object value)
    {
        if (target == null || value == null) return false;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type targetType = target.GetType();
        Type valueType = value.GetType();

        PropertyInfo prop = targetType.GetProperty(memberName, flags);
        if (prop != null && prop.CanWrite && prop.PropertyType.IsAssignableFrom(valueType))
        {
            prop.SetValue(target, value);
            return true;
        }

        FieldInfo field = targetType.GetField(memberName, flags);
        if (field != null && field.FieldType.IsAssignableFrom(valueType))
        {
            field.SetValue(target, value);
            return true;
        }

        return false;
    }

    private bool PrepareB2PlayerPrefab()
    {
        if (!enableB2PlayerPrototype) return true;
        if (networkManagerComponent == null || networkConfigType == null) return false;
        if (networkObjectType == null)
        {
            Debug.LogError("[B2] 未找到 Unity.Netcode.NetworkObject 类型，无法注册玩家预制体。");
            return false;
        }

        object configObj = GetOrCreateNetworkConfig();
        if (configObj == null)
        {
            Debug.LogError("[B2] NetworkConfig 为空，无法配置 PlayerPrefab。");
            return false;
        }

        GameObject currentPlayerPrefab = ReadPlayerPrefab(configObj);
        GameObject desiredPlayerPrefab = ResolveB2PlayerPrefabCandidate(out string source);
        if (desiredPlayerPrefab == null)
        {
            Debug.LogError("[B2] 未能解析可用的 PlayerPrefab。");
            return false;
        }

        EnsureB2Components(desiredPlayerPrefab);
        DebugLogB2TemplateComponents(desiredPlayerPrefab);

        if (currentPlayerPrefab != desiredPlayerPrefab)
        {
            if (!TrySetMember(configObj, "PlayerPrefab", desiredPlayerPrefab))
            {
                Debug.LogError("[B2] 无法写入 NetworkConfig.PlayerPrefab。");
                return false;
            }
        }

        HideRuntimeTemplateInScene(desiredPlayerPrefab);
        bool registered = TryInvokeAddNetworkPrefab(desiredPlayerPrefab);
        Debug.Log($"[B2] Player prefab prepared => source={source}, name={desiredPlayerPrefab.name}, addNetworkPrefab={registered}");
        return true;
    }

    private void DebugLogB2TemplateComponents(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[B2-R] Template verify skipped: prefab is null");
            return;
        }

        bool hasNO = networkObjectType != null && prefab.GetComponent(networkObjectType) != null;
        bool hasNT = networkTransformType != null && prefab.GetComponent(networkTransformType) != null;
        bool hasNPC = prefab.GetComponent<NetworkPlayerController>() != null;
        Debug.Log($"[B2-R] Template verify => name={prefab.name}, hasNO={hasNO}, hasNT={hasNT}, hasNPC={hasNPC}, activeSelf={prefab.activeSelf}");
    }

    private GameObject ResolveB2PlayerPrefabCandidate(out string source)
    {
        if (preferExistingPlayerPrefab && TryCreateRuntimePrefabFromExistingPlayer(out GameObject fromScenePlayer))
        {
            source = "ExistingPlayer";
            return fromScenePlayer;
        }

        source = "CapsuleFallback";
        return CreateRuntimeB2PlayerPrefab();
    }

    private bool TryCreateRuntimePrefabFromExistingPlayer(out GameObject prefab)
    {
        prefab = null;
        GameObject scenePlayer = FindExistingScenePlayerObject();
        if (scenePlayer == null) return false;

        // B2-R: 以场景中既有 Player 的摆放点作为网络出生基准点，
        // 避免默认(0,1,0)导致角色刷在不可见区域或地形下方。
        b2SpawnOrigin = scenePlayer.transform.position;

        bool reuseExistingTemplate = runtimeB2PlayerPrefab != null
                                     && runtimeB2PlayerPrefab.GetComponent<Player>() != null
                                     && runtimeB2PlayerPrefab.name.StartsWith("[B2-R]", StringComparison.Ordinal);
        if (reuseExistingTemplate)
        {
            Debug.Log($"[B2-R] Reuse existing runtime template; spawn origin aligned => {b2SpawnOrigin}");
            prefab = runtimeB2PlayerPrefab;
            return true;
        }

        if (runtimeB2PlayerPrefab != null)
        {
            Destroy(runtimeB2PlayerPrefab);
            runtimeB2PlayerPrefab = null;
        }

        GameObject clone = Instantiate(scenePlayer);
        clone.name = "[B2-R] RuntimeNetworkPlayerPrefab";
        clone.transform.position = b2SpawnOrigin;

        runtimeB2TemplateHash = StripExistingNetcodeComponents(clone);

        // 运行时模板保持激活，保证 NGO 自动实例化时对象为 active。
        // 同时将模板对象移到 Bootstrap 节点下，尽量避开主场景内容扫描链路。
        clone.transform.SetParent(transform, worldPositionStays: true);
        clone.tag = "Untagged";

        clone.hideFlags = HideFlags.HideAndDontSave;
        clone.SetActive(true);

        DontDestroyOnLoad(clone);
        runtimeB2PlayerPrefab = clone;
        Debug.Log($"[B2-R] Runtime player template created from existing Player => source={scenePlayer.name}, spawnOrigin={b2SpawnOrigin}, scene={clone.scene.name}, hashHint={runtimeB2TemplateHash}");

        prefab = runtimeB2PlayerPrefab;
        return true;
    }

    private static GameObject FindExistingScenePlayerObject()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null && tagged.hideFlags == HideFlags.None) return tagged;

        Player[] players = FindObjectsOfType<Player>(true);
        foreach (Player player in players)
        {
            if (player == null) continue;
            GameObject go = player.gameObject;
            if (go == null) continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (go.name.StartsWith("[B2]", StringComparison.Ordinal) || go.name.StartsWith("[B2-R]", StringComparison.Ordinal)) continue;
            return go;
        }

        return null;
    }

    private GameObject CreateRuntimeB2PlayerPrefab()
    {
        if (runtimeB2PlayerPrefab != null) return runtimeB2PlayerPrefab;

        GameObject prefab = new GameObject("[B2] RuntimeNetworkPlayerPrefab");
        prefab.transform.position = b2SpawnOrigin;
        prefab.hideFlags = HideFlags.HideAndDontSave;

        // 可视化体仅用于 B2 验证，后续会切换为正式玩家网络预制体。
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(prefab.transform, false);
        visual.transform.localPosition = Vector3.zero;
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        EnsureB2Components(prefab);

        // 运行时模板仅供 NGO 实例化，不应作为场景实体显示。
        prefab.SetActive(false);
        DontDestroyOnLoad(prefab);
        runtimeB2PlayerPrefab = prefab;
        Debug.Log("[B2] Runtime player template created and hidden for NGO player spawning.");
        return runtimeB2PlayerPrefab;
    }

    private uint StripExistingNetcodeComponents(GameObject root)
    {
        if (root == null) return 0u;

        int removedNetworkObject = 0;
        int removedNetworkTransform = 0;
        uint preservedHash = 0u;

        if (networkObjectType != null)
        {
            Component[] objects = root.GetComponentsInChildren(networkObjectType, true);
            foreach (Component item in objects)
            {
                if (item == null) continue;
                if (preservedHash == 0u)
                {
                    object hashObj = ReadMemberValue(item, item.GetType(), "GlobalObjectIdHash");
                    if (hashObj is uint hashValue && hashValue != 0u)
                    {
                        preservedHash = hashValue;
                    }
                }
                DestroyComponentImmediate(item);
                removedNetworkObject++;
            }
        }

        if (networkTransformType != null)
        {
            Component[] transforms = root.GetComponentsInChildren(networkTransformType, true);
            foreach (Component item in transforms)
            {
                if (item == null) continue;
                DestroyComponentImmediate(item);
                removedNetworkTransform++;
            }
        }

        if (removedNetworkObject > 0 || removedNetworkTransform > 0)
        {
            Debug.Log($"[B2-R] Stripped legacy netcode components => no={removedNetworkObject}, nt={removedNetworkTransform}, preservedHash={preservedHash}");
        }

        return preservedHash;
    }

    private static void DestroyComponentImmediate(Component component)
    {
        if (component == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(component);
            return;
        }
#endif
        // 运行时模板的旧网络组件必须同帧剥离，避免 Ensure 阶段误判“组件仍存在”。
        DestroyImmediate(component);
    }

    private void HideRuntimeTemplateInScene(GameObject prefab)
    {
        if (prefab == null) return;

        bool isRuntimeTemplate = prefab == runtimeB2PlayerPrefab
                                 || prefab.name.StartsWith("[B2] RuntimeNetworkPlayerPrefab", StringComparison.Ordinal);
        if (!isRuntimeTemplate) return;
        if (!prefab.activeSelf) return;

        prefab.SetActive(false);
        Debug.Log("[B2] Runtime player template hidden in scene.");
    }

    private void EnsureB2Components(GameObject target)
    {
        if (target == null) return;

        Component networkObject = null;
        if (networkObjectType != null)
        {
            networkObject = target.GetComponent(networkObjectType);
            if (networkObject == null)
            {
                networkObject = target.AddComponent(networkObjectType);
            }
        }

        if (networkTransformType != null && target.GetComponent(networkTransformType) == null)
        {
            target.AddComponent(networkTransformType);
        }

        EnsureStableTemplateHash(networkObject);
        ForceAsDynamicSpawn(networkObject);

        CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = target.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.center = new Vector3(0f, 0f, 0f);
        }

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        NetworkPlayerController probe = target.GetComponent<NetworkPlayerController>();
        if (probe == null)
        {
            probe = target.AddComponent<NetworkPlayerController>();
        }

        if (probe != null)
        {
            probe.SetSpawnRule(b2SpawnOrigin, b2SpawnSpacing);
        }
        else
        {
            Debug.LogError("[B2-R] 无法挂载 NetworkPlayerController，玩家生成将缺少输入/出生/日志链路。");
        }

        DisableLocalOnlyComponentsForB2(target);

        bool hasNetworkObject = target.GetComponent(networkObjectType) != null;
        bool hasNetworkTransform = target.GetComponent(networkTransformType) != null;
        bool hasNetworkPlayerController = target.GetComponent<NetworkPlayerController>() != null;
        Debug.Log($"[B2-R] Ensure player components => name={target.name}, hasNO={hasNetworkObject}, hasNT={hasNetworkTransform}, hasNPC={hasNetworkPlayerController}");
    }

    private void EnsureStableTemplateHash(Component networkObject)
    {
        if (networkObject == null) return;

        uint currentHash = 0u;
        object current = ReadMemberValue(networkObject, networkObject.GetType(), "GlobalObjectIdHash");
        if (current is uint currentValue)
        {
            currentHash = currentValue;
        }

        uint targetHash = RuntimeB2PlayerStableHash;
        if (runtimeB2TemplateHash == 0u)
        {
            runtimeB2TemplateHash = targetHash;
        }

        if (currentHash != targetHash)
        {
            if (!TrySetMember(networkObject, "GlobalObjectIdHash", targetHash))
            {
                Debug.LogWarning($"[B2-R] Failed to set stable GlobalObjectIdHash => current={currentHash}, target={targetHash}");
            }
            else
            {
                Debug.Log($"[B2-R] GlobalObjectIdHash forced => from={currentHash}, to={targetHash}");
            }
        }
    }

    private void ConfigureNetworkConfigForBPhase()
    {
        object configObj = GetOrCreateNetworkConfig();
        if (configObj == null) return;

        // B 阶段仅验证连接/玩家同步，先禁用场景管理，避免 SceneObject 同步链路与运行时模板冲突。
        if (TrySetMember(configObj, "EnableSceneManagement", false))
        {
            Debug.Log("[B1] NetworkConfig => EnableSceneManagement=False (B-phase stability mode)");
        }
    }

    private static void DisableLocalOnlyComponentsForB2(GameObject target)
    {
        if (target == null) return;

        PlayerController localController = target.GetComponent<PlayerController>();
        if (localController != null && localController.enabled)
        {
            localController.enabled = false;
        }

        WeaponManager[] weaponManagers = target.GetComponentsInChildren<WeaponManager>(true);
        foreach (WeaponManager weaponManager in weaponManagers)
        {
            if (weaponManager != null && weaponManager.enabled)
            {
                weaponManager.enabled = false;
            }
        }
    }

    private bool TryInvokeAddNetworkPrefab(GameObject prefab)
    {
        if (networkManagerComponent == null || prefab == null) return false;

        try
        {
            MethodInfo addMethod = networkManagerType.GetMethod("AddNetworkPrefab", BindingFlags.Instance | BindingFlags.Public);
            if (addMethod == null) return false;
            addMethod.Invoke(networkManagerComponent, new object[] { prefab });
            return true;
        }
        catch (TargetInvocationException ex)
        {
            Exception root = ex.InnerException ?? ex;
            Debug.LogWarning($"[B2] AddNetworkPrefab 调用失败（可忽略一次性重复注册）: {root.Message}");
            return false;
        }
    }

    private static GameObject ReadPlayerPrefab(object configObj)
    {
        if (configObj == null) return null;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = configObj.GetType();

        FieldInfo field = type.GetField("PlayerPrefab", flags);
        if (field != null && typeof(GameObject).IsAssignableFrom(field.FieldType))
        {
            return field.GetValue(configObj) as GameObject;
        }

        PropertyInfo property = type.GetProperty("PlayerPrefab", flags);
        if (property != null && typeof(GameObject).IsAssignableFrom(property.PropertyType) && property.CanRead)
        {
            return property.GetValue(configObj) as GameObject;
        }

        return null;
    }

    private void TryInvokeSpawnRuleSetter(Component probe)
    {
        if (probe == null) return;

        MethodInfo setSpawnRule = probe.GetType().GetMethod("SetSpawnRule", BindingFlags.Instance | BindingFlags.Public);
        if (setSpawnRule == null) return;

        ParameterInfo[] parameters = setSpawnRule.GetParameters();
        if (parameters.Length != 2) return;
        if (parameters[0].ParameterType != typeof(Vector3) || parameters[1].ParameterType != typeof(float)) return;

        setSpawnRule.Invoke(probe, new object[] { b2SpawnOrigin, b2SpawnSpacing });
    }

    private void EnsureServerPlayerObject(ulong clientId)
    {
        if (!enableB2PlayerPrototype || networkManagerComponent == null || networkManagerType == null) return;

        try
        {
            object spawnManager = ReadMemberValue(networkManagerComponent, networkManagerType, "SpawnManager");
            if (spawnManager == null) return;

            MethodInfo getPlayerMethod = spawnManager.GetType().GetMethod("GetPlayerNetworkObject", BindingFlags.Instance | BindingFlags.Public);
            if (getPlayerMethod != null)
            {
                object existingPlayer = getPlayerMethod.Invoke(spawnManager, new object[] { clientId });
                if (existingPlayer is Component existingPlayerComponent)
                {
                    NetworkPlayerController existingController = existingPlayerComponent.GetComponent<NetworkPlayerController>();
                    bool isSpawned = ReadBoolProperty(existingPlayerComponent, existingPlayerComponent.GetType(), "IsSpawned");
                    bool isActive = existingPlayerComponent.gameObject.activeSelf && existingPlayerComponent.gameObject.activeInHierarchy;
                    bool existingValid = existingController != null && isSpawned && isActive;

                    if (existingValid)
                    {
                        if (!existingController.enabled)
                        {
                            existingController.enabled = true;
                        }

                        existingController.SetSpawnRule(b2SpawnOrigin, b2SpawnSpacing);
                        Debug.Log($"[B2] PlayerObject already exists for clientId={clientId}, keep existing => {DescribePlayerObject(existingPlayerComponent.gameObject)}");
                        return;
                    }

                    string reason = existingController == null
                        ? "missing NetworkPlayerController"
                        : (!isSpawned ? "not spawned" : "inactive");
                    Debug.LogWarning($"[B2-R] Existing player object invalid ({reason}), replacing => clientId={clientId}, {DescribePlayerObject(existingPlayerComponent.gameObject)}");
                    ReplaceExistingPlayerObject(existingPlayerComponent, clientId);
                    return;
                }
            }

            object configObj = GetOrCreateNetworkConfig();
            GameObject playerPrefab = ReadPlayerPrefab(configObj);
            if (playerPrefab == null)
            {
                Debug.LogError($"[B2] Fallback spawn failed: PlayerPrefab is null, clientId={clientId}");
                return;
            }

            GameObject instance = Instantiate(playerPrefab);
            instance.name = $"{playerPrefab.name}_Client_{clientId}";
            instance.SetActive(true);

            Component netObj = instance.GetComponent(networkObjectType);
            if (netObj == null)
            {
                Debug.LogError($"[B2] Fallback spawn failed: instance has no NetworkObject, clientId={clientId}");
                Destroy(instance);
                return;
            }

            MethodInfo spawnAsPlayerMethod = networkObjectType.GetMethod(
                "SpawnAsPlayerObject",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(ulong), typeof(bool) },
                modifiers: null);

            if (spawnAsPlayerMethod == null)
            {
                Debug.LogError("[B2] Fallback spawn failed: NetworkObject.SpawnAsPlayerObject not found.");
                Destroy(instance);
                return;
            }

            ForceAsDynamicSpawn(netObj);
            spawnAsPlayerMethod.Invoke(netObj, new object[] { clientId, false });
            Debug.Log($"[B2] Fallback SpawnAsPlayerObject success => clientId={clientId}, instance={instance.name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[B2] EnsureServerPlayerObject exception for clientId={clientId}: {ex}");
        }
    }

    private void ReplaceExistingPlayerObject(Component existingPlayerComponent, ulong clientId)
    {
        if (existingPlayerComponent == null) return;

        try
        {
            MethodInfo despawnMethod = networkObjectType.GetMethod(
                "Despawn",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(bool) },
                modifiers: null);

            if (despawnMethod != null)
            {
                despawnMethod.Invoke(existingPlayerComponent, new object[] { true });
            }
            else
            {
                Destroy(existingPlayerComponent.gameObject);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[B2-R] Existing player despawn failed, fallback destroy => clientId={clientId}, reason={ex.Message}");
            if (existingPlayerComponent != null && existingPlayerComponent.gameObject != null)
            {
                Destroy(existingPlayerComponent.gameObject);
            }
        }

        object configObj = GetOrCreateNetworkConfig();
        GameObject playerPrefab = ReadPlayerPrefab(configObj);
        if (playerPrefab == null)
        {
            Debug.LogError($"[B2-R] Replace existing player failed: PlayerPrefab is null, clientId={clientId}");
            return;
        }

        GameObject instance = Instantiate(playerPrefab);
        instance.name = $"{playerPrefab.name}_Client_{clientId}_Replaced";
        instance.SetActive(true);

        Component netObj = instance.GetComponent(networkObjectType);
        if (netObj == null)
        {
            Debug.LogError($"[B2-R] Replace existing player failed: instance has no NetworkObject, clientId={clientId}");
            Destroy(instance);
            return;
        }

        ForceAsDynamicSpawn(netObj);
        MethodInfo spawnAsPlayerMethod = networkObjectType.GetMethod(
            "SpawnAsPlayerObject",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(ulong), typeof(bool) },
            modifiers: null);
        if (spawnAsPlayerMethod == null)
        {
            Debug.LogError("[B2-R] Replace existing player failed: NetworkObject.SpawnAsPlayerObject not found.");
            Destroy(instance);
            return;
        }

        spawnAsPlayerMethod.Invoke(netObj, new object[] { clientId, false });
        Debug.Log($"[B2-R] Replace existing player success => clientId={clientId}, instance={instance.name}");
    }

    private void ForceAsDynamicSpawn(Component networkObject)
    {
        if (networkObject == null) return;

        TrySetMember(networkObject, "IsSceneObject", (bool?)false);
        TrySetMember(networkObject, "SceneOriginHandle", 0);
        TrySetMember(networkObject, "NetworkSceneHandle", 0);
    }

    private void TryAssignServerTargetPlayer(ulong clientId)
    {
        if (GameManager.Instance == null || networkManagerComponent == null || networkManagerType == null) return;

        try
        {
            object spawnManager = ReadMemberValue(networkManagerComponent, networkManagerType, "SpawnManager");
            if (spawnManager == null) return;

            MethodInfo getPlayerMethod = spawnManager.GetType().GetMethod("GetPlayerNetworkObject", BindingFlags.Instance | BindingFlags.Public);
            if (getPlayerMethod == null) return;

            object playerNetworkObject = getPlayerMethod.Invoke(spawnManager, new object[] { clientId });
            if (playerNetworkObject is not Component playerComponent) return;

            GameManager.Instance.playerTransform = playerComponent.transform;
            Debug.Log($"[B2-R] Server target player assigned => clientId={clientId}, {DescribePlayerObject(playerComponent.gameObject)}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[B2-R] Assign server target player failed => clientId={clientId}, reason={ex.Message}");
        }
    }

    private static void DisableLegacyScenePlayersInCurrentScene()
    {
        Player[] players = FindObjectsOfType<Player>(true);
        int disabled = 0;

        foreach (Player player in players)
        {
            if (player == null) continue;
            GameObject go = player.gameObject;
            if (go == null) continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (go.name.StartsWith("[B2]", StringComparison.Ordinal) || go.name.StartsWith("[B2-R]", StringComparison.Ordinal)) continue;
            if (!go.activeSelf) continue;

            go.SetActive(false);
            disabled++;
        }

        if (disabled > 0)
        {
            Debug.Log($"[B2-R] Legacy scene Player disabled => count={disabled}");
        }
    }

    private static string DescribePlayerObject(GameObject go)
    {
        if (go == null) return "playerObject=null";

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        int enabledRenderers = renderers.Count(r => r != null && r.enabled);
        int activeRenderers = renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy);

        NetworkPlayerController npc = go.GetComponent<NetworkPlayerController>();
        string pos = go.transform.position.ToString("F2");
        string sceneName = go.scene.IsValid() ? go.scene.name : "InvalidScene";

        return $"target={go.name}, pos={pos}, scene={sceneName}, activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}, hasNPC={(npc != null)}, rendererTotal={renderers.Length}, rendererEnabled={enabledRenderers}, rendererActive={activeRenderers}";
    }

    private static object ReadMemberValue(object target, Type targetType, string memberName)
    {
        if (target == null || targetType == null || string.IsNullOrEmpty(memberName)) return null;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo property = targetType.GetProperty(memberName, flags);
        if (property != null && property.CanRead)
        {
            return property.GetValue(target);
        }

        FieldInfo field = targetType.GetField(memberName, flags);
        if (field != null)
        {
            return field.GetValue(target);
        }

        return null;
    }

    private enum LaunchMode
    {
        None,
        DedicatedServer,
        Client
    }

    private readonly struct LaunchArgs
    {
        public readonly LaunchMode Mode;
        public readonly string Address;
        public readonly ushort Port;

        public LaunchArgs(LaunchMode mode, string address, ushort port)
        {
            Mode = mode;
            Address = address;
            Port = port;
        }

        public static LaunchArgs Parse(string defaultAddress, ushort defaultPort)
        {
            string[] args = Environment.GetCommandLineArgs();

            LaunchMode mode = LaunchMode.None;
            if (HasArg(args, "-hklServer") || HasArg(args, "-dedicatedServer") || ArgEquals(args, "-mode", "server"))
            {
                mode = LaunchMode.DedicatedServer;
            }
            else if (HasArg(args, "-hklClient") || HasArg(args, "-client") || ArgEquals(args, "-mode", "client"))
            {
                mode = LaunchMode.Client;
            }

            string address = GetArgValue(args, "-ip") ?? defaultAddress;
            string portRaw = GetArgValue(args, "-port");
            ushort port = ParsePortOrDefault(portRaw, defaultPort);

            return new LaunchArgs(mode, address, port);
        }

        private static bool HasArg(string[] args, string key)
        {
            return args.Any(arg => string.Equals(arg, key, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ArgEquals(string[] args, string key, string expected)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) continue;
                return string.Equals(args[i + 1], expected, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
