using DOFScene.ConstantData;
using DOFScene.Shaders;
using DOFScene.Textures;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;

namespace DOFScene.Renderers
{
    class VisionRenderer : DofRenderer
    {
        #region Shaders

        PixelShaderResource visionInputLayerPixelShader;
        PixelShaderResource visionHiddenLayer1PixelShader;
        PixelShaderResource visionHiddenLayer2PixelShader;
        PixelShaderResource visionOutputLayerPixelShader;

        #endregion

        ConstantData<EyeParamInfo> eyeParam;

        #region Texture Buffers

        // Human Vision BPNN Texture, 4 parameters packed
        ColorTexture visionParamBuffer;

        // hidden layer 1 output result, 12 values packed in 3 textures.
        ColorTexture[] visionHiddenLayer1Output;

        // hidden layer 2 output result. 12 values.
        ColorTexture[] visionHiddenLayer2Output;

        // output layer output result. 2 values.
        ColorTexture visionOutputLayerOutput;

        #endregion

        protected override void initResources()
        {
            spriteVertexShader = new VertexShaderResource(device, "ThinLensCOC.fx");

            compositePixelShader = new PixelShaderResource(device, "Composite.fx");

            visionInputLayerPixelShader = new PixelShaderResource(device, "VisionInput.fx");
            visionHiddenLayer1PixelShader = new PixelShaderResource(device, "VisionHiddenLayer1.fx");
            visionHiddenLayer2PixelShader = new PixelShaderResource(device, "VisionHiddenLayer2.fx");
            visionOutputLayerPixelShader = new PixelShaderResource(device, "VisionOutputLayer.fx");

            horizontalBlurPixelShader = new PixelShaderResource(device, "Vision_horizontal.fx");
            verticalBlurPixelShader = new PixelShaderResource(device, "VVDoF_vertical.fx");

            eyeParam = new ConstantData<EyeParamInfo>(device);

            visionParamBuffer = new ColorTexture(device, displaySize);
            visionHiddenLayer1Output = new ColorTexture[3];
            visionHiddenLayer2Output = new ColorTexture[3];
            for (int i = 0; i < 3; ++i)
            {
                visionHiddenLayer1Output[i] = new ColorTexture(device, displaySize);
                visionHiddenLayer2Output[i] = new ColorTexture(device, displaySize);
            }
            visionOutputLayerOutput = new ColorTexture(device, displaySize);
        }

        protected override void draw(RenderTargetView renderView, ColorTexture sceneColorTexture, DepthTexture sceneDepthTexture,
            Camera camera, RenderMode renderMode)
        {
            eyeParam.data.cameraFarZ = camera.farPlaneZ;
            eyeParam.data.pupil = camera.pupil;
            eyeParam.Update(context);

            drawVisionInputPass(sceneDepthTexture);
            drawVisionHiddenLayer1Pass();
            drawVisionHiddenLayer2Pass();
            drawVisionOutputLayerPass();
            drawVisionHorizontalBlurPass(sceneColorTexture);
            drawVisionVerticalBlurPass();
            drawCompositePass(renderView);
        }

        void drawVisionInputPass(DepthTexture sceneDepthTexture)
        {
            // reuse coc vertex shader
            context.VertexShader.Set(spriteVertexShader.vs);
            context.VertexShader.SetConstantBuffer(0, spritePosition.getBuffer());
            context.PixelShader.Set(visionInputLayerPixelShader.ps);
            context.PixelShader.SetConstantBuffer(0, cameraInfo.getBuffer());
            context.OutputMerger.SetRenderTargets(depthTexture.dsv, visionParamBuffer.rtv);

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(visionParamBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, sceneDepthTexture.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHiddenLayer1Pass()
        {
            context.PixelShader.Set(visionHiddenLayer1PixelShader.ps);
            context.PixelShader.SetConstantBuffer(0, eyeParam.getBuffer());
            var targets = new RenderTargetView[] { visionHiddenLayer1Output[0].rtv, visionHiddenLayer1Output[1].rtv, visionHiddenLayer1Output[2].rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            foreach (RenderTargetView rv in targets)
                context.ClearRenderTargetView(rv, Color.Black);

            context.PixelShader.SetShaderResource(0, visionParamBuffer.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHiddenLayer2Pass()
        {
            context.PixelShader.Set(visionHiddenLayer2PixelShader.ps);
            var targets = new RenderTargetView[] { visionHiddenLayer2Output[0].rtv, visionHiddenLayer2Output[1].rtv, visionHiddenLayer2Output[2].rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            foreach (RenderTargetView rv in targets)
                context.ClearRenderTargetView(rv, Color.Black);

            for (int i = 0; i < visionHiddenLayer1Output.Length; ++i)
                context.PixelShader.SetShaderResource(i, visionHiddenLayer1Output[i].srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionOutputLayerPass()
        {
            context.PixelShader.Set(visionOutputLayerPixelShader.ps);
            context.OutputMerger.SetTargets(visionOutputLayerOutput.rtv);

            // Clear views
            context.ClearRenderTargetView(visionOutputLayerOutput.rtv, Color.Black);

            for (int i = 0; i < visionHiddenLayer2Output.Length ; ++i)
                context.PixelShader.SetShaderResource(i, visionHiddenLayer2Output[i].srv);
            context.PixelShader.SetShaderResource(visionHiddenLayer2Output.Length, visionParamBuffer.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHorizontalBlurPass(ColorTexture sceneColorTexture)
        {
            context.PixelShader.Set(horizontalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(0, blurParam.getBuffer());
            var targets = new RenderTargetView[] { hBlurBuffer.rtv, hNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearRenderTargetView(hBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(hNearBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, sceneColorTexture.srv);
            context.PixelShader.SetShaderResource(1, visionOutputLayerOutput.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionVerticalBlurPass()
        {
            context.PixelShader.Set(verticalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, blurParam.getBuffer());
            var targets = new RenderTargetView[] { vBlurBuffer.rtv, vNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearRenderTargetView(vBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(vNearBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, hBlurBuffer.srv);
            context.PixelShader.SetShaderResource(1, hNearBuffer.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawCompositePass(RenderTargetView renderView)
        {
            context.PixelShader.Set(compositePixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, compositeInfo.getBuffer());
            if (this.renderToResult)
            {
                context.OutputMerger.SetRenderTargets(depthTexture.dsv, this.resultTexture.rtv);
                context.ClearRenderTargetView(this.resultTexture.rtv, Color.Black);
            }
            else
            {
                context.OutputMerger.SetRenderTargets(depthTexture.dsv, renderView);
                context.ClearRenderTargetView(renderView, Color.Black);
            }

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);

            // vision part
            context.PixelShader.SetShaderResource(3, visionParamBuffer.srv);
            context.PixelShader.SetShaderResource(4, vBlurBuffer.srv);
            context.PixelShader.SetShaderResource(5, vNearBuffer.srv);
            context.PixelShader.SetShaderResource(6, visionOutputLayerOutput.srv);

            context.Draw(6, 0);
        }
    }
}
