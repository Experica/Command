using System;
using Unity.RenderStreaming;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Rendering;

namespace Experica
{
    public class FrameStreamer : MonoBehaviour
    {
        VideoStreamSender videoSender;

        void Awake()
        {
            videoSender = GetComponent<VideoStreamSender>();
        }

        public void RegisterStreamTexture(RenderTexture streamRT)
        {
            if (streamRT == null)
            {
                Debug.LogError("FrameStreamer: RegisterStreamTexture called with null RenderTexture.");
                return;
            }

            videoSender.sourceTexture = streamRT;

            // Keep VideoStreamSender's resolution in sync with streamRT
            var W = (uint)streamRT.width;
            var H = (uint)streamRT.height;

            if (videoSender.width != W || videoSender.height != H)
            {
                videoSender.width = W;
                videoSender.height = H;
            }
        }

        void Update()
        {
            // Required by Unity WebRTC plugin to pump frames and callbacks.
            WebRTC.Update();
        }
    }
}
