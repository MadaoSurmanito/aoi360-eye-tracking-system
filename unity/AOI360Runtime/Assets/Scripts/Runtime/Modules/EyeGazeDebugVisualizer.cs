using AOI360.Runtime.AOI;
using AOI360.Runtime.Mapping;
using EyeGaze.Runtime.Core;
using UnityEngine;

namespace EyeGaze.Runtime.Modules
{
    // This helper module is responsible only for debug visualization and debug logging.
    public class EyeGazeDebugVisualizer : EyeGazeModuleBase
    {
        [Header("Debug")]
        // Enables or disables all debug line visualizations
        [SerializeField] private bool enableDebugRay = false;

        // Optional LineRenderer to visualize the eye gaze ray in build and in VR
        [SerializeField] private LineRenderer debugLineRenderer;

        // Color of the eye gaze debug ray visualization
        [SerializeField] private Color debugRayColor = Color.red;

        // Optional LineRenderer to visualize the forward direction of the reference camera
        [SerializeField] private LineRenderer debugCameraLineRenderer;

        // Color of the camera debug ray visualization
        [SerializeField] private Color debugCameraRayColor = Color.blue;

        // Optional LineRenderer to visualize the offset between the camera position and the gaze origin
        [SerializeField] private LineRenderer debugOffsetLineRenderer;

        // Color of the offset visualization between the camera position and the gaze origin
        [SerializeField] private Color debugOffsetLineColor = Color.white;

        [Header("Fallback")]
        // When tracking is lost, keep showing a debug ray using the reference camera forward
        [SerializeField] private bool showFallbackWhenTrackingLost = true;

        [Header("360 Debug")]
        // Optional mapper used to inspect current UV values
        [SerializeField] private SphericalMapper sphericalMapper;

        // Optional AOI lookup used to inspect which AOI is currently being looked at
        [SerializeField] private AOILookup aoiLookup;

        // Center transform of the 360 sphere
        [SerializeField] private Transform sphereCenter;

        // Radius of the 360 sphere
        [SerializeField] private float sphereRadius = 5f;

        // Optional marker placed on the hit point over the 360 sphere
        [SerializeField] private Transform hitMarker;

        // Enables or disables the hit marker visualization
        [SerializeField] private bool enableHitMarker = true;

        // Enables or disables periodic logs about UV and AOI data
        [SerializeField] private bool enableAOILogging = true;

        [Header("Logs")]
        // Enables or disables periodic debug logs comparing the gaze origin and the camera position
        [SerializeField] private bool enableDebugLogs = false;

        // Number of frames between each debug log when logging is active
        [SerializeField] private int debugLogEveryNFrames = 60;

        private Camera referenceCamera;
        private float maxDistance;

        // Called once by the main system during initialization.
        public override void Initialize(EyeGazeSystem systemReference)
        {
            base.Initialize(systemReference);

            referenceCamera = system.ReferenceCamera;
            maxDistance = system.MaxDistance;

            ConfigureAllLineRenderers();
            SetHitMarkerEnabled(false);
        }

        // Called every frame when valid gaze data is available.
        public override void ProcessFrame(EyeGazeFrameData frameData)
        {
            UpdateVisualization(frameData.GazeOrigin, frameData.GazeDirection, frameData.RayEndPoint);
        }

        // Called when tracking is lost or invalid gaze data must be handled.
        public override void HandleTrackingLost(float deltaTime)
        {
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

                UpdateVisualization(fallbackOrigin, fallbackDirection, fallbackEndPoint);
                return;
            }

            DisableAll();
        }

        // Called when the main system is disabled and the module should clear transient state.
        public override void ResetModuleState()
        {
            DisableAll();
        }

        // Disable all debug visuals
        public void DisableAll()
        {
            SetLineRendererEnabled(debugLineRenderer, false);
            SetLineRendererEnabled(debugCameraLineRenderer, false);
            SetLineRendererEnabled(debugOffsetLineRenderer, false);
            SetHitMarkerEnabled(false);
        }

        // Update all debug visuals and logs using the latest gaze data
        public void UpdateVisualization(Vector3 gazeOrigin, Vector3 gazeDirection, Vector3 gazeEndPoint)
        {
            if (!enableDebugRay)
            {
                DisableAll();
                return;
            }

            DrawGazeRay(gazeOrigin, gazeEndPoint);
            DrawReferenceCameraRay();
            DrawCameraToGazeOffset(gazeOrigin);
            UpdateSphereHitMarker(gazeOrigin, gazeDirection);
            WritePeriodicDebugLog(gazeOrigin, gazeDirection);
        }

        // Configure every optional LineRenderer used by this module
        private void ConfigureAllLineRenderers()
        {
            EyeGazeUtils.ConfigureLineRenderer(debugLineRenderer, debugRayColor, enableDebugRay);
            EyeGazeUtils.ConfigureLineRenderer(debugCameraLineRenderer, debugCameraRayColor, enableDebugRay);
            EyeGazeUtils.ConfigureLineRenderer(debugOffsetLineRenderer, debugOffsetLineColor, enableDebugRay);
        }

        // Draw the eye gaze ray
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

        // Draw the camera forward ray for comparison
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

        // Draw the offset line between the camera position and the gaze origin
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

        // Place a marker where the gaze ray intersects the 360 sphere
        private void UpdateSphereHitMarker(Vector3 gazeOrigin, Vector3 gazeDirection)
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
                SetHitMarkerEnabled(false);
                return;
            }

            SetHitMarkerEnabled(true);
            hitMarker.position = hitPoint;

            Vector3 outwardNormal = (hitPoint - sphereCenter.position).normalized;
            hitMarker.rotation = Quaternion.LookRotation(outwardNormal);
        }

        // Periodically log the gaze origin, camera position, UV and AOI data
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
                aoiInfo = $" | AOI={GetAOIDebugText()}";
            }

            Debug.Log($"[EYE DEBUG] {cameraInfo}{mapperInfo}{aoiInfo}");
        }

        private string GetAOIDebugText()
        {
            if (aoiLookup == null)
            {
                return "N/A";
            }

            return aoiLookup.CurrentAOIId.ToString();
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
    }
}