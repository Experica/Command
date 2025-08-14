using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Experica;

namespace Experica.NetEnv
{
    public class CopyMaskCustomPass : CustomPass
    {
        [Header("Copy Settings")]
        public bool EnableCopy = false;
        public float CopyScale = 0.5f; // scaling factor
        public float CopyRate = 30f; // Hz
        float _lastCopyTime;

        RenderTexture _streamRT;
        RTHandle _copyRT;
        FrameStreamer _frameStreamer;

        [Header("Mask Settings")]
        public bool EnableMask = false;
        public Rect MaskRegion = new(0.2f, 0.35f, 0.3f, 0.3f);
        public Color MaskColor = Color.gray;
        Material _maskMaterial;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _frameStreamer = GameObject.FindAnyObjectByType<FrameStreamer>();
            if (_frameStreamer == null) { Debug.LogError("Can not find FrameStreamer in the App, CopyMaskCustomPass won't stream copied frame."); }

            // Initialize mask material
            _maskMaterial = CoreUtils.CreateEngineMaterial("FullScreen/MaskRegion");
        }

        void SetupStreamTexture(CustomPassContext ctx)
        {
            // Get actual render buffer size
            int srcWidth = ctx.cameraColorBuffer.rt.width;
            int srcHeight = ctx.cameraColorBuffer.rt.height;

            int targetWidth = Mathf.Max(1, Mathf.RoundToInt(srcWidth * CopyScale));
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt(srcHeight * CopyScale));

            // If no texture or size changed, recreate
            if (_streamRT == null ||
                _streamRT.width != targetWidth ||
                _streamRT.height != targetHeight)
            {
                if (_streamRT != null)
                {
                    _streamRT.Release();
                    Object.Destroy(_streamRT);
                    _streamRT = null;
                }

                _streamRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.BGRA32)
                {
                    enableRandomWrite = false,
                    useMipMap = false,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    antiAliasing = 1,
                    name = "CopyMask_StreamRT"
                };
                _streamRT.Create();

                // Wrap in RTHandle for HDRP's BlitCameraTexture
                _copyRT?.Release();
                _copyRT = RTHandles.Alloc(_streamRT);

                // Register with FrameStreamer if present
                _frameStreamer?.RegisterStreamTexture(_streamRT);
            }
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (EnableCopy && Time.time - _lastCopyTime >= 1f / CopyRate)
            {
                SetupStreamTexture(ctx);
                HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, _copyRT);
                _lastCopyTime = Time.time;
            }

            if (EnableMask)
            {
                _maskMaterial.SetVector("_Region", new Vector4(
                    MaskRegion.x, MaskRegion.y,
                    MaskRegion.width, MaskRegion.height
                ));
                _maskMaterial.SetColor("_MaskColor", MaskColor);
                ctx.cmd.Blit(ctx.cameraColorBuffer, ctx.cameraColorBuffer, _maskMaterial);
            }
        }

        protected override void Cleanup()
        {
            _copyRT?.Release();
            _copyRT = null;

            if (_streamRT != null)
            {
                _streamRT.Release();
                Object.Destroy(_streamRT);
                _streamRT = null;
            }

            CoreUtils.Destroy(_maskMaterial);
        }
    }
}