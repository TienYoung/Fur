using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GPUFur : ScriptableRendererFeature
{
    class GPUFurRenderPass : ScriptableRenderPass
    {
        public SkinnedMeshRenderer SMRenderer;

        private Material m_FurMat;
        private Matrix4x4 m_Matrix;
        private GraphicsBuffer m_IndexBuffer;
        private GraphicsBuffer m_DeformedDataBuffer;
        private GraphicsBuffer m_StaticDataBuffer;
        private GraphicsBuffer m_SkinningDataBuffer;
        private int m_IndexCount;
        private int m_InstanceCount;

        bool setted = false;
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_DeformedDataBuffer = SMRenderer.GetVertexBuffer();
            SMRenderer.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            int _uvStreamID = SMRenderer.sharedMesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);
            m_StaticDataBuffer = SMRenderer.sharedMesh.GetVertexBuffer(_uvStreamID);
            //m_SkinningDataBuffer = Renderer.sharedMesh.GetVertexBuffer(Renderer.sharedMesh.GetVertexAttributeStream(VertexAttribute.BlendWeight));
            
            m_IndexBuffer = SMRenderer.sharedMesh.GetIndexBuffer();
            m_IndexCount = m_IndexBuffer.count;


            m_FurMat = SMRenderer.sharedMaterial;
            m_FurMat.SetBuffer("_DeformedData", m_DeformedDataBuffer);
            m_FurMat.SetBuffer("_StaticData", m_StaticDataBuffer);
            

            m_Matrix = SMRenderer.transform.parent.Find("ch_bone").Find("Bip001").Find("Bip001 Pelvis").localToWorldMatrix;
            m_InstanceCount = Mathf.FloorToInt(m_FurMat.GetFloat("_LayerCount"));

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

            cmd.DrawProcedural(m_IndexBuffer, m_Matrix, m_FurMat, 1, MeshTopology.Triangles, m_IndexCount, m_InstanceCount);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            m_IndexBuffer?.Dispose();
            m_DeformedDataBuffer?.Dispose();
            m_StaticDataBuffer?.Dispose();
            //m_SkinningDataBuffer?.Dispose();
        }
    }

    GPUFurRenderPass m_GPUFurPass;


    /// <inheritdoc/>
    public override void Create()
    {
        var furObject = GameObject.Find("Wolf_Generic");
        if (furObject == null)
        {
            Debug.LogWarning("Not found Fur !!!!!!!!!!");
            return;
        }
        SkinnedMeshRenderer skinnedMeshRenderer = furObject.GetComponentInChildren<SkinnedMeshRenderer>();

        m_GPUFurPass = new GPUFurRenderPass();

        // Configures where the render pass should be injected.
        m_GPUFurPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        //m_GPUFurPass.IndexBuffer = m_IndexBuffer;
        m_GPUFurPass.SMRenderer = skinnedMeshRenderer;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_GPUFurPass);
    }
}


