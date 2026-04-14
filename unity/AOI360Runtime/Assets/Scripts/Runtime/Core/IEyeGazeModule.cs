using UnityEngine;

namespace EyeGaze.Runtime.Core
{
    // Common contract implemented by every eye gaze helper module.
    public interface IEyeGazeModule
    {
        // Called once by the main system during initialization.
        void Initialize(EyeGazeSystem system);

        // Called every frame when valid gaze data is available.
        void ProcessFrame(EyeGazeFrameData frameData);

        // Called when tracking is lost or invalid gaze data must be handled.
        void HandleTrackingLost(float deltaTime);

        // Called when the main system is disabled and the module should clear transient state.
        void ResetModuleState();
    }
}