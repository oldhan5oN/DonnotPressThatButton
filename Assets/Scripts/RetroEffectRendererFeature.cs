using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RetroEffectRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material retroMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    private RetroEffectRenderPass retroEffectPass;

    public override void Create()
    {
        retroEffectPass = new RetroEffectRenderPass(settings.retroMaterial);
        retroEffectPass.renderPassEvent = settings.renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        retroEffectPass?.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.retroMaterial == null)
        {
            Debug.LogWarning("[RetroEffect] Material is null");
            return;
        }

        var stack = VolumeManager.instance.stack;
        var retroEffect = stack.GetComponent<RetroEffectVolumeComponent>();

        if (retroEffect == null)
        {
            Debug.LogWarning("[RetroEffect] VolumeComponent not found in stack");
            return;
        }

        if (!retroEffect.IsActive())
        {
            Debug.LogWarning("[RetroEffect] VolumeComponent is not active");
            return;
        }

        retroEffectPass.SetMaterial(settings.retroMaterial);
        renderer.EnqueuePass(retroEffectPass);
    }

    private class RetroEffectRenderPass : ScriptableRenderPass
    {
        private Material retroMaterial;
        private RTHandle source;
        private RTHandle tempRT;

        public RetroEffectRenderPass(Material material)
        {
            retroMaterial = material;
        }

        public void SetMaterial(Material material)
        {
            retroMaterial = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Debug.Log("[RetroEffect] Execute called");
            
            if (retroMaterial == null)
            {
                Debug.LogWarning("[RetroEffect] Material is null in Execute");
                return;
            }

            var stack = VolumeManager.instance.stack;
            var retroEffect = stack.GetComponent<RetroEffectVolumeComponent>();

            if (retroEffect == null)
            {
                return;
            }

            retroMaterial.SetFloat("_ScaleResolution", retroEffect.scaleResolution.value ? 1.0f : 0.0f);
            retroMaterial.SetInt("_TargetResolutionScale", retroEffect.targetResolutionScale.value);

            retroMaterial.SetFloat("_ChangeColorDepth", retroEffect.changeColorDepth.value ? 1.0f : 0.0f);
            retroMaterial.SetInt("_TargetColorDepth", retroEffect.targetColorDepth.value);

            retroMaterial.SetFloat("_Dithering", retroEffect.dithering.value ? 1.0f : 0.0f);

            retroMaterial.SetFloat("_EnableRecolor", retroEffect.enableRecolor.value ? 1.0f : 0.0f);

            if (retroEffect.toGradient.value != null)
            {
                retroMaterial.SetTexture("_ToGradient", retroEffect.toGradient.value);
            }

            retroMaterial.SetFloat("_FlipY", retroEffect.flipY.value ? 1.0f : 0.0f);

            CommandBuffer cmd = CommandBufferPool.Get("Retro Effect");

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            if (tempRT == null || tempRT.rt == null || 
                tempRT.rt.width != descriptor.width || 
                tempRT.rt.height != descriptor.height)
            {
                tempRT?.Release();
                tempRT = RTHandles.Alloc(descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_RetroEffectTempRT");
            }

            Blit(cmd, source, tempRT, retroMaterial, 0);
            Blit(cmd, tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempRT?.Release();
            tempRT = null;
        }
    }
}
