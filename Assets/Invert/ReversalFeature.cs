using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Ŭ���� �� : ���̴� + ����
public class ReversalFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class ReversalSettings  // ���̴� ����
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material passMaterial = null;
    }

    public ReversalSettings settings = new ReversalSettings(); 

    class ReversalPass : ScriptableRenderPass
    {
        public Material passMaterial;
        string profilerTag;

        int ssdColorId;
        int ssdDepthId;
        /* RenderTargetIdentifier : CommandBuffer�� ���� RenderTexture�� �ĺ���
         * RenderTexture�� ���� ������� �ĺ����� 
         *
         * 
         */
        RenderTargetIdentifier colorTargetIdentifier;
        RenderTargetIdentifier cameraColorTexture;

        public ReversalPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ssdColorId = Shader.PropertyToID("_SSDColor");
            ssdDepthId = Shader.PropertyToID("_SSDDepth");

            var depthTextureDescriptor = cameraTextureDescriptor;
            depthTextureDescriptor.colorFormat = RenderTextureFormat.Depth;

            cmd.GetTemporaryRT(ssdDepthId, depthTextureDescriptor, FilterMode.Point);
            cmd.GetTemporaryRT(ssdColorId, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            colorTargetIdentifier = new RenderTargetIdentifier(ssdColorId);

            ConfigureTarget(colorTargetIdentifier);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            ///////////////////////////// 

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.Blit(cameraColorTexture, colorTargetIdentifier);

            cmd.Blit(colorTargetIdentifier, cameraColorTexture, passMaterial);

            context.ExecuteCommandBuffer(cmd);
            /////////////////////////////
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {

        }
    }

    private ReversalPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new ReversalPass("Final");
        scriptablePass.passMaterial = settings.passMaterial;
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(scriptablePass);
    }
}