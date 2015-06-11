using DOFScene.Renderers;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System.Windows;
using System.Windows.Forms;
using Device = SharpDX.Direct3D11.Device;


namespace DOFScene
{
    enum RenderMode
    {
        ThinLensResult,
        ThinLensSignedCOC,
        ThinLensNearBuffer,
        Pinhole,
        ThinLensBlurred,
        VisionParam,
        VisionResult,
        VisionXCoC,
        VisionYCoC
    };

    class DisplayWindow
    {
        #region Display Form
        const int WIDTH = 1000;
        const int HEIGHT = 424;
        Form form = new Form();
        RenderControl dx11Control = new RenderControl();
        System.Drawing.Size displaySize = new System.Drawing.Size(WIDTH, HEIGHT);
        #endregion

        #region DirectX Related
        bool initialized = false;
        SwapChainDescription swapChainDesc;
        SwapChain swapChain;
        Device device;
        DeviceContext context;

        Texture2D backBuffer;
        RenderTargetView renderTargetView;

        SamplerState sampler;
        #endregion

        #region Virtual Scene
        public Scene scene;
        //public System.Windows.Point focusPoint = new System.Windows.Point(WIDTH / 2.0, HEIGHT / 2.0);
        bool saveScreenshots = false;
        RenderMode renderMode;
        #endregion

        #region Renderers
        PinholeRenderer pinholeRenderer = new PinholeRenderer();
        DofRenderer visionRenderer = new VisionRenderer();
        DofRenderer thinLensRenderer = new ThinLensRenderer();
        #endregion

        #region Initializer

        public DisplayWindow()
        {
            init();
        }

        #endregion

        #region Interfaces

        //public Form getForm()
        //{
        //    return form;
        //}

        public void setScreenshots(bool toSave)
        {
            this.saveScreenshots = toSave;
            //dofRenderer.setScreenshots(toSave);
        }

        public void setFocusPoint(int x, int y)
        {
            scene.camera.focusPoint.X = x;
            scene.camera.focusPoint.Y = y;
        }

        public void Draw(RenderMode renderMode, float focus, float pupil, float scale)
        {
            scene.scale = scale;
            scene.camera.focusPlaneZ = focus;
            scene.camera.pupil = pupil;
            this.renderMode = renderMode;
            draw();
        }

        public void showWindow()
        {
            form.Show();
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
            renderTargetView.Dispose();
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
            formSize.Height += (int)(SystemParameters.WindowCaptionHeight + 2 * (SystemParameters.ResizeFrameHorizontalBorderHeight + SystemParameters.FixedFrameHorizontalBorderHeight));
            formSize.Width += (int)(SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth) * 2;
            form.Size = formSize;
            form.Controls.Add(dx11Control);
            this.dx11Control.MouseClick += new MouseEventHandler(handleMouseEvent);

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
            renderTargetView = new RenderTargetView(device, backBuffer);

            sampler = new SamplerState(device, new SamplerStateDescription()
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

            // global shader setting (only need to setup one here)
            context.PixelShader.SetSampler(0, sampler);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Create Scene
            scene = new Scene(device, form.Size);

            // Init two renderers
            pinholeRenderer.Init(device, context, displaySize);
            visionRenderer.Init(device, context, displaySize);
            thinLensRenderer.Init(device, context, displaySize);
        }

        private void draw()
        {
            pinholeRenderer.Draw(scene);
            //string filename = focus + "-" + scale * 0.7524f + ".png";
            string filename = "frames/" + this.renderMode + "-P-" + scene.camera.pupil + "-R-" + scene.scale * 0.7524f + "-F-" + scene.camera.focusPoint.X + "-" + scene.camera.focusPoint.Y + ".png";
            if (renderMode < RenderMode.VisionResult)
                thinLensRenderer.Draw(renderTargetView, pinholeRenderer.outputTexture, pinholeRenderer.depthTexture, scene.camera, renderMode);
            else
                visionRenderer.Draw(renderTargetView, pinholeRenderer.outputTexture, pinholeRenderer.depthTexture, scene.camera, renderMode);
            //if (saveScreenshots)
            //    Texture2D.ToFile(context, dofRenderer.outputBuffer, ImageFileFormat.Png, filename);
            swapChain.Present(0, PresentFlags.None);
        }

        private void handleMouseEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // update focus point
                this.scene.camera.focusPoint.X = e.Location.X;// +(int)(SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth);
                this.scene.camera.focusPoint.Y = e.Location.Y;// +(int)(SystemParameters.WindowCaptionHeight + SystemParameters.ResizeFrameHorizontalBorderHeight + SystemParameters.FixedFrameHorizontalBorderHeight);
                draw();
            }
        }

        #endregion
    }
}
