using AOI360.Runtime.Mapping;
using EyeGaze.Runtime.Core;
using UnityEngine;

namespace AOI360.Runtime.Mapping
{
    public class GazeProviderBridge : EyeGazeModuleBase
    {
        [Header("References")]
        [SerializeField] private SphericalMapper sphericalMapper;

        [Header("Debug")]
        [SerializeField] private bool logTrackingChanges = true;
        [SerializeField] private bool logEveryNFrames = false;
        [SerializeField] private int logFrameInterval = 60;

        private bool lastTrackingState;

        public override void Initialize(EyeGazeSystem systemReference)
        {
            base.Initialize(systemReference);

            if (sphericalMapper == null)
            {
                Debug.LogWarning("[GazeProviderBridge] No SphericalMapper assigned.", this);
            }
        }

        public override void ProcessFrame(EyeGazeFrameData frameData)
        {
            if (sphericalMapper == null)
            {
                return;
            }

            sphericalMapper.SetExternalGazeDirection(frameData.GazeDirection, true);

            LogTrackingStateIfNeeded(true);

            if (logEveryNFrames && Time.frameCount % Mathf.Max(1, logFrameInterval) == 0)
            {
                Debug.Log(
                    $"[GazeProviderBridge] dir={frameData.GazeDirection} | " +
                    $"origin={frameData.GazeOrigin} | tracked={frameData.IsTracked}"
                );
            }
        }

        public override void HandleTrackingLost(float deltaTime)
        {
            if (sphericalMapper == null)
            {
                return;
            }

            sphericalMapper.ClearExternalGazeDirection();
            LogTrackingStateIfNeeded(false);
        }

        public override void ResetModuleState()
        {
            if (sphericalMapper == null)
            {
                return;
            }

            sphericalMapper.ClearExternalGazeDirection();
            lastTrackingState = false;
        }

        private void LogTrackingStateIfNeeded(bool isTracked)
        {
            if (!logTrackingChanges || lastTrackingState == isTracked)
            {
                return;
            }

            lastTrackingState = isTracked;
            Debug.Log($"[GazeProviderBridge] Tracking changed -> {isTracked}", this);
        }
    }
}