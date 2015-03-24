﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace DOFScene
{
    public struct PositionConstants
    {
        public Matrix worldViewProj;
    };

    public struct CameraInfoConstants
    {
        public Vector4 clipInfo;
        public float focusPlaneZ;
        public Vector3 padding;
    };

    public struct BlurConstants
    {
        public Vector2 textureSize;
        public Vector2 invTextureSize;
        public float maxCoCRadiusPixels;
        public float nearBlurRadiusPixels;
        public float invNearBlurRadiusPixels;
        public float padding;
    };

    public struct CompositeConstants
    {
        public int renderMode;
        public Vector3 padding;
    };

    class DOFRenderer
    {
        Device device;
        DeviceContext context;
        System.Drawing.Size displaySize;

        InputLayout layout;

        VertexShader cocVertexShader;
        PixelShader cocPixelShader;
        VertexShader compositeVertexShader;
        PixelShader compositePixelShader;
        VertexShader horizontalBlurVertexShader;
        PixelShader horizontalBlurPixelShader;
        VertexShader verticalBlurVertexShader;
        PixelShader verticalBlurPixelShader;

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

        int rectVertexSize = 20;
        Buffer rectVertexBuffer;

        Buffer positionConstantBuffer;
        PositionConstants dofConstants = new PositionConstants();
        Buffer cameraConstantBuffer;
        CameraInfoConstants cameraInfoConstants = new CameraInfoConstants();
        Buffer blurConstantBuffer;
        BlurConstants blurConstants = new BlurConstants();
        Buffer compositeConstantBuffer;
        CompositeConstants compositeConstants = new CompositeConstants();

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

        void drawCompositePass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView textureSRV, ShaderResourceView blurSRV, ShaderResourceView nearSRV, RenderMode renderMode)
        {
            context.VertexShader.Set(compositeVertexShader);
            context.VertexShader.SetConstantBuffer(0, positionConstantBuffer);
            context.PixelShader.Set(compositePixelShader);
            context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            context.PixelShader.SetConstantBuffer(1, compositeConstantBuffer);
            context.PixelShader.SetShaderResource(0, textureSRV);
            context.PixelShader.SetShaderResource(1, blurSRV);
            context.PixelShader.SetShaderResource(2, nearSRV);

            //draw
            context.Draw(6, 0);
        }

        public void Draw(RenderTargetView renderView, DepthStencilView depthView, Texture2D colorBuffer, ShaderResourceView depthSRV, Camera camera, RenderMode renderMode)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));
            ShaderResourceView colorSRV = new ShaderResourceView(device, colorBuffer);

            DataStream stream;
            DataBox databox = context.MapSubresource(positionConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(dofConstants);
            context.UnmapSubresource(positionConstantBuffer, 0);

            float z_n = camera.nearPlaneZ;
            float z_f = camera.farPlaneZ;
            float imagePlanePixelsPerMeter = (float)(displaySize.Height / (-2 * Math.Tan(camera.fov / 2)));
            float scale = (float)(imagePlanePixelsPerMeter * camera.lensRadius / (camera.focusPlaneZ * Math.Max(12, displaySize.Width / 100.0)));
            cameraInfoConstants.clipInfo = new Vector4(
                z_n * z_f,  z_n - z_f,  z_f, scale);
            cameraInfoConstants.focusPlaneZ = camera.focusPlaneZ;

            databox = context.MapSubresource(cameraConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(cameraInfoConstants);
            context.UnmapSubresource(cameraConstantBuffer, 0);

            databox = context.MapSubresource(blurConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(blurConstants);
            context.UnmapSubresource(blurConstantBuffer, 0);

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(rectVertexBuffer, rectVertexSize, 0));

            databox = context.MapSubresource(compositeConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            compositeConstants.renderMode = (int)renderMode;
            if (!databox.IsEmpty)
                stream.Write(compositeConstants);
            context.UnmapSubresource(compositeConstantBuffer, 0);

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(rectVertexBuffer, rectVertexSize, 0));

            drawCoCPass(cocBufferRTV, depthView, colorSRV, depthSRV);
            drawHorizontalBlurPass(depthView, cocBufferSRV);
            drawVerticalBlurPass(depthView, hBlurBufferSRV, hNearBufferSRV);
            drawCompositePass(renderView, depthView, cocBufferSRV, vBlurBufferSRV, vNearBufferSRV, renderMode);
        }

        public void Init(Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            // Compile Vertex and Pixel shaders
            var cocVertexShaderByteCode = ShaderBytecode.CompileFromFile("COC.fx", "VS", "vs_5_0");
            cocVertexShader = new VertexShader(device, cocVertexShaderByteCode);
            var cocPixelShaderByteCode = ShaderBytecode.CompileFromFile("COC.fx", "PS", "ps_5_0");
            cocPixelShader = new PixelShader(device, cocPixelShaderByteCode);

            var compositeVertexShaderByteCode = ShaderBytecode.CompileFromFile("Composite.fx", "VS", "vs_5_0");
            compositeVertexShader = new VertexShader(device, compositeVertexShaderByteCode);
            var compositePixelShaderByteCode = ShaderBytecode.CompileFromFile("Composite.fx", "PS", "ps_5_0");
            compositePixelShader = new PixelShader(device, compositePixelShaderByteCode);

            var horizontalBlurVertexShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_horizontal.fx", "VS", "vs_5_0");
            horizontalBlurVertexShader = new VertexShader(device, horizontalBlurVertexShaderByteCode);
            var horizontalBlurPixelShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_horizontal.fx", "PS", "ps_5_0");
            horizontalBlurPixelShader = new PixelShader(device, horizontalBlurPixelShaderByteCode);

            var verticalBlurVertexShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_vertical.fx", "VS", "vs_5_0");
            verticalBlurVertexShader = new VertexShader(device, verticalBlurVertexShaderByteCode);
            var verticalBlurPixelShaderByteCode = ShaderBytecode.CompileFromFile("VVDoF_vertical.fx", "PS", "ps_5_0");
            verticalBlurPixelShader = new PixelShader(device, verticalBlurPixelShaderByteCode);

            positionConstantBuffer = new Buffer(device, Utilities.SizeOf<PositionConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            cameraConstantBuffer = new Buffer(device, Utilities.SizeOf<CameraInfoConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            blurConstantBuffer = new Buffer(device, Utilities.SizeOf<BlurConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            compositeConstantBuffer = new Buffer(device, Utilities.SizeOf<CompositeConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

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

            cocBuffer = new Texture2D(device, new Texture2DDescription()
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
            cocBufferRTV = new RenderTargetView(device, cocBuffer);
            cocBufferSRV = new ShaderResourceView(device, cocBuffer);

            hNearBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width / 2,
                Height = displaySize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            hNearBufferSRV = new ShaderResourceView(device, hNearBuffer);
            hNearRTV = new RenderTargetView(device, hNearBuffer);

            hBlurBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width / 2,
                Height = displaySize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            hBlurBufferSRV = new ShaderResourceView(device, hBlurBuffer);
            hBlurRTV = new RenderTargetView(device, hBlurBuffer);

            vNearBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width / 2,
                Height = displaySize.Height / 2,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            vNearBufferSRV = new ShaderResourceView(device, vNearBuffer);
            vNearRTV = new RenderTargetView(device, vNearBuffer);

            vBlurBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width / 2,
                Height = displaySize.Height / 2,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            vBlurBufferSRV = new ShaderResourceView(device, vBlurBuffer);
            vBlurRTV = new RenderTargetView(device, vBlurBuffer);

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
            var proj =  Matrix.OrthoLH(displaySize.Width, displaySize.Height, 0, 10.0f);
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
    }
}
