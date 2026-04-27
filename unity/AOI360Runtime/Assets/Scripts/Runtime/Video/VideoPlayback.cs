using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace AOI360.Runtime.Video
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayback : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField] private string videoFileName = "sample360.mp4";
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;

        [Header("Output")]
        [SerializeField] private RenderTexture targetTexture;
        [SerializeField] private Material skyboxMaterial;

        [Header("Debug")]
        [SerializeField] private bool logVideoEvents = true;

        private VideoPlayer videoPlayer;
        private bool isPrepared = false;

        public bool IsPrepared => isPrepared;
        public string VideoFileName => videoFileName;
        public long CurrentFrame => videoPlayer != null ? videoPlayer.frame : -1;
        public double CurrentTime => videoPlayer != null ? videoPlayer.time : 0d;
        public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();

            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            // Configuración base del reproductor de vídeo
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loop;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = targetTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            // Importante para fases posteriores: sincronización por frame mostrado
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.skipOnDrop = false;
            videoPlayer.waitForFirstFrame = true;

            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.errorReceived += HandleErrorReceived;
            videoPlayer.loopPointReached += HandleLoopPointReached;

            // Asignamos la textura al material del skybox
            if (skyboxMaterial != null && targetTexture != null)
            {
                skyboxMaterial.SetTexture("_MainTex", targetTexture);
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }

        private IEnumerator Start()
        {
            string videoPath = Path.Combine(Application.streamingAssetsPath, "Videos", videoFileName);

            if (!File.Exists(videoPath))
            {
                Debug.LogError($"[VideoPlayback] No se encontró el vídeo en: {videoPath}");
                yield break;
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoPath;

            if (logVideoEvents)
            {
                Debug.Log($"[VideoPlayback] Preparando vídeo desde: {videoPath}");
            }

            videoPlayer.Prepare();

            // Espera simple hasta que el vídeo quede preparado
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            if (playOnStart)
            {
                videoPlayer.Play();

                if (logVideoEvents)
                {
                    Debug.Log("[VideoPlayback] Reproducción iniciada.");
                }
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.errorReceived -= HandleErrorReceived;
            videoPlayer.loopPointReached -= HandleLoopPointReached;
        }

        private void HandlePrepareCompleted(VideoPlayer source)
        {
            isPrepared = true;

            if (logVideoEvents)
            {
                Debug.Log("[VideoPlayback] Vídeo preparado correctamente.");
            }
        }

        private void HandleErrorReceived(VideoPlayer source, string message)
        {
            Debug.LogError($"[VideoPlayback] Error reproduciendo vídeo: {message}");
        }

        private void HandleLoopPointReached(VideoPlayer source)
        {
            if (logVideoEvents)
            {
                Debug.Log("[VideoPlayback] El vídeo ha llegado al final y continuará en loop.");
            }
        }

        public void PlayVideo()
        {
            if (videoPlayer != null && isPrepared)
            {
                videoPlayer.Play();
            }
        }

        public void PauseVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }
        }

        public void StopVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }

        public void SetLoop(bool value)
        {
            loop = value;

            if (videoPlayer != null)
            {
                videoPlayer.isLooping = loop;
            }
        }
    }
}
