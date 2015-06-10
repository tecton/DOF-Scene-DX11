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
    class ThinLensRenderer : Renderer
    {
        InputLayout layout;

        ColorTexture resultTexture;
        DepthTexture depthTexture;

        #region Shaders

        VertexShaderResource spriteVertexShader;
        PixelShaderResource cocPixelShader;

        PixelShaderResource horizontalBlurPixelShader;
        PixelShaderResource verticalBlurPixelShader;
        PixelShaderResource compositePixelShader;

        #endregion

        #region Shader Constants

        ConstantData<SpritePositionInfo> spritePosition;
        ConstantData<CameraInfo> cameraInfo;
        ConstantData<BlurParamInfo> blurParam;
        ConstantData<CompositeInfo> compositeInfo;

        SpriteConstantData spriteVertexBuffer;

        #endregion

        #region Texture Buffers

        // CoC Buffer, colour + packed coc
        ColorTexture cocBuffer;
        ColorTexture hNearBuffer;
        ColorTexture hBlurBuffer;
        ColorTexture vNearBuffer;
        ColorTexture vBlurBuffer;

        #endregion

        public override void Init(SharpDX.Direct3D11.Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            // Compile Vertex and Pixel shaders
            spriteVertexShader = new VertexShaderResource(device, "ThinLensCOC.fx");
            cocPixelShader = new PixelShaderResource(device, "ThinLensCOC.fx");

            compositePixelShader = new PixelShaderResource(device, "Composite.fx");

            horizontalBlurPixelShader = new PixelShaderResource(device, "VVDoF_horizontal.fx");

            verticalBlurPixelShader = new PixelShaderResource(device, "VVDoF_vertical.fx");

            spritePosition = new ConstantData<SpritePositionInfo>(device);
            cameraInfo = new ConstantData<CameraInfo>(device);
            blurParam = new ConstantData<BlurParamInfo>(device);
            compositeInfo = new ConstantData<CompositeInfo>(device);

            blurParam.data.nearBlurRadiusPixels = 12.0f;
            blurParam.data.maxCoCRadiusPixels = 12.0f;
            blurParam.data.invNearBlurRadiusPixels = 1.0f / 12.0f;
            blurParam.data.textureSize = new Vector2((float)displaySize.Width, (float)displaySize.Height);
            blurParam.data.invTextureSize = new Vector2(1.0f / (float)displaySize.Width, 1.0f / (float)displaySize.Height);

            var view = Matrix.LookAtLH(new Vector3(0, 0, 0), new Vector3(0, 0, 1), Vector3.UnitY);
            var proj = Matrix.OrthoLH(displaySize.Width, displaySize.Height, 0, 10.0f);
            spritePosition.data.worldViewProj = Matrix.Translation(0, 0, 0) * view * proj;
            spritePosition.data.worldViewProj.Transpose();

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(spriteVertexShader.byteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
                    });

            cocBuffer = new ColorTexture(device, size);

            size.Width /= 2;
            hNearBuffer = new ColorTexture(device, size);
            hBlurBuffer = new ColorTexture(device, size);

            size.Height /= 2;
            vNearBuffer = new ColorTexture(device, size);
            vBlurBuffer = new ColorTexture(device, size);

            resultTexture = new ColorTexture(device, displaySize);
            depthTexture = new DepthTexture(device, displaySize);

            spriteVertexBuffer = new SpriteConstantData(device, displaySize);
        }

        public void Draw(RenderTargetView renderView, ColorTexture sceneColorTexture, DepthTexture sceneDepthTexture,
            Camera camera, float focus, float pupil, RenderMode renderMode, System.Windows.Point focusPoint)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));

            #region Prepare Shader Constants

            float z_n = camera.nearPlaneZ;
            float z_f = camera.farPlaneZ;
            float imagePlanePixelsPerMeter = (float)(displaySize.Height / (-2 * Math.Tan(camera.fov / 2)));
            float scale = (float)(imagePlanePixelsPerMeter * pupil * 0.001 / (camera.focusPlaneZ * Math.Max(12, displaySize.Width / 100.0)));
            cameraInfo.data.clipInfo = new Vector4(z_n * z_f, z_n - z_f, z_f, scale);
            cameraInfo.data.focusPlaneZ = -focus;
            float top = (float)(z_f * Math.Tan(camera.fov / 2));
            float right = top / camera.height * camera.width;
            cameraInfo.data.frustum = new Vector3(right, top, z_f);
            cameraInfo.data.focusPoint.X = (float)focusPoint.X / camera.width;
            cameraInfo.data.focusPoint.Y = (float)focusPoint.Y / camera.height;
            cameraInfo.Update(context);

            spritePosition.Update(context);
            blurParam.Update(context);
            
            compositeInfo.data.renderMode = (int)renderMode;
            compositeInfo.data.focusPosition.X = (float)focusPoint.X / camera.width;
            compositeInfo.data.focusPosition.Y = (float)focusPoint.Y / camera.height;
            compositeInfo.Update(context);

            spriteVertexBuffer.Update(context);

            #endregion

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
            //context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
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
            context.OutputMerger.SetRenderTargets(depthTexture.dsv, renderView);

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);
            
            context.PixelShader.SetShaderResource(0, cocBuffer.srv);
            context.PixelShader.SetShaderResource(1, vBlurBuffer.srv);
            context.PixelShader.SetShaderResource(2, vNearBuffer.srv);

            context.Draw(6, 0);
        }
    }
}
