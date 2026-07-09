using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Enemy))]
public class EnemyHitFlash : MonoBehaviour
{
    private const string OverlayMaterialResourcePath = "Materials/EnemyHitFlashOverlay";

    [Header("受击闪白")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashAlpha = 0.85f;

    private readonly List<OverlayTarget> overlayTargets = new List<OverlayTarget>();
    private bool isFlashing;
    private float flashTimer;
    private static Material sharedOverlayMaterial;
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        EnsureOverlayMaterial();
        BuildOverlayTargets();
    }

    private void OnEnable()
    {
        ResetFlashState();
    }

    private void OnDisable()
    {
        ResetFlashState();
    }

    private void Update()
    {
        if (!isFlashing)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        float intensity = flashDuration > 0.0001f
            ? Mathf.Clamp01(flashTimer / flashDuration)
            : 0f;

        SetOverlayIntensity(intensity);

        if (flashTimer <= 0f)
        {
            ResetFlashState();
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
        SetOverlayIntensity(1f);
    }

    private void EnsureOverlayMaterial()
    {
        if (sharedOverlayMaterial != null)
        {
            return;
        }

        Material templateMaterial = Resources.Load<Material>(OverlayMaterialResourcePath);
        if (templateMaterial == null)
        {
            Debug.LogWarning($"EnemyHitFlash overlay material not found at Resources/{OverlayMaterialResourcePath}.");
            return;
        }

        sharedOverlayMaterial = new Material(templateMaterial)
        {
            name = templateMaterial.name + "_Runtime"
        };
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
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
        {
            return;
        }

        if (sourceRenderer.GetComponent<EnemyHitFlashOverlayMarker>() != null)
        {
            return;
        }

        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        GameObject overlayObject = new GameObject(sourceRenderer.gameObject.name + "_HitFlash");
        overlayObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayObject.AddComponent<EnemyHitFlashOverlayMarker>();

        SkinnedMeshRenderer overlayRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
        overlayRenderer.sharedMesh = sourceRenderer.sharedMesh;
        overlayRenderer.rootBone = sourceRenderer.rootBone;
        overlayRenderer.bones = sourceRenderer.bones;
        overlayRenderer.localBounds = sourceRenderer.localBounds;
        overlayRenderer.updateWhenOffscreen = true;
        overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
        overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        overlayRenderer.allowOcclusionWhenDynamic = false;
        overlayRenderer.enabled = false;
        overlayRenderer.sharedMaterials = CreateOverlayMaterialArray(sourceMaterials.Length);

        overlayTargets.Add(new OverlayTarget
        {
            Renderer = overlayRenderer,
            PropertyBlock = new MaterialPropertyBlock()
        });
    }

    private void TryCreateMeshOverlay(MeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer.GetComponent<EnemyHitFlashOverlayMarker>() != null)
        {
            return;
        }

        MeshFilter meshFilter = sourceRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        GameObject overlayObject = new GameObject(sourceRenderer.gameObject.name + "_HitFlash");
        overlayObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayObject.AddComponent<EnemyHitFlashOverlayMarker>();

        MeshFilter overlayFilter = overlayObject.AddComponent<MeshFilter>();
        overlayFilter.sharedMesh = meshFilter.sharedMesh;

        MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
        overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
        overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        overlayRenderer.allowOcclusionWhenDynamic = false;
        overlayRenderer.enabled = false;
        overlayRenderer.sharedMaterials = CreateOverlayMaterialArray(sourceMaterials.Length);

        overlayTargets.Add(new OverlayTarget
        {
            Renderer = overlayRenderer,
            PropertyBlock = new MaterialPropertyBlock()
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
        float clampedIntensity = Mathf.Clamp01(intensity) * Mathf.Clamp01(flashAlpha);
        Color overlayColor = flashColor;
        overlayColor.a *= clampedIntensity;

        for (int i = 0; i < overlayTargets.Count; i++)
        {
            OverlayTarget target = overlayTargets[i];
            if (target.Renderer == null)
            {
                continue;
            }

            bool visible = overlayColor.a > 0.001f;
            target.Renderer.enabled = visible;
            if (!visible)
            {
                continue;
            }

            MaterialPropertyBlock block = target.PropertyBlock;
            block.Clear();
            block.SetColor(BaseColorPropertyId, overlayColor);

            int materialCount = target.Renderer.sharedMaterials != null ? target.Renderer.sharedMaterials.Length : 0;
            if (materialCount <= 0)
            {
                target.Renderer.SetPropertyBlock(block);
                continue;
            }

            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                target.Renderer.SetPropertyBlock(block, materialIndex);
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

    private class OverlayTarget
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
    }
}

public class EnemyHitFlashOverlayMarker : MonoBehaviour
{
}
