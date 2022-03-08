using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kriko.RenderPipeline
{
    [CreateAssetMenu(menuName = "Rendering/CustomRenderPipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipe(this);
        }
    }
}
