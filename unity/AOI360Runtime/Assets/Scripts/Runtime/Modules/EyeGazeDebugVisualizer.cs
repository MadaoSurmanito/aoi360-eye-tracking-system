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

        // Color of the offset visualization between camera and gaze origin
        [SerializeField] private Color debugOffsetLineColor = Color.white;

        // Enables or disables periodic debug logs comparing the gaze origin and the camera position
        [SerializeField] private bool enableDebugLogs = false;

        // Number of frames between each debug log when enableDebugLogs is active
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
        }

        // Called every frame when valid gaze data is available.
        public override void ProcessFrame(EyeGazeFrameData frameData)
        {
            UpdateVisualization(frameData.GazeOrigin, frameData.GazeDirection, frameData.RayEndPoint);
        }

        // Called when tracking is lost or invalid gaze data must be handled.
        public override void HandleTrackingLost(float deltaTime)
        {
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
            debugOffsetLineRenderer.SetPosition(0, referenceCamera.transform.position);
            debugOffsetLineRenderer.SetPosition(1, gazeOrigin);
        }

        // Periodically log the gaze origin, the camera position, and the difference between them for debugging alignment issues
        private void WritePeriodicDebugLog(Vector3 gazeOrigin, Vector3 gazeDirection)
        {
            if (!enableDebugLogs || referenceCamera == null || debugLogEveryNFrames <= 0)
            {
                return;
            }

            if (Time.frameCount % debugLogEveryNFrames != 0)
            {
                return;
            }

            Vector3 cameraPosition = referenceCamera.transform.position;
            Vector3 offset = gazeOrigin - cameraPosition;

            Debug.Log(
                $"[EYE DEBUG] " +
                $"GazeOrigin={gazeOrigin} | " +
                $"CameraPosition={cameraPosition} | " +
                $"Offset={offset} | " +
                $"OffsetMagnitude={offset.magnitude} | " +
                $"Direction={gazeDirection}"
            );
        }

        // Enable or disable a LineRenderer safely
        private void SetLineRendererEnabled(LineRenderer lineRenderer, bool value)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = value;
            }
        }
    }
}