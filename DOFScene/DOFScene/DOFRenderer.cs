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
    public struct DOFConstantBuffer
    {
        public Matrix worldViewProj;
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

        Texture2D cocBuffer;
        RenderTargetView cocBufferRTV;
        ShaderResourceView cocBufferSRV;

        Buffer constantBuffer;
        int rectVertexSize = 20;
        Buffer rectVertexBuffer;
        DOFConstantBuffer dofConstants = new DOFConstantBuffer();

        void drawCoCPass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView colorSRV, ShaderResourceView depthSRV)
        {
            context.VertexShader.Set(cocVertexShader);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            context.PixelShader.Set(cocPixelShader);
            context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            context.PixelShader.SetShaderResource(0, depthSRV);
            context.PixelShader.SetShaderResource(1, colorSRV);

            //draw
            context.Draw(6, 0);
        }

        void drawCompositePass(RenderTargetView renderView, DepthStencilView depthView, ShaderResourceView cocBufferSRV)
        {
            context.VertexShader.Set(compositeVertexShader);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            context.PixelShader.Set(compositePixelShader);
            context.OutputMerger.SetRenderTargets(depthView, renderView);

            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            context.PixelShader.SetShaderResource(0, cocBufferSRV);

            //draw
            context.Draw(6, 0);
        }

        public void Draw(RenderTargetView renderView, DepthStencilView depthView, Texture2D colorBuffer, ShaderResourceView depthSRV)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));
            ShaderResourceView colorSRV = new ShaderResourceView(device, colorBuffer);

            DataStream stream;
            DataBox databox = context.MapSubresource(constantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(dofConstants);
            context.UnmapSubresource(constantBuffer, 0);

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(rectVertexBuffer, rectVertexSize, 0));

            drawCoCPass(cocBufferRTV, depthView, colorSRV, depthSRV);
            drawCompositePass(renderView, depthView, cocBufferSRV);
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

            constantBuffer = new Buffer(device, Utilities.SizeOf<DOFConstantBuffer>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

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

            float ratio = (float)displaySize.Height / displaySize.Width;
            float w = (float)displaySize.Width * 0.5f, h = (float)displaySize.Height * 0.5f;
            rectVertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, new[]
                               {
                                      // 3D coordinates              UV Texture coordinates
                                      -w, -h, 1.0f,     0.0f, 1.0f, // Front
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
