using UnityEngine;

namespace EyeGaze.Runtime.Core
{
    // Optional base class that reduces boilerplate for eye gaze modules.
    public abstract class EyeGazeModuleBase : MonoBehaviour, IEyeGazeModule
    {
        protected EyeGazeSystem system;

        // Called once by the main system during initialization.
        public virtual void Initialize(EyeGazeSystem systemReference)
        {
            system = systemReference;
        }

        // Called every frame when valid gaze data is available.
        public abstract void ProcessFrame(EyeGazeFrameData frameData);

        // Called when tracking is lost or invalid gaze data must be handled.
        public virtual void HandleTrackingLost(float deltaTime)
        {
        }

        // Called when the main system is disabled and the module should clear transient state.
        public virtual void ResetModuleState()
        {
        }
    }
}