using DOFScene.ConstantData;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace DOFScene.Renderers
{
    class DOFRenderer
    {
        Device device;
        DeviceContext context;
        System.Drawing.Size displaySize;

        InputLayout layout;

        public Texture2D outputBuffer;
        RenderTargetView outputRTV;

        #region Shaders

        // thin lens
        VertexShader cocVertexShader;
        PixelShader cocPixelShader;
        VertexShader compositeVertexShader;
        PixelShader compositePixelShader;

        VertexShader horizontalBlurVertexShader;
        PixelShader horizontalBlurPixelShader;
        VertexShader verticalBlurVertexShader;
        PixelShader verticalBlurPixelShader;

        // human vision
        PixelShader visionInputLayerPixelShader;
        PixelShader visionHiddenLayer1PixelShader;
        PixelShader visionHiddenLayer2PixelShader;
        PixelShader visionOutputLayerPixelShader;

        PixelShader visionHorizontalBlurPixelShader;
        PixelShader visionVerticalBlurPixelShader;

        #endregion

        #region Texture Buffers

        // Pinhole Camera CoC Buffer, colour + packed coc
        Texture2D cocBuffer;
        RenderTargetView cocBufferRTV;
        ShaderResourceView cocBufferSRV;
        Texture2D hNearBuffer;
        RenderTargetView hNearRTV;
        ShaderResourceView hNearBufferSRV;
        Texture2D hBlurBuffer;
        ShaderResourceView hBlurBufferSRV;
        RenderTargetView hBlurRTV;
        Texture2D vNearBuffer;
        RenderTargetView vNearRTV;
        ShaderResourceView vNearBufferSRV;
        Texture2D vBlurBuffer;
        ShaderResourceView vBlurBufferSRV;
        RenderTargetView vBlurRTV;

        // Human Vision BPNN Texture, 4 parameters packed
        Texture2D visionParamBuffer;
        RenderTargetView visionParamBufferRTV;
        ShaderResourceView visionParamBufferSRV;

        // hidden layer 1 output result, 12 values packed in 3 textures.
        Texture2D[] visionHiddenLayer1Output;
        RenderTargetView[] visionHiddenLayer1OutputRTV;
        ShaderResourceView[] visionHiddenLayer1OutputSRV;

        // hidden layer 2 output result. 12 values.
        Texture2D[] visionHiddenLayer2Output;
        RenderTargetView[] visionHiddenLayer2OutputRTV;
        ShaderResourceView[] visionHiddenLayer2OutputSRV;

        // output layer output result. 2 values.
        Texture2D visionOutputLayerOutput;
        RenderTargetView visionOutputLayerOutputRTV;
        ShaderResourceView visionOutputLayerOutputSRV;

        Texture2D visionHNearBuffer;
        RenderTargetView visionHNearRTV;
        ShaderResourceView visionHNearBufferSRV;
        Texture2D visionHBlurBuffer;
        ShaderResourceView visionHBlurBufferSRV;
        RenderTargetView visionHBlurRTV;
        Texture2D visionVNearBuffer;
        RenderTargetView visionVNearRTV;
        ShaderResourceView visionVNearBufferSRV;
        Texture2D visionVBlurBuffer;
        ShaderResourceView visionVBlurBufferSRV;
        RenderTargetView visionVBlurRTV;

        #endregion

        int rectVertexSize = 20;
        Buffer rectVertexBuffer;
        bool saveScreenshots = false;

        #region Shader Constants

        Buffer positionConstantBuffer;
        SpritePositionInfo dofConstants = new SpritePositionInfo();
        Buffer cameraConstantBuffer;
        CameraInfo cameraInfoConstants = new CameraInfo();
        Buffer blurConstantBuffer;
        BlurParamInfo blurConstants = new BlurParamInfo();
        Buffer compositeConstantBuffer;
        CompositeInfo compositeConstants = new CompositeInfo();
        Buffer eyeParamConstantBuffer;
        EyeParamInfo eyeParamConstants = new EyeParamInfo();

        #endregion

        public void setScreenshots(bool toSave)
        {
            this.saveScreenshots = toSave;
        }

        public void Draw(RenderTargetView renderView, DepthStencilView depthView, Texture2D colorBuffer, ShaderResourceView depthSRV,
            Camera camera, float focus, float pupil, RenderMode renderMode, System.Windows.Point focusPoint)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));
            ShaderResourceView colorSRV = new ShaderResourceView(device, colorBuffer);

            #region Prepare Shader Constants
            DataStream stream;

            DataBox databox = context.MapSubresource(positionConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            if (!databox.IsEmpty)
                stream.Write(dofConstants);
            context.UnmapSubresource(positionConstantBuffer, 0);

            float z_n = camera.nearPlaneZ;
            float z_f = camera.farPlaneZ;
            float imagePlanePixelsPerMeter = (float)(displaySize.Height / (-2 * Math.Tan(camera.fov / 2)));
            float scale = (float)(imagePlanePixelsPerMeter * pupil * 0.001 / (camera.focusPlaneZ * Math.Max(12, displaySize.Width / 100.0)));
            cameraInfoConstants.clipInfo = new Vector4(
                z_n * z_f, z_n - z_f, z_f, scale);
            //cameraInfoConstants.focusPlaneZ = camera.focusPlaneZ;
            cameraInfoConstants.focusPlaneZ = -focus;

            float top = (float)(z_f * Math.Tan(camera.fov / 2));
            float right = top / camera.height * camera.width;
            cameraInfoConstants.frustum = new Vector3(right, top, z_f);

            cameraInfoConstants.focusPoint.X = (float)focusPoint.X / camera.width;
            cameraInfoConstants.focusPoint.Y = (float)focusPoint.Y / camera.height;

            databox = context.MapSubresource(cameraConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            if (!databox.IsEmpty)
                stream.Write(cameraInfoConstants);
            context.UnmapSubresource(cameraConstantBuffer, 0);

            databox = context.MapSubresource(blurConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            if (!databox.IsEmpty)
                stream.Write(blurConstants);
            context.UnmapSubresource(blurConstantBuffer, 0);

            databox = context.MapSubresource(compositeConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            compositeConstants.renderMode = (int)renderMode;
            compositeConstants.focusPosition.X = (float)focusPoint.X / camera.width;
            compositeConstants.focusPosition.Y = (float)focusPoint.Y / camera.height;
            if (!databox.IsEmpty)
                stream.Write(compositeConstants);
            context.UnmapSubresource(compositeConstantBuffer, 0);

            //eyeParamConstants.focus = focus;
            eyeParamConstants.cameraFarZ = camera.farPlaneZ;
            eyeParamConstants.pupil = pupil;
            databox = context.MapSubresource(eyeParamConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            if (!databox.IsEmpty)
                stream.Write(eyeParamConstants);
            context.UnmapSubresource(eyeParamConstantBuffer, 0);
            #endregion

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(rectVertexBuffer, rectVertexSize, 0));

            // pin-hole part
            drawCoCPass(cocBufferRTV, depthView, colorSRV, depthSRV);
            //Texture2D.ToFile(context, cocBuffer, ImageFileFormat.Png, "aa.png");
            drawHorizontalBlurPass(depthView, cocBufferSRV);
            drawVerticalBlurPass(depthView, hBlurBufferSRV, hNearBufferSRV);
            //drawCompositePass(renderView, depthView, cocBufferSRV, vBlurBufferSRV, vNearBufferSRV, renderMode);

            // eye vision part
            drawVisionInputPass(visionParamBufferRTV, depthView, depthSRV);
            Texture2D.ToFile(context, visionParamBuffer, ImageFileFormat.Png, "a.png");
            drawVisionHiddenLayer1Pass(visionHiddenLayer1OutputRTV, depthView, visionParamBufferSRV);
            drawVisionHiddenLayer2Pass(visionHiddenLayer2OutputRTV, depthView, visionHiddenLayer1OutputSRV);
            drawVisionOutputLayerPass(visionOutputLayerOutputRTV, depthView, visionHiddenLayer2OutputSRV, visionParamBufferSRV);
            Texture2D.ToFile(context, visionOutputLayerOutput, ImageFileFormat.Png, "b.png");
            Texture2D.ToFile(context, visionHNearBuffer, ImageFileFormat.Png, "c.png");
            Texture2D.ToFile(context, visionVNearBuffer, ImageFileFormat.Png, "d.png");
            drawVisionHorizontalBlurPass(depthView, colorSRV, visionOutputLayerOutputSRV);
            drawVisionVerticalBlurPass(depthView, visionHBlurBufferSRV, visionHNearBufferSRV);
            drawCompositePass(renderView, depthView, cocBufferSRV, vBlurBufferSRV, vNearBufferSRV,
                visionParamBufferSRV, visionOutputLayerOutputSRV, visionVBlurBufferSRV, visionVNearBufferSRV, renderMode);
            //drawCompositePass(renderView, depthView, cocBufferSRV, vBlurBufferSRV, vNearBufferSRV,
            //      visionParamBufferSRV, visionOutputLayerOutputSRV, visionVBlurBufferSRV, visionVNearBufferSRV, renderMode);
        }

        public void Init(Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            // Compile Vertex and Pixel shaders
            var cocVertexShaderByteCode = ShaderBytecode.CompileFromFile("shaders/ThinLensCOC.fx", "VS", "vs_5_0");
            cocVertexShader = new VertexShader(device, cocVertexShaderByteCode);
            var cocPixelShaderByteCode = ShaderBytecode.CompileFromFile("shaders/ThinLensCOC.fx", "PS", "ps_5_0");
            cocPixelShader = new PixelShader(device, cocPixelShaderByteCode);

            var compositeVertexShaderByteCode = ShaderBytecode.CompileFromFile("shaders/Composite.fx", "VS", "vs_5_0");
            compositeVertexShader = new VertexShader(device, compositeVertexShaderByteCode);
            var compositePixelShaderByteCode = ShaderBytecode.CompileFromFile("shaders/Composite.fx", "PS", "ps_5_0");
            compositePixelShader = new PixelShader(device, compositePixelShaderByteCode);

            var horizontalBlurVertexShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_horizontal.fx", "VS", "vs_5_0");
            horizontalBlurVertexShader = new VertexShader(device, horizontalBlurVertexShaderByteCode);
            var horizontalBlurPixelShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_horizontal.fx", "PS", "ps_5_0");
            horizontalBlurPixelShader = new PixelShader(device, horizontalBlurPixelShaderByteCode);

            var verticalBlurVertexShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_vertical.fx", "VS", "vs_5_0");
            verticalBlurVertexShader = new VertexShader(device, verticalBlurVertexShaderByteCode);
            var verticalBlurPixelShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_vertical.fx", "PS", "ps_5_0");
            verticalBlurPixelShader = new PixelShader(device, verticalBlurPixelShaderByteCode);

            var visionInputPixelShaderByteCode = ShaderBytecode.CompileFromFile("VisionInput.fx", "PS", "ps_5_0");
            visionInputLayerPixelShader = new PixelShader(device, visionInputPixelShaderByteCode);

            var visionHiddenLayer1PixelShaderByteCode = ShaderBytecode.CompileFromFile("VisionHiddenLayer1.fx", "PS", "ps_5_0");
            visionHiddenLayer1PixelShader = new PixelShader(device, visionHiddenLayer1PixelShaderByteCode);

            var visionHiddenLayer2PixelShaderByteCode = ShaderBytecode.CompileFromFile("VisionHiddenLayer2.fx", "PS", "ps_5_0");
            visionHiddenLayer2PixelShader = new PixelShader(device, visionHiddenLayer2PixelShaderByteCode);

            var visionOutputLayerPixelShaderByteCode = ShaderBytecode.CompileFromFile("VisionOutputLayer.fx", "PS", "ps_5_0");
            visionOutputLayerPixelShader = new PixelShader(device, visionOutputLayerPixelShaderByteCode);

            var visionHorizontalBlurPixelShaderByteCode = ShaderBytecode.CompileFromFile("Vision_horizontal.fx", "PS", "ps_5_0");
            visionHorizontalBlurPixelShader = new PixelShader(device, visionHorizontalBlurPixelShaderByteCode);

            positionConstantBuffer = new Buffer(device, Utilities.SizeOf<SpritePositionInfo>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            cameraConstantBuffer = new Buffer(device, Utilities.SizeOf<CameraInfo>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            blurConstantBuffer = new Buffer(device, Utilities.SizeOf<BlurParamInfo>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            compositeConstantBuffer = new Buffer(device, Utilities.SizeOf<CompositeInfo>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            eyeParamConstantBuffer = new Buffer(device, Utilities.SizeOf<EyeParamInfo>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

            blurConstants.nearBlurRadiusPixels = 12.0f;
            blurConstants.maxCoCRadiusPixels = 12.0f;
            blurConstants.invNearBlurRadiusPixels = 1.0f / 12.0f;
            blurConstants.textureSize = new Vector2((float)displaySize.Width, (float)displaySize.Height);
            blurConstants.invTextureSize = new Vector2(1.0f / (float)displaySize.Width, 1.0f / (float)displaySize.Height);

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(cocVertexShaderByteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
                    });

            cocBuffer = createTextureBuffer(1, 1);
            cocBufferRTV = new RenderTargetView(device, cocBuffer);
            cocBufferSRV = new ShaderResourceView(device, cocBuffer);

            hNearBuffer = createTextureBuffer(2, 1);
            hNearBufferSRV = new ShaderResourceView(device, hNearBuffer);
            hNearRTV = new RenderTargetView(device, hNearBuffer);

            hBlurBuffer = createTextureBuffer(2, 1);
            hBlurBufferSRV = new ShaderResourceView(device, hBlurBuffer);
            hBlurRTV = new RenderTargetView(device, hBlurBuffer);

            vNearBuffer = createTextureBuffer(2, 2);
            vNearBufferSRV = new ShaderResourceView(device, vNearBuffer);
            vNearRTV = new RenderTargetView(device, vNearBuffer);

            vBlurBuffer = createTextureBuffer(2, 2);
            vBlurBufferSRV = new ShaderResourceView(device, vBlurBuffer);
            vBlurRTV = new RenderTargetView(device, vBlurBuffer);

            visionParamBuffer = createTextureBuffer(1, 1);
            visionParamBufferSRV = new ShaderResourceView(device, visionParamBuffer);
            visionParamBufferRTV = new RenderTargetView(device, visionParamBuffer);

            visionHiddenLayer1Output = new Texture2D[3];
            visionHiddenLayer1OutputRTV = new RenderTargetView[3];
            visionHiddenLayer1OutputSRV = new ShaderResourceView[3];
            visionHiddenLayer2Output = new Texture2D[3];
            visionHiddenLayer2OutputRTV = new RenderTargetView[3];
            visionHiddenLayer2OutputSRV = new ShaderResourceView[3];
            for (int i = 0; i < 3; ++i)
            {
                visionHiddenLayer1Output[i] = createTextureBuffer(1, 1);
                visionHiddenLayer1OutputSRV[i] = new ShaderResourceView(device, visionHiddenLayer1Output[i]);
                visionHiddenLayer1OutputRTV[i] = new RenderTargetView(device, visionHiddenLayer1Output[i]);
                visionHiddenLayer2Output[i] = createTextureBuffer(1, 1);
                visionHiddenLayer2OutputSRV[i] = new ShaderResourceView(device, visionHiddenLayer2Output[i]);
                visionHiddenLayer2OutputRTV[i] = new RenderTargetView(device, visionHiddenLayer2Output[i]);
            }

            visionOutputLayerOutput = createTextureBuffer(1, 1);
            visionOutputLayerOutputSRV = new ShaderResourceView(device, visionOutputLayerOutput);
            visionOutputLayerOutputRTV = new RenderTargetView(device, visionOutputLayerOutput);

            visionHNearBuffer = createTextureBuffer(2, 1);
            visionHNearBufferSRV = new ShaderResourceView(device, visionHNearBuffer);
            visionHNearRTV = new RenderTargetView(device, visionHNearBuffer);

            visionHBlurBuffer = createTextureBuffer(2, 1);
            visionHBlurBufferSRV = new ShaderResourceView(device, visionHBlurBuffer);
            visionHBlurRTV = new RenderTargetView(device, visionHBlurBuffer);

            visionVNearBuffer = createTextureBuffer(2, 2);
            visionVNearBufferSRV = new ShaderResourceView(device, visionVNearBuffer);
            visionVNearRTV = new RenderTargetView(device, visionVNearBuffer);

            visionVBlurBuffer = createTextureBuffer(2, 2);
            visionVBlurBufferSRV = new ShaderResourceView(device, visionVBlurBuffer);
            visionVBlurRTV = new RenderTargetView(device, visionVBlurBuffer);

            // for automatic screen save
            outputBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width,
                Height = displaySize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            outputRTV = new RenderTargetView(device, outputBuffer);

            float ratio = (float)displaySize.Height / displaySize.Width;
            float w = (float)displaySize.Width * 0.5f, h = (float)displaySize.Height * 0.5f;
            rectVertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, new[]
                               {
                                      // 3D coordinates UV Texture coordinates
                                      -w, -h, 1.0f,     0.0f, 1.0f,
                                      -w,  h, 1.0f,     0.0f, 0.0f,
                                       w,  h, 1.0f,     1.0f, 0.0f,
                                      -w, -h, 1.0f,     0.0f, 1.0f,
                                       w,  h, 1.0f,     1.0f, 0.0f,
                                       w, -h, 1.0f,     1.0f, 1.0f,
                                });

            var view = Matrix.LookAtLH(new Vector3(0, 0, 0), new Vector3(0, 0, 1), Vector3.UnitY);
            var proj = Matrix.OrthoLH(displaySize.Width, displaySize.Height, 0, 10.0f);
            dofConstants.worldViewProj = Matrix.Translation(0, 0, 0) * view * proj;
            dofConstants.worldViewProj.Transpose();
        }

        public void Dispose()
        {
            cocVertexShader.Dispose();
            cocPixelShader.Dispose();
            compositeVertexShader.Dispose();
            compositePixelShader.Dispose();
        }

        #region thin lens methods

        void drawCoCPass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView colorSRV, ShaderResourceView depthSRV)
        {
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(cocPixelShader);
            context.PixelShader.SetConstantBuffer(1, cameraConstantBuffer);
            context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            context.PixelShader.SetShaderResource(0, depthSRV);
            context.PixelShader.SetShaderResource(1, colorSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawHorizontalBlurPass(DepthStencilView depthView, ShaderResourceView blurInputSRV)
        {
            context.VertexShader.Set(horizontalBlurVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(horizontalBlurPixelShader);
            context.PixelShader.SetConstantBuffer(1, blurConstantBuffer);
            var targets = new RenderTargetView[] { hBlurRTV, hNearRTV };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(hBlurRTV, Color.Black);
            context.ClearRenderTargetView(hNearRTV, Color.Black);

            context.PixelShader.SetShaderResource(0, blurInputSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawVerticalBlurPass(DepthStencilView depthView, ShaderResourceView blurInputSRV, ShaderResourceView nearInputSRV)
        {
            context.VertexShader.Set(verticalBlurVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(verticalBlurPixelShader);
            context.PixelShader.SetConstantBuffer(1, blurConstantBuffer);
            var targets = new RenderTargetView[] { vBlurRTV, vNearRTV };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(vBlurRTV, Color.Black);
            context.ClearRenderTargetView(vNearRTV, Color.Black);

            context.PixelShader.SetShaderResource(0, blurInputSRV);
            context.PixelShader.SetShaderResource(1, nearInputSRV);

            //draw
            context.Draw(6, 0);
        }

        #endregion

        #region human vision methods

        void drawVisionInputPass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView depthSRV)
        {
            // reuse coc vertex shader
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(visionInputLayerPixelShader);
            context.PixelShader.SetConstantBuffer(0, cameraConstantBuffer);
            context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            context.PixelShader.SetShaderResource(0, depthSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHiddenLayer1Pass(RenderTargetView[] renderView, DepthStencilView depthView, ShaderResourceView visionInputParamSRV)
        {
            // reuse coc vertex shader
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(visionHiddenLayer1PixelShader);
            context.PixelShader.SetConstantBuffer(0, eyeParamConstantBuffer);
            context.OutputMerger.SetTargets(renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            foreach (RenderTargetView rv in renderView)
                context.ClearRenderTargetView(rv, Color.Black);

            context.PixelShader.SetShaderResource(0, visionInputParamSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHiddenLayer2Pass(RenderTargetView[] renderView, DepthStencilView depthView, ShaderResourceView[] visionInputParamSRV)
        {
            // reuse coc vertex shader
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(visionHiddenLayer2PixelShader);
            context.OutputMerger.SetTargets(renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            foreach (RenderTargetView rv in renderView)
                context.ClearRenderTargetView(rv, Color.Black);

            for (int i = 0; i < visionInputParamSRV.Length; ++i)
                context.PixelShader.SetShaderResource(i, visionInputParamSRV[i]);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionOutputLayerPass(RenderTargetView renderView, DepthStencilView depthView,
            ShaderResourceView[] visionInputParamSRV, ShaderResourceView visionParamSRV)
        {
            // reuse coc vertex shader
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(visionOutputLayerPixelShader);
            context.OutputMerger.SetTargets(renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            for (int i = 0; i < visionInputParamSRV.Length; ++i)
                context.PixelShader.SetShaderResource(i, visionInputParamSRV[i]);
            context.PixelShader.SetShaderResource(visionInputParamSRV.Length, visionParamSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionHorizontalBlurPass(DepthStencilView depthView, ShaderResourceView blurInputSRV, ShaderResourceView visionCocSRV)
        {
            context.VertexShader.Set(horizontalBlurVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(visionHorizontalBlurPixelShader);
            context.PixelShader.SetConstantBuffer(0, blurConstantBuffer);
            var targets = new RenderTargetView[] { visionHBlurRTV, visionHNearRTV };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(visionHBlurRTV, Color.Black);
            context.ClearRenderTargetView(visionHNearRTV, Color.Black);

            context.PixelShader.SetShaderResource(0, blurInputSRV);
            context.PixelShader.SetShaderResource(1, visionCocSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawVisionVerticalBlurPass(DepthStencilView depthView, ShaderResourceView blurInputSRV, ShaderResourceView nearInputSRV)
        {
            context.VertexShader.Set(verticalBlurVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(verticalBlurPixelShader);
            context.PixelShader.SetConstantBuffer(1, blurConstantBuffer);
            var targets = new RenderTargetView[] { visionVBlurRTV, visionVNearRTV };
            context.OutputMerger.SetTargets(targets);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(visionVBlurRTV, Color.Black);
            context.ClearRenderTargetView(visionVNearRTV, Color.Black);

            context.PixelShader.SetShaderResource(0, blurInputSRV);
            context.PixelShader.SetShaderResource(1, nearInputSRV);

            //draw
            context.Draw(6, 0);
        }

        #endregion

        void drawCompositePass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView textureSRV,
            ShaderResourceView blurSRV, ShaderResourceView nearSRV,
            ShaderResourceView visionParamSRV, ShaderResourceView visionCoCSRV, ShaderResourceView visionBlurSRV, ShaderResourceView visionNearSRV,
            RenderMode renderMode)
        {
            context.VertexShader.Set(compositeVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(compositePixelShader);
            // just for automatic screen save
            if (saveScreenshots)
                context.OutputMerger.SetRenderTargets(depthView, outputRTV);
            else
                context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            // pin-hole part
            context.PixelShader.SetConstantBuffer(1, compositeConstantBuffer);
            context.PixelShader.SetShaderResource(0, textureSRV);
            context.PixelShader.SetShaderResource(1, blurSRV);
            context.PixelShader.SetShaderResource(2, nearSRV);

            // vision part
            context.PixelShader.SetShaderResource(3, visionParamSRV);
            context.PixelShader.SetShaderResource(4, visionBlurSRV);
            context.PixelShader.SetShaderResource(5, visionNearSRV);
            context.PixelShader.SetShaderResource(6, visionCoCSRV);

            //draw
            context.Draw(6, 0);
        }

        private Texture2D createTextureBuffer(int splitW, int splitH)
        {
            return new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width / splitW,
                Height = displaySize.Height / splitH,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
        }

    }
}
