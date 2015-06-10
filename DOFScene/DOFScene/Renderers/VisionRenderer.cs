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
    class VisionRenderer : Renderer
    {
        InputLayout layout;

        ColorTexture resultTexture;
        DepthTexture depthTexture;

        #region Shaders

        VertexShaderResource spriteVertexShader;

        PixelShaderResource visionInputLayerPixelShader;
        PixelShaderResource visionHiddenLayer1PixelShader;
        PixelShaderResource visionHiddenLayer2PixelShader;
        PixelShaderResource visionOutputLayerPixelShader;

        PixelShaderResource visionHorizontalBlurPixelShader;
        PixelShaderResource verticalBlurPixelShader;

        PixelShaderResource compositePixelShader;

        #endregion

        #region Shader Constants

        ConstantData<SpritePositionInfo> spritePosition;
        ConstantData<CameraInfo> cameraInfo;
        ConstantData<BlurParamInfo> blurParam;
        ConstantData<CompositeInfo> compositeInfo;
        ConstantData<EyeParamInfo> eyeParam;

        SpriteConstantData spriteVertexBuffer;

        #endregion

        #region Texture Buffers

        // Human Vision BPNN Texture, 4 parameters packed
        ColorTexture visionParamBuffer;

        // hidden layer 1 output result, 12 values packed in 3 textures.
        ColorTexture[] visionHiddenLayer1Output;

        // hidden layer 2 output result. 12 values.
        ColorTexture[] visionHiddenLayer2Output;

        // output layer output result. 2 values.
        ColorTexture visionOutputLayerOutput;

        ColorTexture visionHNearBuffer;
        ColorTexture visionHBlurBuffer;
        ColorTexture visionVNearBuffer;
        ColorTexture visionVBlurBuffer;

        #endregion

        public override void Init(SharpDX.Direct3D11.Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            // Compile Vertex and Pixel shaders
            spriteVertexShader = new VertexShaderResource(device, "ThinLensCOC.fx");

            compositePixelShader = new PixelShaderResource(device, "Composite.fx");

            visionInputLayerPixelShader = new PixelShaderResource(device, "VisionInput.fx");
            visionHiddenLayer1PixelShader = new PixelShaderResource(device, "VisionHiddenLayer1.fx");
            visionHiddenLayer2PixelShader = new PixelShaderResource(device, "VisionHiddenLayer2.fx");
            visionOutputLayerPixelShader = new PixelShaderResource(device, "VisionOutputLayer.fx");

            visionHorizontalBlurPixelShader = new PixelShaderResource(device, "Vision_horizontal.fx");
            verticalBlurPixelShader = new PixelShaderResource(device, "VVDoF_vertical.fx");

            spritePosition = new ConstantData<SpritePositionInfo>(device);
            cameraInfo = new ConstantData<CameraInfo>(device);
            blurParam = new ConstantData<BlurParamInfo>(device);
            compositeInfo = new ConstantData<CompositeInfo>(device);
            eyeParam = new ConstantData<EyeParamInfo>(device);

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

            visionParamBuffer = new ColorTexture(device, size);
            visionHiddenLayer1Output = new ColorTexture[3];
            visionHiddenLayer2Output = new ColorTexture[3];
            for (int i = 0; i < 3; ++i)
            {
                visionHiddenLayer1Output[i] = new ColorTexture(device, size);
                visionHiddenLayer2Output[i] = new ColorTexture(device, size);
            }
            visionOutputLayerOutput = new ColorTexture(device, size);

            size.Width /= 2;
            visionHNearBuffer = new ColorTexture(device, size);
            visionHBlurBuffer = new ColorTexture(device, size);

            size.Height /= 2;
            visionVNearBuffer = new ColorTexture(device, size);
            visionVBlurBuffer = new ColorTexture(device, size);

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

            eyeParam.data.cameraFarZ = camera.farPlaneZ;
            eyeParam.data.pupil = pupil;
            eyeParam.Update(context);

            #endregion

            // eye vision part
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
            context.PixelShader.Set(visionHorizontalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(0, blurParam.getBuffer());
            var targets = new RenderTargetView[] { visionHBlurBuffer.rtv, visionHNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearRenderTargetView(visionHBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(visionHNearBuffer.rtv, Color.Black);

            Texture2D.ToFile(context, sceneColorTexture.texture, ImageFileFormat.Png, "b1.png");
            context.PixelShader.SetShaderResource(0, sceneColorTexture.srv);
            context.PixelShader.SetShaderResource(1, visionOutputLayerOutput.srv);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionVerticalBlurPass()
        {
            context.PixelShader.Set(verticalBlurPixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, blurParam.getBuffer());
            var targets = new RenderTargetView[] { visionVBlurBuffer.rtv, visionVNearBuffer.rtv };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearRenderTargetView(visionVBlurBuffer.rtv, Color.Black);
            context.ClearRenderTargetView(visionVNearBuffer.rtv, Color.Black);

            context.PixelShader.SetShaderResource(0, visionHBlurBuffer.srv);
            context.PixelShader.SetShaderResource(1, visionHNearBuffer.srv);

            //draw
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

            // vision part
            context.PixelShader.SetShaderResource(3, visionParamBuffer.srv);
            context.PixelShader.SetShaderResource(4, visionVBlurBuffer.srv);
            context.PixelShader.SetShaderResource(5, visionVNearBuffer.srv);
            context.PixelShader.SetShaderResource(6, visionOutputLayerOutput.srv);

            context.Draw(6, 0);
        }
    }
}
