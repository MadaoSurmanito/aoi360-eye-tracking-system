using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AOI360.Runtime.AOI;
using AOI360.Runtime.Mapping;
using AOI360.Runtime.Video;
using UnityEngine;

namespace AOI360.Runtime.Logging
{
    public class DataRecorder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VideoPlayback videoPlayback;
        [SerializeField] private SphericalMapper sphericalMapper;
        [SerializeField] private AOILookup aoiLookup;

        [Header("Recording")]
        [SerializeField] private bool recordOnStart = true;
        [SerializeField] private bool autoExportOnDisable = true;
        [SerializeField] private string participantId = "P001";
        [SerializeField] private string sessionId = "S001";
        [SerializeField] private string videoId = "sample360";
        [SerializeField] private string outputFileName = "phase0_gaze_log.csv";

        [Header("Debug")]
        [SerializeField] private bool logRecordingState = true;
        [SerializeField] private bool logEveryNFrames = false;
        [SerializeField] private int frameLogInterval = 60;

        private readonly List<string> rows = new();
        private bool isRecording = false;
        private float sessionStartTime;

        private void Start()
        {
            rows.Clear();
            rows.Add(BuildHeader());

            if (recordOnStart)
            {
                StartRecording();
            }
        }

        private void Update()
        {
            if (!isRecording || sphericalMapper == null || aoiLookup == null)
            {
                return;
            }

            long frameIndex = videoPlayback != null ? videoPlayback.CurrentFrame : -1;
            double videoTime = videoPlayback != null ? videoPlayback.CurrentTime : 0d;

            Vector3 dir = sphericalMapper.CurrentDirection;
            Vector2 uv = sphericalMapper.CurrentUV;
            float az = sphericalMapper.CurrentAzimuthRad;
            float el = sphericalMapper.CurrentElevationRad;
            int aoiId = aoiLookup.CurrentAOIId;

            float timestampMs = (Time.time - sessionStartTime) * 1000f;

            string row = string.Join(",",
                Escape(participantId),
                Escape(sessionId),
                Escape(videoId),
                timestampMs.ToString("F3", CultureInfo.InvariantCulture),
                frameIndex.ToString(CultureInfo.InvariantCulture),
                videoTime.ToString("F6", CultureInfo.InvariantCulture),
                dir.x.ToString("F6", CultureInfo.InvariantCulture),
                dir.y.ToString("F6", CultureInfo.InvariantCulture),
                dir.z.ToString("F6", CultureInfo.InvariantCulture),
                az.ToString("F6", CultureInfo.InvariantCulture),
                el.ToString("F6", CultureInfo.InvariantCulture),
                uv.x.ToString("F6", CultureInfo.InvariantCulture),
                uv.y.ToString("F6", CultureInfo.InvariantCulture),
                aoiId.ToString(CultureInfo.InvariantCulture)
            );

            rows.Add(row);

            if (logEveryNFrames && Time.frameCount % Mathf.Max(1, frameLogInterval) == 0)
            {
                Debug.Log($"[DataRecorder] frame={frameIndex} | videoTime={videoTime:F3} | uv=({uv.x:F3}, {uv.y:F3}) | aoi={aoiId}");
            }
        }

        private void OnDisable()
        {
            if (autoExportOnDisable)
            {
                ExportCsv();
            }
        }

        public void StartRecording()
        {
            sessionStartTime = Time.time;
            isRecording = true;

            if (logRecordingState)
            {
                Debug.Log("[DataRecorder] Recording started.");
            }
        }

        public void StopRecording()
        {
            isRecording = false;

            if (logRecordingState)
            {
                Debug.Log("[DataRecorder] Recording stopped.");
            }
        }

        public void ExportCsv()
        {
            if (rows.Count <= 1)
            {
                Debug.LogWarning("[DataRecorder] No data to export.");
                return;
            }

            string folderPath = Path.Combine(Application.persistentDataPath, "Exports");
            Directory.CreateDirectory(folderPath);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{Path.GetFileNameWithoutExtension(outputFileName)}_{timestamp}.csv";
            string filePath = Path.Combine(folderPath, fileName);

            File.WriteAllText(filePath, BuildCsvContent(), Encoding.UTF8);

            Debug.Log($"[DataRecorder] CSV exported to: {filePath}");
        }

        private string BuildHeader()
        {
            return "participant_id,session_id,video_id,timestamp_ms,frame_index,video_time,direction_x,direction_y,direction_z,azimuth_rad,elevation_rad,uv_x,uv_y,aoi_id";
        }

        private string BuildCsvContent()
        {
            StringBuilder sb = new();

            for (int i = 0; i < rows.Count; i++)
            {
                sb.AppendLine(rows[i]);
            }

            return sb.ToString();
        }

        private string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}