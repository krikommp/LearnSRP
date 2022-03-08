using UnityEngine;
using UnityEngine.Rendering;

namespace Kriko.RenderPipeline
{
    public class CustomRenderPipe : UnityEngine.Rendering.RenderPipeline
    {
        private CustomRenderPipelineAsset m_pipeAsset;
        private CameraRenderer m_cameraRenderer;

        public CustomRenderPipe(CustomRenderPipelineAsset InAsset)
        {
            m_pipeAsset = InAsset;
            m_cameraRenderer = new CameraRenderer();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                m_cameraRenderer.Render(context, camera);
            }
        }
    }
}
