using UnityEngine;
using AOI360.Runtime.Mapping;
using System.Collections.Generic;

namespace AOI360.Runtime.AOI
{
    public class AOILookup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SphericalMapper sphericalMapper;
        [SerializeField] private Texture2D aoiMapTexture;
        [SerializeField] private bool useGrayscaleIdEncoding = true;
        [SerializeField] private float neighborhoodRadiusDegrees = 1.5f;
        [SerializeField] private int neighborhoodSamples = 8;

        [Header("Debug")]
        [SerializeField] private bool logAOIChanges = true;
        [SerializeField] private bool logContinuous = false;
        [SerializeField] private int logEveryNFrames = 30;

        public int CurrentAOIId { get; private set; } = 0;
        public float CurrentAOIConfidence { get; private set; } = 0f;
        public Color CurrentAOIColor { get; private set; } = Color.clear;
        public Vector2 CurrentUV { get; private set; }
        public IReadOnlyList<int> NeighborAOIIds => neighborAOIIds;
        public Texture2D AOIMapTexture => aoiMapTexture;

        private int lastLoggedAOIId = -1;
        private readonly List<int> neighborAOIIds = new();
        private readonly HashSet<int> neighborAOISet = new();

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
            CurrentAOIColor = pixel;

            int aoiId = ResolveAOIIdFromColor(pixel);
            CurrentAOIId = aoiId;
            CurrentAOIConfidence = ComputeNeighborhoodConfidence(uv, aoiId);

            if (logAOIChanges && CurrentAOIId != lastLoggedAOIId)
            {
                Debug.Log(
                    $"[AOILookup] AOI cambiado -> id={CurrentAOIId} | conf={CurrentAOIConfidence:F2} " +
                    $"| uv=({uv.x:F3}, {uv.y:F3}) | px=({pixelX}, {pixelY})"
                );
                lastLoggedAOIId = CurrentAOIId;
            }

            if (logContinuous && Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0)
            {
                Debug.Log(
                    $"[AOILookup] id={CurrentAOIId} | conf={CurrentAOIConfidence:F2} " +
                    $"| uv=({uv.x:F3}, {uv.y:F3}) | px=({pixelX}, {pixelY})"
                );
            }
        }

        private int ResolveAOIIdFromColor(Color pixel)
        {
            bool looksGrayscale = Mathf.Abs(pixel.r - pixel.g) < 0.01f &&
                                  Mathf.Abs(pixel.g - pixel.b) < 0.01f;

            if (useGrayscaleIdEncoding && looksGrayscale)
            {
                int grayscaleId = Mathf.Clamp(Mathf.RoundToInt(pixel.r * 255f), 0, 255);
                if (grayscaleId > 0)
                {
                    return grayscaleId;
                }
            }

            float r = pixel.r;
            float g = pixel.g;
            float b = pixel.b;

            if (r < 0.1f && g < 0.1f && b < 0.1f)
            {
                return 0;
            }
            else if (r > 0.9f && g < 0.1f && b < 0.1f)
            {
                return 1;
            }
            else if (r < 0.1f && g > 0.9f && b < 0.1f)
            {
                return 2;
            }

            if (r > g && r > b)
            {
                return 1;
            }

            if (g > r && g > b)
            {
                return 2;
            }

            return 0;
        }

        private float ComputeNeighborhoodConfidence(Vector2 uv, int centerAoiId)
        {
            neighborAOIIds.Clear();
            neighborAOISet.Clear();

            if (centerAoiId <= 0 || neighborhoodSamples <= 0)
            {
                return 0f;
            }

            float angularRadiusRad = neighborhoodRadiusDegrees * Mathf.Deg2Rad;
            float deltaU = angularRadiusRad / (2f * Mathf.PI);
            float deltaV = angularRadiusRad / Mathf.PI;

            int total = neighborhoodSamples + 1;
            int matches = 0;

            for (int sampleIndex = 0; sampleIndex < total; sampleIndex++)
            {
                Vector2 sampleUv;

                if (sampleIndex == 0)
                {
                    sampleUv = uv;
                }
                else
                {
                    float t = (sampleIndex - 1) / (float)neighborhoodSamples;
                    float angle = t * Mathf.PI * 2f;
                    float offsetU = Mathf.Cos(angle) * deltaU;
                    float offsetV = Mathf.Sin(angle) * deltaV;
                    sampleUv = new Vector2(Mathf.Repeat(uv.x + offsetU, 1f), Mathf.Clamp01(uv.y + offsetV));
                }

                int sampleX = Mathf.Clamp(Mathf.FloorToInt(sampleUv.x * aoiMapTexture.width), 0, aoiMapTexture.width - 1);
                int sampleY = Mathf.Clamp(Mathf.FloorToInt(sampleUv.y * aoiMapTexture.height), 0, aoiMapTexture.height - 1);
                int sampleAoiId = ResolveAOIIdFromColor(aoiMapTexture.GetPixel(sampleX, sampleY));

                if (sampleAoiId == centerAoiId)
                {
                    matches++;
                }
                else if (sampleAoiId > 0 && neighborAOISet.Add(sampleAoiId))
                {
                    neighborAOIIds.Add(sampleAoiId);
                }
            }

            return matches / (float)total;
        }
    }
}
