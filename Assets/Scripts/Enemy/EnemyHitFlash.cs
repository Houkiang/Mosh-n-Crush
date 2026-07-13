using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Enemy))]
public class EnemyHitFlash : MonoBehaviour
{
    private const string OverlayMaterialResourcePath = "Materials/EnemyHitFlashOverlay";
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    [Header("Hit Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashAlpha = 0.85f;

    private readonly List<OverlayTarget> overlayTargets = new List<OverlayTarget>();
    private static Material sharedOverlayMaterial;
    private bool isFlashing;
    private float flashTimer;

    private void Awake()
    {
        EnsureOverlayMaterial();
        BuildOverlayTargets();
    }

    private void OnEnable()
    {
        if (!isFlashing)
        {
            ResetFlashState();
            enabled = false;
        }
    }

    private void OnDisable()
    {
        ResetFlashState();
    }

    private void Update()
    {
        flashTimer -= Time.deltaTime;
        float intensity = flashDuration > 0.0001f
            ? Mathf.Clamp01(flashTimer / flashDuration)
            : 0f;

        SetOverlayIntensity(intensity);

        if (flashTimer <= 0f)
        {
            ResetFlashState();
            enabled = false;
        }
    }

    public void PlayFlash()
    {
        if (overlayTargets.Count == 0)
        {
            EnsureOverlayMaterial();
            BuildOverlayTargets();
        }

        if (overlayTargets.Count == 0)
        {
            return;
        }

        flashTimer = Mathf.Max(0.01f, flashDuration);
        isFlashing = true;

        if (!enabled)
        {
            enabled = true;
        }

        SetOverlayIntensity(1f);
    }

    private void EnsureOverlayMaterial()
    {
        if (sharedOverlayMaterial != null)
        {
            return;
        }

        sharedOverlayMaterial = Resources.Load<Material>(OverlayMaterialResourcePath);
        if (sharedOverlayMaterial == null)
        {
            Debug.LogWarning($"EnemyHitFlash overlay material not found at Resources/{OverlayMaterialResourcePath}.");
        }
    }

    private void BuildOverlayTargets()
    {
        overlayTargets.Clear();
        if (sharedOverlayMaterial == null)
        {
            return;
        }

        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            TryCreateSkinnedOverlay(skinnedRenderers[i]);
        }

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            TryCreateMeshOverlay(meshRenderers[i]);
        }
    }

    private void TryCreateSkinnedOverlay(SkinnedMeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null ||
            sourceRenderer.GetComponent<EnemyHitFlashOverlayMarker>() != null)
        {
            return;
        }

        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        GameObject overlayObject = CreateOverlayObject(sourceRenderer.gameObject.name, sourceRenderer.transform);
        SkinnedMeshRenderer overlayRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
        overlayRenderer.sharedMesh = sourceRenderer.sharedMesh;
        overlayRenderer.rootBone = sourceRenderer.rootBone;
        overlayRenderer.bones = sourceRenderer.bones;
        overlayRenderer.localBounds = sourceRenderer.localBounds;
        overlayRenderer.updateWhenOffscreen = false;
        ConfigureOverlayRenderer(overlayRenderer, sourceMaterials.Length);
    }

    private void TryCreateMeshOverlay(MeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer.GetComponent<EnemyHitFlashOverlayMarker>() != null)
        {
            return;
        }

        MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        GameObject overlayObject = CreateOverlayObject(sourceRenderer.gameObject.name, sourceRenderer.transform);
        overlayObject.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
        ConfigureOverlayRenderer(overlayRenderer, sourceMaterials.Length);
    }

    private GameObject CreateOverlayObject(string sourceName, Transform sourceTransform)
    {
        GameObject overlayObject = new GameObject(sourceName + "_HitFlash");
        overlayObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        overlayObject.transform.SetParent(sourceTransform, false);
        overlayObject.AddComponent<EnemyHitFlashOverlayMarker>();
        return overlayObject;
    }

    private void ConfigureOverlayRenderer(Renderer overlayRenderer, int materialCount)
    {
        overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
        overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        overlayRenderer.allowOcclusionWhenDynamic = false;
        overlayRenderer.sharedMaterials = CreateOverlayMaterialArray(materialCount);
        overlayRenderer.enabled = false;

        overlayTargets.Add(new OverlayTarget
        {
            Renderer = overlayRenderer,
            PropertyBlock = new MaterialPropertyBlock(),
            MaterialCount = materialCount
        });
    }

    private Material[] CreateOverlayMaterialArray(int count)
    {
        Material[] materials = new Material[count];
        for (int i = 0; i < count; i++)
        {
            materials[i] = sharedOverlayMaterial;
        }

        return materials;
    }

    private void SetOverlayIntensity(float intensity)
    {
        float alpha = Mathf.Clamp01(intensity) * Mathf.Clamp01(flashAlpha);
        Color overlayColor = flashColor;
        overlayColor.a *= alpha;
        bool visible = overlayColor.a > 0.001f;

        for (int i = 0; i < overlayTargets.Count; i++)
        {
            OverlayTarget target = overlayTargets[i];
            if (target.Renderer == null)
            {
                continue;
            }

            target.Renderer.enabled = visible;
            if (!visible)
            {
                continue;
            }

            target.PropertyBlock.Clear();
            target.PropertyBlock.SetColor(BaseColorPropertyId, overlayColor);
            for (int materialIndex = 0; materialIndex < target.MaterialCount; materialIndex++)
            {
                target.Renderer.SetPropertyBlock(target.PropertyBlock, materialIndex);
            }
        }
    }

    private void ResetFlashState()
    {
        isFlashing = false;
        flashTimer = 0f;

        for (int i = 0; i < overlayTargets.Count; i++)
        {
            if (overlayTargets[i].Renderer != null)
            {
                overlayTargets[i].Renderer.enabled = false;
            }
        }
    }

    private sealed class OverlayTarget
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
        public int MaterialCount;
    }
}

public class EnemyHitFlashOverlayMarker : MonoBehaviour
{
}
