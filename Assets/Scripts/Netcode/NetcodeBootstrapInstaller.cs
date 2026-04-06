using UnityEngine;

public static class NetcodeBootstrapInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        NetcodeBootstrap existingBootstrap = UnityEngine.Object.FindObjectOfType<NetcodeBootstrap>(true);
        if (existingBootstrap != null)
        {
            EnsureB5Binder(existingBootstrap.gameObject);
            return;
        }

        GameObject bootstrap = new GameObject("[B1] NetcodeBootstrap");
        bootstrap.AddComponent<NetcodeBootstrap>();
        EnsureB5Binder(bootstrap);
    }

    private static void EnsureB5Binder(GameObject bootstrap)
    {
        if (bootstrap == null) return;
        if (bootstrap.GetComponent<LocalPresentationBinder>() != null)
        {
            Debug.Log("[B5] LocalPresentationBinder already attached.");
            return;
        }

        bootstrap.AddComponent<LocalPresentationBinder>();
        Debug.Log("[B5] LocalPresentationBinder attached by installer.");
    }
}
