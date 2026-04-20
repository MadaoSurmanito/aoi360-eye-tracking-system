using UnityEngine;
using AOI360.Runtime.Mapping;

namespace AOI360.Runtime.AOI
{
    public class AOILookup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SphericalMapper sphericalMapper;
        [SerializeField] private Texture2D aoiMapTexture;

        [Header("Debug")]
        [SerializeField] private bool logAOIChanges = true;
        [SerializeField] private bool logContinuous = false;
        [SerializeField] private int logEveryNFrames = 30;

        public int CurrentAOIId { get; private set; } = 0;
        public Vector2 CurrentUV { get; private set; }

        private int lastLoggedAOIId = -1;

        private void Update()
        {
            if (sphericalMapper == null || aoiMapTexture == null)
            {
                return;
            }

            Vector2 uv = sphericalMapper.CurrentUV;
            CurrentUV = uv;

            int pixelX = Mathf.Clamp(Mathf.FloorToInt(uv.x * aoiMapTexture.width), 0, aoiMapTexture.width - 1);
            int pixelY = Mathf.Clamp(Mathf.FloorToInt(uv.y * aoiMapTexture.height), 0, aoiMapTexture.height - 1);

            Color pixel = aoiMapTexture.GetPixel(pixelX, pixelY);

            // Reglas manuales para esta fase de prueba:
            // negro = 0
            // rojo = 1
            // verde = 2
            int aoiId = ResolveAOIIdFromColor(pixel);

            CurrentAOIId = aoiId;

            if (logAOIChanges && CurrentAOIId != lastLoggedAOIId)
            {
                Debug.Log($"[AOILookup] AOI cambiado -> id={CurrentAOIId} | uv=({uv.x:F3}, {uv.y:F3}) | px=({pixelX}, {pixelY})");
                lastLoggedAOIId = CurrentAOIId;
            }

            if (logContinuous && Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0)
            {
                Debug.Log($"[AOILookup] id={CurrentAOIId} | uv=({uv.x:F3}, {uv.y:F3}) | px=({pixelX}, {pixelY})");
            }
        }

        private int ResolveAOIIdFromColor(Color pixel)
        {
            float r = pixel.r;
            float g = pixel.g;
            float b = pixel.b;

            // Fondo negro
            if (r < 0.1f && g < 0.1f && b < 0.1f)
            {
                return 0;
            }

            // AOI 1 = rojo
            if (r > 0.8f && g < 0.2f && b < 0.2f)
            {
                return 1;
            }

            // AOI 2 = verde
            if (g > 0.8f && r < 0.2f && b < 0.2f)
            {
                return 2;
            }

            return 0;
        }
    }
}