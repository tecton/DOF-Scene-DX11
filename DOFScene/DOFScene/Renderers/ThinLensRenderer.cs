using SharpDX.Direct3D11;
using DOFScene.Textures;
using DOFScene.Shaders;
using DOFScene.ConstantData;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using System;

namespace DOFScene.Renderers
{
    class ThinLensRenderer : DofRenderer
    {
        PixelShaderResource cocPixelShader;

        // CoC Buffer, colour + packed coc
        ColorTexture cocBuffer;

        protected override void initResources()
        {
            cocPixelShader = new PixelShaderResource(device, "ThinLensCOC.fx");
            spriteVertexShader = new VertexShaderResource(device, "ThinLensCOC.fx");
            compositePixelShader = new PixelShaderResource(device, "Composite.fx");
            horizontalBlurPixelShader = new PixelShaderResource(device, "VVDoF_horizontal.fx");
            verticalBlurPixelShader = new PixelShaderResource(device, "VVDoF_vertical.fx");

            cocBuffer = new ColorTexture(device, displaySize);
        }

        protected override void draw(RenderTargetView renderView, ColorTexture sceneColorTexture, DepthTexture sceneDepthTexture,
            Camera camera, RenderMode renderMode)
        {
            // pin-hole part
            drawCoCPass(sceneColorTexture, sceneDepthTexture);
            drawHorizontalBlurPass();
            drawVerticalBlurPass();
            drawCompositePass(renderView);
        }

        void drawCoCPass(ColorTexture sceneColorTexture, DepthTexture sceneDepthTexture)
        {
            context.VertexShader.Set(spriteVertexShader.vs);
            context.VertexShader.SetConstantBuffer(0, spritePosition.getBuffer());
            context.PixelShader.Set(cocPixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, cameraInfo.getBuffer());
            context.OutputMerger.SetRenderTargets(depthTexture.dsv, cocBuffer.rtv);

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(cocBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, sceneDepthTexture.srv);
            context.PixelShader.SetShaderResource(1, sceneColorTexture.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawHorizontalBlurPass()
        {
            // reuse coc vertex shader
            context.PixelShader.Set(horizontalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, blurParam.getBuffer());
            var targets = new RenderTargetView[] { hBlurBuffer.rtv, hNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearRenderTargetView(hBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(hNearBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, cocBuffer.srv);

            context.Draw(6, 0);
        }

        void drawVerticalBlurPass()
        {
            context.PixelShader.Set(verticalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, blurParam.getBuffer());
            var targets = new RenderTargetView[] { vBlurBuffer.rtv, vNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(vBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(vNearBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, hBlurBuffer.srv);
            context.PixelShader.SetShaderResource(1, hNearBuffer.srv);

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
            
            context.PixelShader.SetShaderResource(0, cocBuffer.srv);
            context.PixelShader.SetShaderResource(1, vBlurBuffer.srv);
            context.PixelShader.SetShaderResource(2, vNearBuffer.srv);

            context.Draw(6, 0);
        }
    }
}
