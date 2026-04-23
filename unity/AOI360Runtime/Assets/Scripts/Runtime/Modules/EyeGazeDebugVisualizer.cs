using AOI360.Runtime.AOI;
using AOI360.Runtime.Mapping;
using EyeGaze.Runtime.Core;
using UnityEngine;

namespace EyeGaze.Runtime.Modules
{
    // Debug visualization plus a lightweight fixation detector for the Phase 0 runtime.
    public class EyeGazeDebugVisualizer : EyeGazeModuleBase
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugRay = false;
        [SerializeField] private LineRenderer debugLineRenderer;
        [SerializeField] private Color debugRayColor = Color.red;
        [SerializeField] private LineRenderer debugCameraLineRenderer;
        [SerializeField] private Color debugCameraRayColor = Color.blue;
        [SerializeField] private LineRenderer debugOffsetLineRenderer;
        [SerializeField] private Color debugOffsetLineColor = Color.white;

        [Header("Fallback")]
        [SerializeField] private bool showFallbackWhenTrackingLost = true;

        [Header("360 Debug")]
        [SerializeField] private SphericalMapper sphericalMapper;
        [SerializeField] private AOILookup aoiLookup;
        [SerializeField] private Transform sphereCenter;
        [SerializeField] private float sphereRadius = 5f;
        [SerializeField] private Transform hitMarker;
        [SerializeField] private bool enableHitMarker = true;
        [SerializeField] private bool enableAOILogging = true;

        [Header("Fixations")]
        [SerializeField] private float fixationCommitIntervalSeconds = 0.25f;
        [SerializeField] private float fixationAngularThresholdDegrees = 3f;
        [SerializeField] private float fixationMarkerBaseScale = 0.08f;
        [SerializeField] private float fixationMarkerScaleGrowth = 0.2f;
        [SerializeField] private float fixationMarkerMaxScale = 0.22f;

        [Header("Logs")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private int debugLogEveryNFrames = 60;

        private Camera referenceCamera;
        private float maxDistance;
        private Renderer hitMarkerRenderer;
        private Transform hitMarkerVisual;
        private Material runtimeHitMarkerMaterial;
        private Texture hitMarkerTexture;
        private Vector3 hitMarkerInitialScale = Vector3.one;
        private float fixationCandidateStartTimestampMs;

        private bool fixationCandidateValid;
        private Vector3 fixationCandidateDirection = Vector3.forward;
        private Vector3 fixationAnchorPoint = Vector3.zero;
        private Vector3 fixationAnchorNormal = Vector3.forward;
        private float fixationCandidateDuration;
        private int fixationCommitCount;
        private int fixationSequence;

        public bool HasCommittedFixation { get; private set; }
        public int LatestCommittedFixationSequence { get; private set; }
        public float LatestCommittedFixationTimestampMs { get; private set; }
        public Vector3 LatestCommittedFixationPoint { get; private set; }
        public Vector3 LatestCommittedFixationNormal { get; private set; }
        public Vector2 LatestCommittedFixationUv { get; private set; }
        public int LatestCommittedFixationAoiId { get; private set; }
        public float LatestCommittedFixationConfidence { get; private set; }
        public int ActiveFixationCommitCount => fixationCommitCount;

        public override void Initialize(EyeGazeSystem systemReference)
        {
            base.Initialize(systemReference);

            referenceCamera = system.ReferenceCamera;
            maxDistance = system.MaxDistance;

            ConfigureAllLineRenderers();
            CacheHitMarkerRenderer();
            ResetFixationState();
            SetHitMarkerEnabled(false);
        }

        public override void ProcessFrame(EyeGazeFrameData frameData)
        {
            UpdateVisualization(frameData.GazeOrigin, frameData.GazeDirection, frameData.RayEndPoint, frameData.DeltaTime);
        }

        public override void HandleTrackingLost(float deltaTime)
        {
            ResetFixationState();

            if (!enableDebugRay)
            {
                DisableAll();
                return;
            }

            if (showFallbackWhenTrackingLost && referenceCamera != null)
            {
                Vector3 fallbackOrigin = referenceCamera.transform.position;
                Vector3 fallbackDirection = referenceCamera.transform.forward.normalized;
                Vector3 fallbackEndPoint = fallbackOrigin + fallbackDirection * maxDistance;

                DrawGazeRay(fallbackOrigin, fallbackEndPoint);
                DrawReferenceCameraRay();
                DrawCameraToGazeOffset(fallbackOrigin);
                SetHitMarkerEnabled(false);
                return;
            }

            DisableAll();
        }

        public override void ResetModuleState()
        {
            ResetFixationState();
            DisableAll();
        }

        public void DisableAll()
        {
            SetLineRendererEnabled(debugLineRenderer, false);
            SetLineRendererEnabled(debugCameraLineRenderer, false);
            SetLineRendererEnabled(debugOffsetLineRenderer, false);
            SetHitMarkerEnabled(false);
        }

        public void UpdateVisualization(Vector3 gazeOrigin, Vector3 gazeDirection, Vector3 gazeEndPoint, float deltaTime)
        {
            if (!enableDebugRay)
            {
                DisableAll();
                return;
            }

            DrawGazeRay(gazeOrigin, gazeEndPoint);
            DrawReferenceCameraRay();
            DrawCameraToGazeOffset(gazeOrigin);
            UpdateSphereHitMarker(gazeOrigin, gazeDirection, deltaTime);
            WritePeriodicDebugLog(gazeOrigin, gazeDirection);
        }

        private void ConfigureAllLineRenderers()
        {
            EyeGazeUtils.ConfigureLineRenderer(debugLineRenderer, debugRayColor, enableDebugRay);
            EyeGazeUtils.ConfigureLineRenderer(debugCameraLineRenderer, debugCameraRayColor, enableDebugRay);
            EyeGazeUtils.ConfigureLineRenderer(debugOffsetLineRenderer, debugOffsetLineColor, enableDebugRay);
        }

        private void CacheHitMarkerRenderer()
        {
            if (hitMarker == null)
            {
                return;
            }

            hitMarkerRenderer = hitMarker.GetComponentInChildren<Renderer>(true);

            if (hitMarkerRenderer == null)
            {
                return;
            }

            hitMarkerVisual = hitMarkerRenderer.transform;
            hitMarkerInitialScale = hitMarkerVisual.localScale;
            hitMarkerTexture = hitMarkerRenderer.sharedMaterial != null
                ? hitMarkerRenderer.sharedMaterial.mainTexture
                : null;
            hitMarkerVisual.localPosition = new Vector3(0f, 0f, -0.01f);

            Shader markerShader = ResolveTransparentShader();
            if (markerShader == null)
            {
                Debug.LogWarning("[EyeGazeDebugVisualizer] Could not find a transparent runtime shader for hit markers.");
                return;
            }

            runtimeHitMarkerMaterial = new Material(markerShader);
            runtimeHitMarkerMaterial.name = "Runtime_HitMarker";
            ConfigureTransparentMaterial(runtimeHitMarkerMaterial, hitMarkerTexture, Color.white);
            hitMarkerRenderer.material = runtimeHitMarkerMaterial;
            hitMarkerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hitMarkerRenderer.receiveShadows = false;
        }

        private void DrawGazeRay(Vector3 gazeOrigin, Vector3 gazeEndPoint)
        {
            if (debugLineRenderer == null)
            {
                return;
            }

            debugLineRenderer.enabled = true;
            debugLineRenderer.positionCount = 2;
            debugLineRenderer.SetPosition(0, gazeOrigin);
            debugLineRenderer.SetPosition(1, gazeEndPoint);
        }

        private void DrawReferenceCameraRay()
        {
            if (debugCameraLineRenderer == null || referenceCamera == null)
            {
                return;
            }

            Vector3 cameraStart = referenceCamera.transform.position;
            Vector3 cameraEnd = cameraStart + referenceCamera.transform.forward * maxDistance;

            debugCameraLineRenderer.enabled = true;
            debugCameraLineRenderer.positionCount = 2;
            debugCameraLineRenderer.SetPosition(0, cameraStart);
            debugCameraLineRenderer.SetPosition(1, cameraEnd);
        }

        private void DrawCameraToGazeOffset(Vector3 gazeOrigin)
        {
            if (debugOffsetLineRenderer == null || referenceCamera == null)
            {
                return;
            }

            debugOffsetLineRenderer.enabled = true;
            debugOffsetLineRenderer.positionCount = 2;
            debugOffsetLineRenderer.SetPosition(0, referenceCamera.transform.position);
            debugOffsetLineRenderer.SetPosition(1, gazeOrigin);
        }

        private void UpdateSphereHitMarker(Vector3 gazeOrigin, Vector3 gazeDirection, float deltaTime)
        {
            if (!enableHitMarker || sphereCenter == null || hitMarker == null)
            {
                SetHitMarkerEnabled(false);
                return;
            }

            if (!TryIntersectRaySphere(
                gazeOrigin,
                gazeDirection.normalized,
                sphereCenter.position,
                sphereRadius,
                out Vector3 hitPoint))
            {
                ResetFixationState();
                SetHitMarkerEnabled(false);
                return;
            }

            Vector3 outwardNormal = (hitPoint - sphereCenter.position).normalized;
            Vector3 inwardDirection = -outwardNormal;
            UpdateFixationState(hitPoint, outwardNormal, inwardDirection, deltaTime);
        }

        private void UpdateFixationState(Vector3 hitPoint, Vector3 outwardNormal, Vector3 inwardDirection, float deltaTime)
        {
            if (!fixationCandidateValid)
            {
                fixationCandidateValid = true;
                fixationCandidateDirection = inwardDirection;
                fixationAnchorPoint = hitPoint;
                fixationAnchorNormal = outwardNormal;
                fixationCandidateStartTimestampMs = Time.time * 1000f;
                fixationCandidateDuration = deltaTime;
                fixationCommitCount = 0;
                SetHitMarkerEnabled(false);
                return;
            }

            float angularDelta = Vector3.Angle(fixationCandidateDirection, inwardDirection);
            if (angularDelta > fixationAngularThresholdDegrees)
            {
                fixationCandidateDirection = inwardDirection;
                fixationAnchorPoint = hitPoint;
                fixationAnchorNormal = outwardNormal;
                fixationCandidateStartTimestampMs = Time.time * 1000f;
                fixationCandidateDuration = deltaTime;
                fixationCommitCount = 0;
                SetHitMarkerEnabled(false);
                return;
            }

            fixationCandidateDuration += deltaTime;
            int targetCommitCount = Mathf.FloorToInt(fixationCandidateDuration / fixationCommitIntervalSeconds);
            while (targetCommitCount > fixationCommitCount)
            {
                CommitFixation();
            }

            if (fixationCommitCount <= 0)
            {
                SetHitMarkerEnabled(false);
                return;
            }

            SetHitMarkerEnabled(true);
            hitMarker.position = fixationAnchorPoint;
            hitMarker.rotation = Quaternion.LookRotation(fixationAnchorNormal);

            float scaleMultiplier = 1f + (fixationCommitCount - 1) * fixationMarkerScaleGrowth;
            float clampedScale = Mathf.Min(fixationMarkerBaseScale * scaleMultiplier, fixationMarkerMaxScale);
            if (hitMarkerVisual != null)
            {
                hitMarkerVisual.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
            }

            if (runtimeHitMarkerMaterial != null)
            {
                runtimeHitMarkerMaterial.color = ResolveFixationColor();
            }
        }

        private void CommitFixation()
        {
            fixationCommitCount++;
            fixationSequence++;
            HasCommittedFixation = true;
            LatestCommittedFixationSequence = fixationSequence;
            LatestCommittedFixationTimestampMs = fixationCandidateStartTimestampMs + (fixationCommitCount * fixationCommitIntervalSeconds * 1000f);
            LatestCommittedFixationPoint = fixationAnchorPoint;
            LatestCommittedFixationNormal = fixationAnchorNormal;
            LatestCommittedFixationUv = sphericalMapper != null ? sphericalMapper.CurrentUV : Vector2.zero;
            LatestCommittedFixationAoiId = aoiLookup != null ? aoiLookup.CurrentAOIId : 0;
            LatestCommittedFixationConfidence = aoiLookup != null ? aoiLookup.CurrentAOIConfidence : 0f;
        }

        private Color ResolveFixationColor()
        {
            if (aoiLookup == null)
            {
                return Color.white;
            }

            Color baseColor = aoiLookup.CurrentAOIId > 0 ? aoiLookup.CurrentAOIColor : Color.white;
            baseColor.a = 1f;
            return Color.Lerp(baseColor, Color.white, 1f - Mathf.Clamp01(aoiLookup.CurrentAOIConfidence));
        }

        private void ResetFixationState()
        {
            fixationCandidateValid = false;
            fixationCandidateDirection = Vector3.forward;
            fixationAnchorPoint = Vector3.zero;
            fixationAnchorNormal = Vector3.forward;
            fixationCandidateStartTimestampMs = 0f;
            fixationCandidateDuration = 0f;
            fixationCommitCount = 0;

            if (hitMarkerVisual != null)
            {
                hitMarkerVisual.localScale = hitMarkerInitialScale;
            }
        }

        private void WritePeriodicDebugLog(Vector3 gazeOrigin, Vector3 gazeDirection)
        {
            if ((!enableDebugLogs && !enableAOILogging) || debugLogEveryNFrames <= 0)
            {
                return;
            }

            if (Time.frameCount % debugLogEveryNFrames != 0)
            {
                return;
            }

            string cameraInfo = "";
            if (enableDebugLogs && referenceCamera != null)
            {
                Vector3 cameraPosition = referenceCamera.transform.position;
                Vector3 offset = gazeOrigin - cameraPosition;

                cameraInfo =
                    $"GazeOrigin={gazeOrigin} | " +
                    $"CameraPosition={cameraPosition} | " +
                    $"Offset={offset} | " +
                    $"OffsetMagnitude={offset.magnitude:F4} | " +
                    $"Direction={gazeDirection}";
            }

            string mapperInfo = "";
            if (enableAOILogging && sphericalMapper != null && sphericalMapper.HasValidDirection)
            {
                mapperInfo =
                    $" | UV=({sphericalMapper.CurrentUV.x:F3}, {sphericalMapper.CurrentUV.y:F3})" +
                    $" | Azimuth={sphericalMapper.CurrentAzimuthRad:F3}" +
                    $" | Elevation={sphericalMapper.CurrentElevationRad:F3}";
            }

            string aoiInfo = "";
            if (enableAOILogging && aoiLookup != null)
            {
                aoiInfo =
                    $" | AOI={aoiLookup.CurrentAOIId}" +
                    $" | Conf={aoiLookup.CurrentAOIConfidence:F2}" +
                    $" | FixSteps={fixationCommitCount}";
            }

            Debug.Log($"[EYE DEBUG] {cameraInfo}{mapperInfo}{aoiInfo}");
        }

        private bool TryIntersectRaySphere(
            Vector3 rayOrigin,
            Vector3 rayDirection,
            Vector3 sphereCenterWorld,
            float radius,
            out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            Vector3 oc = rayOrigin - sphereCenterWorld;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - (radius * radius);
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
            {
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtDiscriminant) / (2f * a);
            float t2 = (-b + sqrtDiscriminant) / (2f * a);
            float t = -1f;

            if (t1 > 0f && t2 > 0f)
            {
                t = Mathf.Min(t1, t2);
            }
            else if (t1 > 0f)
            {
                t = t1;
            }
            else if (t2 > 0f)
            {
                t = t2;
            }

            if (t <= 0f)
            {
                return false;
            }

            hitPoint = rayOrigin + rayDirection * t;
            return true;
        }

        private void SetLineRendererEnabled(LineRenderer lineRenderer, bool value)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = value;
            }
        }

        private void SetHitMarkerEnabled(bool value)
        {
            if (hitMarker != null)
            {
                hitMarker.gameObject.SetActive(value);
            }
        }

        private Shader ResolveTransparentShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Unlit/Transparent");
            return shader;
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
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
