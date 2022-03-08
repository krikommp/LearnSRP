using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kriko.RenderPipeline
{
    public class CameraRenderer
    {
        private readonly static string BufferName = "Render Camera";
        private readonly static ShaderTagId unlitShaderTagId = new ShaderTagId("gbuffer");

        private Camera m_camera;
        private ScriptableRenderContext m_context;
        private CommandBuffer m_buffer = new CommandBuffer {name = BufferName};
        private CullingResults m_cullingResults;
        
        // 延迟渲染
        private RenderTexture m_gDepth;
        private RenderTexture[] m_gBuffers = new RenderTexture[4];
        private RenderTargetIdentifier[] m_gBufferIDs = new RenderTargetIdentifier[4];

        public CameraRenderer()
        {
            PreRenderData();
        }

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            this.m_camera = camera;
            this.m_context = context;
            if (!Culling()) return;
            
            m_buffer.SetRenderTarget(m_gBufferIDs, m_gDepth);

            OnCameraRenderBegin();

            DrawGeometryPass();
            SetGlobalUniform();
            DrawLightPass();
            DrawSkyAndOther();

            OnCameraRenderEnd();
        }

        private void PreRenderData()
        {
            m_gDepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth,
                RenderTextureReadWrite.Linear);
            // albedo
            m_gBuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            // normal
            m_gBuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010,
                RenderTextureReadWrite.Linear);
            // motion vec & rough & metallic
            m_gBuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64,
                RenderTextureReadWrite.Linear);
            // emission & occlusion
            m_gBuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            
            // 为纹理id赋值
            for (int i = 0; i < 4; ++i)
            {
                m_gBufferIDs[i] = m_gBuffers[i];
            }
        }

        private void DrawGeometryPass()
        {
            var sortingSettings = new SortingSettings(m_camera) { criteria = SortingCriteria.CommonOpaque};
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            m_context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings); 
        }

        private void SetGlobalUniform()
        {
            m_buffer.SetGlobalTexture("_gdepth", m_gDepth);
            for (int i = 0; i < 4; ++i)
            {
                m_buffer.SetGlobalTexture("_GT" + i, m_gBuffers[i]);
            }
            
            Matrix4x4 viewMatrix = m_camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(m_camera.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            m_buffer.SetGlobalMatrix("_vpMatrix", vpMatrix);
            m_buffer.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
        }

        private void DrawLightPass()
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "LightPass";

            Material mat = new Material(Shader.Find("ToyRP/LightPass"));
            cmd.Blit(m_gBufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
            m_context.ExecuteCommandBuffer(cmd);
        }

        private void DrawSkyAndOther()
        {
            m_context.DrawSkybox(m_camera);
            if (Handles.ShouldRenderGizmos())
            {
                m_context.DrawGizmos(m_camera, GizmoSubset.PreImageEffects);
                m_context.DrawGizmos(m_camera, GizmoSubset.PostImageEffects);
            }
        }

        private void OnCameraRenderBegin()
        {
            m_context.SetupCameraProperties(m_camera);
            m_buffer.ClearRenderTarget(true, true, Color.gray);
            m_buffer.BeginSample(BufferName);
            ExecuteBuffer();
        }

        private void OnCameraRenderEnd()
        {
            m_buffer.EndSample(BufferName);
            ExecuteBuffer();
            m_context.Submit();
        }

        private void ExecuteBuffer()
        {
            m_context.ExecuteCommandBuffer(m_buffer);
            m_buffer.Clear();
        }

        private bool Culling()
        {
            if (m_camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                m_cullingResults = m_context.Cull(ref scriptableCullingParameters);
                return true;
            }

            return false;
        }
    }
}
