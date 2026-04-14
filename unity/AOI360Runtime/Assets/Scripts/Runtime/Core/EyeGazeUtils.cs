using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace EyeGaze.Runtime.Core
{
    // Common helper functions shared by the eye gaze modules.
    public static class EyeGazeUtils
    {
        // Resolve the output directory using either the custom path or Unity's persistent data path
        public static string GetOutputDirectory(bool useCustomOutputDirectory, string customOutputDirectory)
        {
            if (useCustomOutputDirectory && !string.IsNullOrWhiteSpace(customOutputDirectory))
            {
                return customOutputDirectory;
            }

            return Path.Combine(Application.persistentDataPath, "EyeGazeLogs");
        }

        // Build the output file name according to the current settings
        public static string GetOutputFileName(string outputFileName, string defaultBaseName, bool generateTimestampedFileName)
        {
            string safeBaseName = string.IsNullOrWhiteSpace(outputFileName)
                ? defaultBaseName
                : outputFileName.Trim();

            if (generateTimestampedFileName)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                return $"{safeBaseName}_{timestamp}.txt";
            }

            return $"{safeBaseName}.txt";
        }

        // Configure a LineRenderer with the basic settings needed by the debug module
        public static void ConfigureLineRenderer(LineRenderer lineRenderer, Color color, bool enabled)
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.positionCount = 2;
            lineRenderer.enabled = enabled;

            if (lineRenderer.material != null && lineRenderer.material.HasProperty("_Color"))
            {
                lineRenderer.material.color = color;
            }
        }

        // Returns the Renderer attached to the hit object if available
        public static Renderer GetRendererFromGameObject(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            return target.GetComponent<Renderer>();
        }

        // Returns whether a renderer can be color-highlighted safely
        public static bool CanHighlightRenderer(Renderer renderer)
        {
            return renderer != null &&
                   renderer.material != null &&
                   renderer.material.HasProperty("_Color");
        }
    }
}