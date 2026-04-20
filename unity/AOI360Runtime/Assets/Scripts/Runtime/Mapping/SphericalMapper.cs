using UnityEngine;

namespace AOI360.Runtime.Mapping
{
    public class SphericalMapper : MonoBehaviour
    {
        public enum GazeInputMode
        {
            Auto,
            TransformForwardOnly,
            ExternalDirectionOnly
        }

        [Header("Gaze source")]
        [SerializeField] private GazeInputMode inputMode = GazeInputMode.Auto;
        [SerializeField] private Transform gazeDirectionSource;

        [Header("Debug")]
        [SerializeField] private bool logValues = true;
        [SerializeField] private int logEveryNFrames = 30;

        public Vector3 CurrentDirection { get; private set; }
        public float CurrentAzimuthRad { get; private set; }
        public float CurrentElevationRad { get; private set; }
        public Vector2 CurrentUV { get; private set; }

        public bool HasValidDirection { get; private set; }
        public bool IsUsingExternalDirection { get; private set; }

        private Vector3 externalDirection;
        private bool hasExternalDirection;

        private void Update()
        {
            if (!TryResolveDirection(out Vector3 dir, out bool usingExternalDirection))
            {
                HasValidDirection = false;
                return;
            }

            HasValidDirection = true;
            IsUsingExternalDirection = usingExternalDirection;
            CurrentDirection = dir;

            float azimuth = Mathf.Atan2(dir.x, dir.z);
            float elevation = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f));

            float u = (azimuth + Mathf.PI) / (2f * Mathf.PI);
            float v = 0.5f - (elevation / Mathf.PI);

            CurrentAzimuthRad = azimuth;
            CurrentElevationRad = elevation;
            CurrentUV = new Vector2(Mathf.Repeat(u, 1f), Mathf.Clamp01(v));

            if (logValues && Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0)
            {
                Debug.Log(
                    $"[SphericalMapper] source={(IsUsingExternalDirection ? "external" : "transform")} | " +
                    $"dir={CurrentDirection} | az={CurrentAzimuthRad:F3} rad | " +
                    $"el={CurrentElevationRad:F3} rad | uv=({CurrentUV.x:F3}, {CurrentUV.y:F3})"
                );
            }
        }

        private bool TryResolveDirection(out Vector3 direction, out bool usingExternalDirection)
        {
            direction = Vector3.zero;
            usingExternalDirection = false;

            switch (inputMode)
            {
                case GazeInputMode.ExternalDirectionOnly:
                    if (!hasExternalDirection || externalDirection.sqrMagnitude <= 0.000001f)
                    {
                        return false;
                    }

                    direction = externalDirection.normalized;
                    usingExternalDirection = true;
                    return true;

                case GazeInputMode.TransformForwardOnly:
                    if (gazeDirectionSource == null)
                    {
                        return false;
                    }

                    direction = gazeDirectionSource.forward.normalized;
                    return true;

                case GazeInputMode.Auto:
                default:
                    if (hasExternalDirection && externalDirection.sqrMagnitude > 0.000001f)
                    {
                        direction = externalDirection.normalized;
                        usingExternalDirection = true;
                        return true;
                    }

                    if (gazeDirectionSource == null)
                    {
                        return false;
                    }

                    direction = gazeDirectionSource.forward.normalized;
                    return true;
            }
        }

        public void SetGazeDirectionSource(Transform source)
        {
            gazeDirectionSource = source;
        }

        public void SetExternalGazeDirection(Vector3 direction, bool isValid = true)
        {
            hasExternalDirection = isValid && direction.sqrMagnitude > 0.000001f;
            externalDirection = hasExternalDirection ? direction.normalized : Vector3.zero;
        }

        public void ClearExternalGazeDirection()
        {
            hasExternalDirection = false;
            externalDirection = Vector3.zero;
            IsUsingExternalDirection = false;
        }
    }
}