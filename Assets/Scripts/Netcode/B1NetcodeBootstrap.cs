using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class B1NetcodeBootstrap : MonoBehaviour
{
    private const string BootstrapObjectName = "[B1] NetcodeBootstrap";
    private const string NetworkManagerObjectName = "[B1] NetworkManager";

    [Header("B1 启动配置")]
    [SerializeField] private bool autoStartFromCommandLine = true;
    [SerializeField] private bool showRuntimePanel = true;
    [SerializeField] private string defaultConnectAddress = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("运行时状态(只读)")]
    [SerializeField] private bool netcodeAvailable;
    [SerializeField] private string currentStatus = "Not Initialized";

    private Type networkManagerType;
    private Type unityTransportType;
    private Type networkConfigType;

    private Component networkManagerComponent;
    private Component unityTransportComponent;
    private bool callbacksSubscribed;

    private string panelAddress;
    private string panelPort;

    private void Awake()
    {
        EnsureSingleton();
        DontDestroyOnLoad(gameObject);
        gameObject.name = BootstrapObjectName;

        panelAddress = defaultConnectAddress;
        panelPort = defaultPort.ToString(CultureInfo.InvariantCulture);

        ResolveNetcodeTypes();
        if (!netcodeAvailable)
        {
            currentStatus = "Netcode package missing";
            Debug.LogWarning("B1NetcodeBootstrap: 未检测到 NGO/Transport 运行时类型，请确认 Unity 已完成包导入。\n" +
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
                StartByMode(args.Mode, args.Address, args.Port);
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

    public bool StartDedicatedServer(string address, ushort port)
    {
        Debug.Log($"[B1] StartDedicatedServer requested: {address}:{port}");
        if (!PrepareForStart(address, port)) return false;

        ShutdownIfListening();
        bool started = InvokeStartMethod("StartServer");
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
            Debug.LogError("B1NetcodeBootstrap: 创建 NetworkManager 失败。");
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
            Debug.LogError("B1NetcodeBootstrap: 创建 UnityTransport 失败。");
            return false;
        }

        if (!EnsureNetworkConfig())
        {
            Debug.LogError("B1NetcodeBootstrap: 创建/获取 NetworkConfig 失败。");
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
            Debug.LogError("B1NetcodeBootstrap: NetworkConfig 为空，无法绑定 NetworkTransport。");
            return;
        }

        if (!TrySetMember(configObj, "NetworkTransport", unityTransportComponent))
        {
            Debug.LogWarning("B1NetcodeBootstrap: 未能将 UnityTransport 绑定到 NetworkConfig.NetworkTransport。");
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

        Debug.LogWarning("B1NetcodeBootstrap: 未找到可用的 UnityTransport.SetConnectionData 重载。");
    }

    private bool InvokeStartMethod(string methodName)
    {
        if (networkManagerComponent == null) return false;

        MethodInfo method = networkManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            Debug.LogError($"B1NetcodeBootstrap: 未找到方法 {methodName}。");
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
            Debug.LogError($"B1NetcodeBootstrap.{methodName} 异常: {root}");
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
        B1NetcodeBootstrap[] all = FindObjectsOfType<B1NetcodeBootstrap>(true);
        if (all.Length <= 1) return;

        foreach (B1NetcodeBootstrap item in all)
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
