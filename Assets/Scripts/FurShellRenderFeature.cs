using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FurShellRenderFeature : ScriptableRendererFeature
{
    public Material FurShellMaterial;
    [Range(0, 64)]
    public int LayerNumber;

    class FurShellRenderPass : ScriptableRenderPass
    {
        //private List<ShaderTagId> m_ShaderTagId = new List<ShaderTagId>(new ShaderTagId[] {new ShaderTagId("Fur1"), new ShaderTagId("Fur2") });
        private ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalForward");

        private FilteringSettings m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, LayerMask.GetMask("Fur"));

        public Material FurShellMaterial;
        public int LayerNumber;

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;


            for (int i = 0; i < LayerNumber; i++)
            {
                Material furMat = new Material(FurShellMaterial.shader);
                furMat.CopyPropertiesFromMaterial(FurShellMaterial);
                furMat.SetInt("_LayerNumber", LayerNumber);
                furMat.SetInt("_Layer", i);
                Light mainlight = Light.GetLights(LightType.Directional, 0)[renderingData.lightData.mainLightIndex];

                furMat.SetVector("_LightDir", -mainlight.transform.forward);

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortingCriteria);
                drawingSettings.overrideMaterial = furMat;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    FurShellRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new FurShellRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        m_ScriptablePass.FurShellMaterial = FurShellMaterial;
        m_ScriptablePass.LayerNumber = LayerNumber;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


