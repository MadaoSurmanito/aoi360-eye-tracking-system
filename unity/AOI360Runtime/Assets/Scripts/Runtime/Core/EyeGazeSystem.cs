using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EyeGaze.Runtime.Core
{
    // This main module reads eye gaze data, performs the gaze raycast,
    // and delegates the result to the optional helper modules.
    public class EyeGazeSystem : MonoBehaviour
    {
        [Header("Raycast")]
        // Maximum distance for the gaze raycast
        [SerializeField] private float maxDistance = 10f;

        // Layer mask to specify which objects can be detected by the gaze raycast
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Fallback Visual Fixation")]
        // Distance used to place a visual fixation point when gaze does not hit anything
        [SerializeField] private float fallbackFixationDistance = 3f;

        // Clamp the visual fixation distance so very far hits do not produce exaggerated depth
        [SerializeField] private bool clampVisualFixationDistance = false;

        // Maximum allowed distance for the visual fixation point when clamping is enabled
        [SerializeField] private float maxVisualFixationDistance = 5f;

        [Header("References")]
        // Camera used as reference (usually HMD / Main Camera)
        [SerializeField] private Camera referenceCamera;

        [Header("Optional Modules")]
        // List of optional eye gaze modules to be driven by the system
        [SerializeField] private MonoBehaviour[] moduleBehaviours;

        // InputActions for eye gaze position, rotation and tracking state
        private InputAction gazePositionAction;
        private InputAction gazeRotationAction;
        private InputAction gazeTrackedAction;

        // Store the last valid gaze position and rotation
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation = Quaternion.identity;
        private bool hasValidGazePose;

        // Runtime list of valid modules implementing the common module interface
        private readonly List<IEyeGazeModule> modules = new();

        public Camera ReferenceCamera => referenceCamera;
        public float MaxDistance => maxDistance;
        public LayerMask HitMask => hitMask;
        public float FallbackFixationDistance => fallbackFixationDistance;
        public bool ClampVisualFixationDistance => clampVisualFixationDistance;
        public float MaxVisualFixationDistance => maxVisualFixationDistance;

        // Initialize InputActions and modules
        private void Awake()
        {
            CreateInputActions();
            ResolveReferenceCamera();
            CacheModules();
            InitializeModules();
        }

        // Enable InputActions
        private void OnEnable()
        {
            gazePositionAction.Enable();
            gazeRotationAction.Enable();
            gazeTrackedAction.Enable();
        }

        // Disable InputActions and clean state
        private void OnDisable()
        {
            gazePositionAction.Disable();
            gazeRotationAction.Disable();
            gazeTrackedAction.Disable();

            ResetAllModules();
        }

        // Main update loop
        private void Update()
        {
            ReadGazePose();

            if (!hasValidGazePose)
            {
                HandleInvalidTracking();
                return;
            }

            ProcessValidGaze();
        }

        // Create the InputActions used to read eye gaze data
        private void CreateInputActions()
        {
            gazePositionAction = new InputAction(
                name: "EyeGazePosition",
                type: InputActionType.Value,
                binding: "<EyeGaze>/pose/position"
            );

            gazeRotationAction = new InputAction(
                name: "EyeGazeRotation",
                type: InputActionType.Value,
                binding: "<EyeGaze>/pose/rotation"
            );

            gazeTrackedAction = new InputAction(
                name: "EyeGazeTracked",
                type: InputActionType.Value,
                binding: "<EyeGaze>/isTracked"
            );
        }

        // Use main camera if none assigned
        private void ResolveReferenceCamera()
        {
            if (referenceCamera == null)
            {
                referenceCamera = Camera.main;
            }
        }

        // Cache all assigned MonoBehaviours that implement IEyeGazeModule
        private void CacheModules()
        {
            modules.Clear();

            if (moduleBehaviours == null)
            {
                return;
            }

            foreach (MonoBehaviour behaviour in moduleBehaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is IEyeGazeModule module)
                {
                    modules.Add(module);
                }
                else
                {
                    Debug.LogWarning(
                        $"[EYE GAZE SYSTEM] Assigned behaviour '{behaviour.name}' does not implement IEyeGazeModule.",
                        behaviour
                    );
                }
            }
        }

        // Initialize all optional helper modules
        private void InitializeModules()
        {
            foreach (IEyeGazeModule module in modules)
            {
                module.Initialize(this);
            }
        }

        // Read the current eye gaze pose from Input System
        private void ReadGazePose()
        {
            Vector3 gazePosition = gazePositionAction.ReadValue<Vector3>();
            Quaternion gazeRotation = gazeRotationAction.ReadValue<Quaternion>();
            bool isTracked = gazeTrackedAction.ReadValue<float>() > 0.5f;

            hasValidGazePose = isTracked;

            if (hasValidGazePose)
            {
                lastValidPosition = gazePosition;
                lastValidRotation = gazeRotation;
            }
        }

        // Reset modules when the eye gaze is not currently tracked
        private void HandleInvalidTracking()
        {
            foreach (IEyeGazeModule module in modules)
            {
                module.HandleTrackingLost(Time.deltaTime);
            }
        }

        // Process the current valid eye gaze pose
        private void ProcessValidGaze()
        {
            Vector3 direction = lastValidRotation * Vector3.forward;
            Ray ray = new Ray(lastValidPosition, direction);

            bool hasHit = Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, hitMask);
            bool hasPhysicsHit = hasHit;

            GameObject hitObject = hasHit ? hitInfo.collider.gameObject : null;

            Vector3 hitPoint = hasHit
                ? hitInfo.point
                : lastValidPosition + direction * fallbackFixationDistance;

            Vector3 hitNormal = hasHit
                ? hitInfo.normal
                : -direction;

            Vector3 visualFixationPoint;
            Vector3 visualFixationNormal;
            bool isFallbackFixationPoint;

            if (hasPhysicsHit)
            {
                visualFixationPoint = hitInfo.point;
                visualFixationNormal = hitInfo.normal.sqrMagnitude > 0f
                    ? hitInfo.normal.normalized
                    : -direction;
                isFallbackFixationPoint = false;

                if (clampVisualFixationDistance)
                {
                    float hitDistance = Vector3.Distance(lastValidPosition, visualFixationPoint);

                    if (hitDistance > maxVisualFixationDistance)
                    {
                        visualFixationPoint = lastValidPosition + direction * maxVisualFixationDistance;
                        visualFixationNormal = -direction;
                        isFallbackFixationPoint = true;
                    }
                }
            }
            else
            {
                visualFixationPoint = lastValidPosition + direction * fallbackFixationDistance;
                visualFixationNormal = -direction;
                isFallbackFixationPoint = true;
            }

            Vector3 rayEndPoint = hasPhysicsHit
                ? visualFixationPoint
                : lastValidPosition + direction * maxDistance;

            EyeGazeFrameData frameData = new EyeGazeFrameData(
                isTracked: true,
                gazeOrigin: lastValidPosition,
                gazeRotation: lastValidRotation,
                gazeDirection: direction,
                gazeRay: ray,
                hasHit: hasHit,
                hitInfo: hitInfo,
                hitObject: hitObject,
                hitPoint: hitPoint,
                hitNormal: hitNormal,
                rayEndPoint: rayEndPoint,
                deltaTime: Time.deltaTime,
                hasPhysicsHit: hasPhysicsHit,
                visualFixationPoint: visualFixationPoint,
                visualFixationNormal: visualFixationNormal,
                isFallbackFixationPoint: isFallbackFixationPoint
            );

            foreach (IEyeGazeModule module in modules)
            {
                module.ProcessFrame(frameData);
            }
        }

        // Reset the internal state of all optional modules
        private void ResetAllModules()
        {
            foreach (IEyeGazeModule module in modules)
            {
                module.ResetModuleState();
            }
        }
    }
}