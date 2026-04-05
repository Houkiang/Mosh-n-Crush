using UnityEngine;

public static class B1NetcodeBootstrapInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Object.FindObjectOfType<B1NetcodeBootstrap>(true) != null) return;

        GameObject bootstrap = new GameObject("[B1] NetcodeBootstrap");
        bootstrap.AddComponent<B1NetcodeBootstrap>();
    }
}
