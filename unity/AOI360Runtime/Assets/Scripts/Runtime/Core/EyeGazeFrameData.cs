using UnityEngine;

namespace EyeGaze.Runtime.Core
{
    // Immutable data package generated every frame by EyeGazeSystem
    // and consumed by the optional eye gaze modules.
    public readonly struct EyeGazeFrameData
    {
        // Whether tracking is currently valid this frame
        public readonly bool IsTracked;

        // Origin of the gaze ray in world space
        public readonly Vector3 GazeOrigin;

        // Rotation of the gaze pose in world space
        public readonly Quaternion GazeRotation;

        // Forward direction of the gaze ray in world space
        public readonly Vector3 GazeDirection;

        // Full gaze ray used for this frame
        public readonly Ray GazeRay;

        // Whether the physics raycast hit any collider in the configured hit mask
        public readonly bool HasHit;

        // Full physics raycast information if there was a hit
        public readonly RaycastHit HitInfo;

        // GameObject hit by the raycast, or null if nothing was hit
        public readonly GameObject HitObject;

        // Exact world point of the physics hit if there was one, or fallback/default point otherwise
        public readonly Vector3 HitPoint;

        // Surface normal of the physics hit if there was one, or fallback/default normal otherwise
        public readonly Vector3 HitNormal;

        // End point of the visible gaze ray for debug rendering
        public readonly Vector3 RayEndPoint;

        // Frame delta time used by helper modules
        public readonly float DeltaTime;

        // Whether a real physics hit happened this frame
        public readonly bool HasPhysicsHit;

        // World point that should be used by visual modules even if gaze does not hit an AOI
        public readonly Vector3 VisualFixationPoint;

        // Normal associated with the visual fixation point
        public readonly Vector3 VisualFixationNormal;

        // True if the visual fixation point is a fallback point generated in empty space
        public readonly bool IsFallbackFixationPoint;

        public EyeGazeFrameData(
            bool isTracked,
            Vector3 gazeOrigin,
            Quaternion gazeRotation,
            Vector3 gazeDirection,
            Ray gazeRay,
            bool hasHit,
            RaycastHit hitInfo,
            GameObject hitObject,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 rayEndPoint,
            float deltaTime,
            bool hasPhysicsHit,
            Vector3 visualFixationPoint,
            Vector3 visualFixationNormal,
            bool isFallbackFixationPoint
        )
        {
            IsTracked = isTracked;
            GazeOrigin = gazeOrigin;
            GazeRotation = gazeRotation;
            GazeDirection = gazeDirection;
            GazeRay = gazeRay;
            HasHit = hasHit;
            HitInfo = hitInfo;
            HitObject = hitObject;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            RayEndPoint = rayEndPoint;
            DeltaTime = deltaTime;
            HasPhysicsHit = hasPhysicsHit;
            VisualFixationPoint = visualFixationPoint;
            VisualFixationNormal = visualFixationNormal;
            IsFallbackFixationPoint = isFallbackFixationPoint;
        }
    }
}