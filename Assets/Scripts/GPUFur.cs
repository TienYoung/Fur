using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GPUFur : ScriptableRendererFeature
{
    class GPUFurRenderPass : ScriptableRenderPass
    {
        public Material FurMat;
        public Matrix4x4 Matrix;
        public SkinnedMeshRenderer Renderer;
        public GraphicsBuffer VertexBuffer;
        public GraphicsBuffer IndexBuffer;
        private int m_IndexCount;
        public int InstanceCount;
        public MaterialPropertyBlock MatPropBlk = new MaterialPropertyBlock();

        private FilteringSettings m_FilterSettings = new FilteringSettings(RenderQueueRange.all, LayerMask.GetMask("GPUFur"));
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            VertexBuffer = Renderer.GetVertexBuffer();
            IndexBuffer = Renderer.sharedMesh.GetIndexBuffer();
            m_IndexCount = IndexBuffer.count;
            Matrix = Renderer.transform.localToWorldMatrix;
            FurMat.SetBuffer("_Vertics", VertexBuffer);
            FurMat.enableInstancing = true;

            int layerCount = Mathf.FloorToInt(FurMat.GetFloat("_LayerCount"));
            float[] layerNums = new float[layerCount];
            for (int i = 0; i < layerCount; ++i)
            {
                layerNums[i] = i;
            }
            MatPropBlk.SetFloatArray("_LayerNums", layerNums);
            InstanceCount = layerCount;
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


            cmd.DrawProcedural(IndexBuffer, Matrix, FurMat, 1, MeshTopology.Triangles, m_IndexCount, InstanceCount, MatPropBlk);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
            //FurMat.enableInstancing = false;
        }
    }

    GPUFurRenderPass m_GPUFurPass;
    //GraphicsBuffer m_VertexBuffer;
    //GraphicsBuffer m_IndexBuffer;

    /// <inheritdoc/>
    public override void Create()
    {
        var furObject = GameObject.Find("cat");
        if (furObject == null)
        {
            Debug.LogWarning("Not found Fur !!!!!!!!!!");
            return;
        }

        SkinnedMeshRenderer skinnedMeshRenderer = furObject.GetComponentInChildren<SkinnedMeshRenderer>();
        //m_VertexBuffer = skinnedMeshRenderer.GetVertexBuffer();
        //skinnedMeshRenderer.vertexBufferTarget = GraphicsBuffer.Target.Structured;
        //skinnedMeshRenderer.sharedMesh.indexBufferTarget = GraphicsBuffer.Target.Raw;
        //m_IndexBuffer = skinnedMeshRenderer.sharedMesh.GetIndexBuffer();
        //int stride = skinnedMeshRenderer.sharedMesh.GetVertexBufferStride(0);
        //int positionOffset = skinnedMeshRenderer.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Position);
        //int normalOffset = skinnedMeshRenderer.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Normal);
        //int tangentOffset = skinnedMeshRenderer.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Tangent);

        Material material = skinnedMeshRenderer.sharedMaterial;
        //material.SetBuffer("_Vertics", m_VertexBuffer);

        //material.SetInteger("_Stride", stride);
        //material.SetInteger("_PosOffset", positionOffset);
        //material.SetInteger("_NorOffset", normalOffset);
        //material.SetInteger("_TanOffset", tangentOffset);

        m_GPUFurPass = new GPUFurRenderPass();

        // Configures where the render pass should be injected.
        m_GPUFurPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        m_GPUFurPass.FurMat = material;



        //m_GPUFurPass.IndexBuffer = m_IndexBuffer;
        m_GPUFurPass.Renderer = skinnedMeshRenderer;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_GPUFurPass);
    }
}


