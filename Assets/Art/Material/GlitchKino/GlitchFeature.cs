using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlitchFeature : ScriptableRendererFeature
{
     [System.Serializable]
    public class GlitchSettings  
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material passMaterial = null;
    }

    public GlitchSettings settings = new GlitchSettings(); 

    class GlitchPass : ScriptableRenderPass
    {
        public Material passMaterial;
        string profilerTag;

        int ssdColorId;
        int ssdDepthId;

        RenderTargetIdentifier colorTargetIdentifier;
        RenderTargetIdentifier cameraColorTexture;

        public GlitchPass(string profilerTag)
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

    private GlitchPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new GlitchPass("Glitch");
        scriptablePass.passMaterial = settings.passMaterial;
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(scriptablePass);
    }
}
