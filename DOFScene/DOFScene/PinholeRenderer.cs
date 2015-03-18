using SharpDX;
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
    class PinholeRenderer
    {
        Device device;
        DeviceContext context;
        System.Drawing.Size displaySize;

        public Texture2D outputBuffer;
        RenderTargetView renderView;
        Texture2D depthBuffer;
        DepthStencilView depthView;
        public ShaderResourceView depthSRV;

        ShaderBytecode vertexShaderByteCode;
        VertexShader vertexShader;
        ShaderBytecode pixelShaderByteCode;
        PixelShader pixelShader;

        InputLayout layout;

        Buffer frameConstantBuffer;
        Buffer objectConstantBuffer;

        public void Draw(Scene scene, RenderTargetView renderTargetView)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, objectConstantBuffer);
            context.VertexShader.SetConstantBuffer(1, frameConstantBuffer);
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetConstantBuffer(0, objectConstantBuffer);
            context.PixelShader.SetConstantBuffer(1, frameConstantBuffer);
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));
            context.OutputMerger.SetRenderTargets(depthView, renderView);
            //context.OutputMerger.SetRenderTargets(depthView, renderTargetView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);
            //context.ClearRenderTargetView(renderTargetView, Color.Black);

            scene.UpdateFrameConstants(context, frameConstantBuffer);
            scene.Draw(context, objectConstantBuffer);
        }

        public void Init(Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;
            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("Render.fx", "VS", "vs_5_0");
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            pixelShaderByteCode = ShaderBytecode.CompileFromFile("Render.fx", "PS", "ps_5_0");
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            frameConstantBuffer = new Buffer(device, Utilities.SizeOf<PerFrameData>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            objectConstantBuffer = new Buffer(device, Utilities.SizeOf<PerObjectData>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

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
            renderView = new RenderTargetView(device, outputBuffer);
            

            // Create Depth Buffer & View
            depthBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R24G8_Typeless,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width,
                Height = displaySize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            depthView = new DepthStencilView(device, depthBuffer, new DepthStencilViewDescription()
            {
                Flags = 0,
                Format = Format.D24_UNorm_S8_UInt,
                Dimension = DepthStencilViewDimension.Texture2D,
            });
            var srvd = new ShaderResourceViewDescription();
            srvd.Format = Format.R24_UNorm_X8_Typeless;
            srvd.Dimension = ShaderResourceViewDimension.Texture2D;
            srvd.Texture2D.MipLevels = 1;
            srvd.Texture2D.MostDetailedMip = 0;
            depthSRV = new ShaderResourceView(device, depthBuffer, srvd);

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
                    });
        }

        public void Dispose()
        {
            vertexShaderByteCode.Dispose();
            vertexShader.Dispose();
            pixelShaderByteCode.Dispose();
            pixelShader.Dispose();
        }
    }
}
