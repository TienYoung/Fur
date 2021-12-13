using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GPUFur : ScriptableRendererFeature
{
    class GPUFurRenderPass : ScriptableRenderPass
    {
        public Material FurMat;
        public GraphicsBuffer IndexBuffer;
        public int VertexCount;
        public int InstanceCount;
        public MaterialPropertyBlock MatPropBlk;

        private FilteringSettings m_FilterSettings = new FilteringSettings(RenderQueueRange.all, LayerMask.GetMask("GPUFur"));
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

            cmd.DrawProcedural(IndexBuffer, Matrix4x4.identity, FurMat, 0, MeshTopology.Triangles, VertexCount, InstanceCount, MatPropBlk);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    GPUFurRenderPass m_GPUFurPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_GPUFurPass = new GPUFurRenderPass();

        // Configures where the render pass should be injected.
        m_GPUFurPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        var furObject = GameObject.Find("GPU Fur");
        SkinnedMeshRenderer skinnedMeshRenderer = furObject.GetComponent<SkinnedMeshRenderer>();
        GraphicsBuffer vertexBuffer = skinnedMeshRenderer.GetVertexBuffer();
        GraphicsBuffer indexBuffer = skinnedMeshRenderer.sharedMesh.GetIndexBuffer();
        int positionStreamIndex = skinnedMeshRenderer.sharedMesh.GetVertexAttributeStream(VertexAttribute.Position);
        int positionOffset = skinnedMeshRenderer.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Position);

        Material material = CoreUtils.CreateEngineMaterial("Fur/GPU Fur");
        material.SetBuffer("_Vertics", vertexBuffer);
        m_GPUFurPass.FurMat = material;

        int layerCount = material.GetInt("_LayerCount");
        float[] layerNums = new float[layerCount];
        for (int i = 0; i < layerCount; ++i)
        {
            layerNums[i] = i;
        }
        m_GPUFurPass.MatPropBlk.SetFloatArray("_LayerNums", layerNums);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_GPUFurPass);
    }
}


