using AOI360.Runtime.AOI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AOI360.Runtime.Core
{
    [DefaultExecutionOrder(-200)]
    public class Phase0Bootstrap : MonoBehaviour
    {
        private const string TargetSceneName = "Phase0_360Playback_VR";
        private const string RuntimeBootstrapName = "Phase0Bootstrap_Runtime";

        [Header("Overlay")]
        [SerializeField] private bool createAoiOverlay = true;
        [SerializeField] private float overlaySphereRadius = 4.98f;
        [SerializeField] private float overlayOpacity = 0.24f;
        [SerializeField] private float focusedOverlayOpacity = 0.6f;

        private AOILookup aoiLookup;
        private Transform sphereCenter;
        private GameObject overlaySphere;
        private Texture2D overlayTexture;
        private Material overlayMaterial;
        private int lastHighlightedAoiId = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != TargetSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<Phase0Bootstrap>() != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject(RuntimeBootstrapName);
            bootstrap.AddComponent<Phase0Bootstrap>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName)
            {
                enabled = false;
                return;
            }

            ResolveReferences();
            EnsureOverlaySphere();
            RefreshOverlayTexture(forceRefresh: true);
        }

        private void Update()
        {
            if (!createAoiOverlay || aoiLookup == null || overlayMaterial == null)
            {
                return;
            }

            if (aoiLookup.CurrentAOIId != lastHighlightedAoiId)
            {
                RefreshOverlayTexture(forceRefresh: false);
            }
        }

        private void ResolveReferences()
        {
            if (aoiLookup == null)
            {
                aoiLookup = FindFirstObjectByType<AOILookup>();
            }

            if (sphereCenter == null)
            {
                GameObject center = GameObject.Find("SphereCenter");
                sphereCenter = center != null ? center.transform : null;
            }
        }

        private void EnsureOverlaySphere()
        {
            if (!createAoiOverlay || aoiLookup == null || aoiLookup.AOIMapTexture == null)
            {
                return;
            }

            if (overlaySphere != null)
            {
                return;
            }

            Shader overlayShader = ResolveTransparentShader();
            if (overlayShader == null)
            {
                Debug.LogWarning("[Phase0Bootstrap] Could not find a transparent runtime shader for AOI overlay.");
                return;
            }

            overlayMaterial = new Material(overlayShader);
            overlayMaterial.name = "Runtime_AOIOverlay";
            ConfigureTransparentMaterial(overlayMaterial, null, Color.white);

            overlaySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            overlaySphere.name = "AOIOverlaySphere";
            overlaySphere.transform.SetParent(sphereCenter, false);
            overlaySphere.transform.localPosition = Vector3.zero;
            overlaySphere.transform.localRotation = Quaternion.identity;
            overlaySphere.transform.localScale = new Vector3(
                overlaySphereRadius * 2f,
                overlaySphereRadius * 2f,
                overlaySphereRadius * 2f
            );

            Collider overlayCollider = overlaySphere.GetComponent<Collider>();
            if (overlayCollider != null)
            {
                Destroy(overlayCollider);
            }

            MeshRenderer overlayRenderer = overlaySphere.GetComponent<MeshRenderer>();
            if (overlayRenderer != null)
            {
                overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;
                overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
                overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                overlayRenderer.material = overlayMaterial;
            }

            MeshFilter overlayFilter = overlaySphere.GetComponent<MeshFilter>();
            if (overlayFilter != null && overlayFilter.sharedMesh != null)
            {
                overlayFilter.sharedMesh = CreateInvertedSphereMesh(overlayFilter.sharedMesh);
            }
        }

        private void RefreshOverlayTexture(bool forceRefresh)
        {
            Texture2D sourceTexture = aoiLookup != null ? aoiLookup.AOIMapTexture : null;
            if (sourceTexture == null)
            {
                return;
            }

            int highlightedAoiId = aoiLookup.CurrentAOIId;
            if (!forceRefresh && highlightedAoiId == lastHighlightedAoiId)
            {
                return;
            }

            lastHighlightedAoiId = highlightedAoiId;

            if (overlayTexture == null || overlayTexture.width != sourceTexture.width || overlayTexture.height != sourceTexture.height)
            {
                overlayTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
                overlayTexture.name = "Runtime_AOIOverlayTexture";
                overlayTexture.wrapMode = TextureWrapMode.Repeat;
                overlayTexture.filterMode = FilterMode.Bilinear;
            }

            Color[] sourcePixels = sourceTexture.GetPixels();
            Color[] overlayPixels = new Color[sourcePixels.Length];

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color sourcePixel = sourcePixels[i];
                int aoiId = ResolveAoiIdFromPixel(sourcePixel);

                if (aoiId <= 0)
                {
                    overlayPixels[i] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                Color overlayColor = ResolveOverlayColor(sourcePixel, aoiId);
                float alpha = aoiId == highlightedAoiId ? focusedOverlayOpacity : overlayOpacity;
                overlayColor.a = alpha;
                overlayPixels[i] = overlayColor;
            }

            overlayTexture.SetPixels(overlayPixels);
            overlayTexture.Apply(false, false);
            ConfigureTransparentMaterial(overlayMaterial, overlayTexture, Color.white);
        }

        private int ResolveAoiIdFromPixel(Color pixel)
        {
            int grayscaleId = Mathf.Clamp(Mathf.RoundToInt(pixel.r * 255f), 0, 255);
            if (grayscaleId > 0 && Mathf.Abs(pixel.r - pixel.g) < 0.01f && Mathf.Abs(pixel.g - pixel.b) < 0.01f)
            {
                return grayscaleId;
            }

            if (pixel.r < 0.1f && pixel.g < 0.1f && pixel.b < 0.1f)
            {
                return 0;
            }

            if (pixel.r > pixel.g && pixel.r > pixel.b)
            {
                return 1;
            }

            if (pixel.g > pixel.r && pixel.g > pixel.b)
            {
                return 2;
            }

            return grayscaleId;
        }

        private Color ResolveOverlayColor(Color sourcePixel, int aoiId)
        {
            if (Mathf.Abs(sourcePixel.r - sourcePixel.g) < 0.01f && Mathf.Abs(sourcePixel.g - sourcePixel.b) < 0.01f)
            {
                float hue = Mathf.Repeat(aoiId * 0.173f, 1f);
                return Color.HSVToRGB(hue, 0.8f, 1f);
            }

            return new Color(sourcePixel.r, sourcePixel.g, sourcePixel.b, 1f);
        }

        private Mesh CreateInvertedSphereMesh(Mesh sourceMesh)
        {
            Mesh invertedMesh = Instantiate(sourceMesh);
            invertedMesh.name = $"{sourceMesh.name}_Inverted";

            int[] triangles = invertedMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int tmp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = tmp;
            }

            invertedMesh.triangles = triangles;

            Vector3[] normals = invertedMesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }

            invertedMesh.normals = normals;
            invertedMesh.RecalculateBounds();
            return invertedMesh;
        }

        private Shader ResolveTransparentShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Unlit/Transparent");
        }

        private void ConfigureTransparentMaterial(Material material, Texture texture, Color color)
        {
            material.mainTexture = texture;
            material.color = color;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
