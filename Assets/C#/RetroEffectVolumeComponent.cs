using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenuForRenderPipeline("Custom/Retro Effect", typeof(UniversalRenderPipeline))]
public class RetroEffectVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);

    public BoolParameter scaleResolution = new BoolParameter(false);
    public ClampedIntParameter targetResolutionScale = new ClampedIntParameter(3, 1, 20);

    public BoolParameter changeColorDepth = new BoolParameter(false);
    public ClampedIntParameter targetColorDepth = new ClampedIntParameter(5, 1, 8);

    public BoolParameter dithering = new BoolParameter(false);

    public BoolParameter enableRecolor = new BoolParameter(false);
    public TextureParameter toGradient = new TextureParameter(null);

    public BoolParameter flipY = new BoolParameter(false);

    public bool IsActive()
    {
        return enable.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}