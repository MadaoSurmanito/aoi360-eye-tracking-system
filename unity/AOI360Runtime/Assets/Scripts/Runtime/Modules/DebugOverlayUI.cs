using AOI360.Runtime.AOI;
using AOI360.Runtime.Mapping;
using AOI360.Runtime.Video;
using EyeGaze.Runtime.Modules;
using TMPro;
using UnityEngine;

namespace AOI360.Runtime.Modules
{
    public class DebugOverlayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VideoPlayback videoPlayback;
        [SerializeField] private SphericalMapper sphericalMapper;
        [SerializeField] private AOILookup aoiLookup;
        [SerializeField] private EyeGazeDebugVisualizer debugVisualizer;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI debugText;

        private void Update()
        {
            if (debugText == null || sphericalMapper == null || aoiLookup == null)
            {
                return;
            }

            if (debugVisualizer == null)
            {
                debugVisualizer = FindFirstObjectByType<EyeGazeDebugVisualizer>();
            }

            long frameIndex = videoPlayback != null ? videoPlayback.CurrentFrame : -1;
            double videoTime = videoPlayback != null ? videoPlayback.CurrentTime : 0d;

            Vector2 uv = sphericalMapper.CurrentUV;
            int aoiId = aoiLookup.CurrentAOIId;
            float confidence = aoiLookup.CurrentAOIConfidence;
            int fixationSteps = debugVisualizer != null ? debugVisualizer.ActiveFixationCommitCount : 0;

            debugText.text =
                $"Frame: {frameIndex}\n" +
                $"Video Time: {videoTime:F3}\n" +
                $"UV: ({uv.x:F3}, {uv.y:F3})\n" +
                $"AOI ID: {aoiId}\n" +
                $"AOI Conf: {confidence:F2}\n" +
                $"Fixation Steps: {fixationSteps}";
        }
    }
}
