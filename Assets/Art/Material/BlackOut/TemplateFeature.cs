using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TemplateFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class TemplateSettings  
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material passMaterial = null;
    }

    public TemplateSettings settings = new TemplateSettings(); 

    class TemplatePass : ScriptableRenderPass
    {
        public Material passMaterial;
        string profilerTag;

        int ssdColorId;
        int ssdDepthId;

        RenderTargetIdentifier colorTargetIdentifier;
        RenderTargetIdentifier cameraColorTexture;

        public TemplatePass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ssdColorId = Shader.PropertyToID("_SSDColor");
            ssdDepthId = Shader.PropertyToID("_SSDDepth");

            var depthTextureDescriptor = cameraTextureDescriptor;
            depthTextureDescriptor.colorFormat = RenderTextureFormat.Depth;

            cmd.GetTemporaryRT(ssdDepthId, depthTextureDescriptor, FilterMode.Point); // 렌더 텍스쳐 받아오기
            // RenderTextureFormat.DefaultHDR >> blur가 죽어버리는 현상이 있어서 hdr로 받아서 사용
            cmd.GetTemporaryRT(ssdColorId, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

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

    private TemplatePass scriptablePass;

    public override void Create()
    {
        scriptablePass = new TemplatePass("Template Name"); // srp 이름
        scriptablePass.passMaterial = settings.passMaterial;
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(scriptablePass);
    }
}