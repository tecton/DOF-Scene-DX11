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
    class DisplayWindow
    {
        #region Display Form
        Form form = new Form();
        RenderControl dx11Control = new RenderControl();
        System.Drawing.Size displaySize = new System.Drawing.Size(1000, 424);
        #endregion

        #region DirectX Related
        bool initialized = false;
        SwapChainDescription swapChainDesc;
        SwapChain swapChain;
        Device device;
        DeviceContext context;

        Texture2D backBuffer;
        RenderTargetView renderView;
        Texture2D depthBuffer;
        DepthStencilView depthView;

        ShaderBytecode vertexShaderByteCode;
        VertexShader vertexShader;
        ShaderBytecode pixelShaderByteCode;
        PixelShader pixelShader;

        InputLayout layout;

        Buffer frameConstantBuffer;
        Buffer objectConstantBuffer;
        #endregion

        #region Virtual Scene
        Scene scene;
        #endregion

        #region Initializer
        public DisplayWindow()
        {
            init();
        }
        #endregion

        #region Interfaces
        public void Draw()
        {
            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.Black);

            scene.UpdateFrameConstants(context, frameConstantBuffer);
            scene.Draw(context, objectConstantBuffer);

            swapChain.Present(0, PresentFlags.None);
        }

        public void showWindow()
        {
            form.Show();
            Draw();
        }

        public void hideWindow()
        {
            form.Hide();
        }

        public void closeWindow()
        {
            form.Close();
        }

        public void Dispose()
        {
            // Release all resources
            vertexShaderByteCode.Dispose();
            vertexShader.Dispose();
            pixelShaderByteCode.Dispose();
            pixelShader.Dispose();
            layout.Dispose();
            depthView.Dispose();
            depthBuffer.Dispose();
            renderView.Dispose();
            backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            swapChain.Dispose();
        }
        #endregion

        #region private methods
        private void init()
        {
            // set window size
            dx11Control.Size = displaySize;
            System.Drawing.Size formSize = displaySize;
            formSize.Height += 40;
            formSize.Width += 40;
            form.Size = formSize;
            form.Left = 0;
            form.Controls.Add(dx11Control);

            if (initialized)
                Dispose();


            // SwapChain description
            swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(displaySize.Width, displaySize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = this.dx11Control.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, swapChainDesc, out device, out swapChain);
            context = device.ImmediateContext;

            // New RenderTargetView from the backbuffer
            backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);

            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("LoadMesh.fx", "VS", "vs_5_0");
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            pixelShaderByteCode = ShaderBytecode.CompileFromFile("LoadMesh.fx", "PS", "ps_5_0");
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
                    });

            frameConstantBuffer = new Buffer(device, Utilities.SizeOf<PerFrameData>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            objectConstantBuffer = new Buffer(device, Utilities.SizeOf<PerObjectData>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

            // Create Depth Buffer & View
            depthBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = displaySize.Width,
                Height = displaySize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            depthView = new DepthStencilView(device, depthBuffer);

            var sampler = new SamplerState(device, new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = 16,
            });

            // Prepare All the stages
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, objectConstantBuffer);
            context.VertexShader.SetConstantBuffer(1, frameConstantBuffer);
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetConstantBuffer(0, objectConstantBuffer);
            context.PixelShader.SetConstantBuffer(1, frameConstantBuffer);
            context.PixelShader.SetSampler(0, sampler);
            context.Rasterizer.SetViewport(new Viewport(0, 0, form.Size.Width, form.Size.Height, 0.0f, 1.0f));
            context.OutputMerger.SetTargets(depthView, renderView);

            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Create Scene
            scene = new Scene(device, form.Size);
        }
        #endregion
    }
}
