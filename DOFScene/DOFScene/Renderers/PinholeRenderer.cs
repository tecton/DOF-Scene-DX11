#define SCREENSHOT
using DOFScene.ConstantData;
using DOFScene.Shaders;
using DOFScene.Textures;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace DOFScene.Renderers
{
    class PinholeRenderer : Renderer
    {
        public ColorTexture outputTexture;
        public DepthTexture depthTexture;

        VertexShaderResource vertexShader;
        PixelShaderResource pixelShader;

        InputLayout layout;

        ConstantData<LightingDataInfo> lightingConstant;

        public void Draw(Scene scene)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vertexShader.vs);
            context.PixelShader.Set(pixelShader.ps);
            context.PixelShader.SetConstantBuffer(1, lightingConstant.getBuffer());
            context.Rasterizer.SetViewport(new Viewport(0, 0, displaySize.Width, displaySize.Height, 0.0f, 1.0f));
            context.OutputMerger.SetRenderTargets(depthTexture.dsv, outputTexture.rtv);

            // Clear views
            context.ClearDepthStencilView(depthTexture.dsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(outputTexture.rtv, Color.Black);

            scene.UpdateFrameConstants(context, lightingConstant);
            scene.Draw(context);
        }

        public override void Init(Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            // Compile Vertex and Pixel shaders
            vertexShader = new VertexShaderResource(device, "Render.fx");
            pixelShader = new PixelShaderResource(device, "Render.fx");

            lightingConstant = new ConstantData<LightingDataInfo>(device);

            // Create color and depth texture
            outputTexture = new ColorTexture(device, size);
            depthTexture = new DepthTexture(device, size);
            
            // Layout from VertexShader input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShader.byteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
                    });
        }

        public void Dispose()
        {
            // TODO
            vertexShader.Dispose();
            pixelShader.Dispose();
        }
    }
}
