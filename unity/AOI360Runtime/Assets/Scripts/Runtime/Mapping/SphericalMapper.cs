using UnityEngine;

namespace AOI360.Runtime.Mapping
{
    public class SphericalMapper : MonoBehaviour
    {
        [Header("Gaze source")]
        [SerializeField] private Transform gazeDirectionSource;

        [Header("Debug")]
        [SerializeField] private bool logValues = true;
        [SerializeField] private int logEveryNFrames = 30;

        public Vector3 CurrentDirection { get; private set; }
        public float CurrentAzimuthRad { get; private set; }
        public float CurrentElevationRad { get; private set; }
        public Vector2 CurrentUV { get; private set; }

        private void Update()
        {
            if (gazeDirectionSource == null)
            {
                return;
            }

            // En esta fase usamos la forward de la cámara como dirección de mirada temporal
            Vector3 dir = gazeDirectionSource.forward.normalized;

            CurrentDirection = dir;

            // Azimut: ángulo horizontal respecto al eje Z
            float azimuth = Mathf.Atan2(dir.x, dir.z);

            // Elevación: ángulo vertical
            float elevation = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f));

            // Conversión a UV equirectangular
            float u = (azimuth + Mathf.PI) / (2f * Mathf.PI);
            float v = 0.5f - (elevation / Mathf.PI);

            CurrentAzimuthRad = azimuth;
            CurrentElevationRad = elevation;
            CurrentUV = new Vector2(Mathf.Repeat(u, 1f), Mathf.Clamp01(v));

            if (logValues && Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0)
            {
                Debug.Log(
                    $"[SphericalMapper] dir={CurrentDirection} | az={CurrentAzimuthRad:F3} rad | " +
                    $"el={CurrentElevationRad:F3} rad | uv=({CurrentUV.x:F3}, {CurrentUV.y:F3})"
                );
            }
        }

        public void SetGazeDirectionSource(Transform source)
        {
            gazeDirectionSource = source;
        }
    }
}