using DOFScene.ConstantData;
using DOFScene.Shaders;
using DOFScene.Textures;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOFScene.Renderers
{
    class DofRenderer : Renderer
    {
        protected InputLayout layout;

        protected ColorTexture resultTexture;
        protected DepthTexture depthTexture;

        #region Shaders

        protected VertexShaderResource spriteVertexShader;
        protected PixelShaderResource horizontalBlurPixelShader;
        protected PixelShaderResource verticalBlurPixelShader;
        protected PixelShaderResource compositePixelShader;

        #endregion

        #region Shader Constants

        protected ConstantData<SpritePositionInfo> spritePosition;
        protected ConstantData<CameraInfo> cameraInfo;
        protected ConstantData<BlurParamInfo> blurParam;
        protected ConstantData<CompositeInfo> compositeInfo;

        protected SpriteConstantData spriteVertexBuffer;

        #endregion

        #region Texture Buffers

        protected ColorTexture hNearBuffer;
        protected ColorTexture hBlurBuffer;
        protected ColorTexture vNearBuffer;
        protected ColorTexture vBlurBuffer;

        #endregion

        public override void Init(SharpDX.Direct3D11.Device device, DeviceContext context, System.Drawing.Size size)
        {
            this.device = device;
            this.context = context;
            this.displaySize = size;

            spritePosition = new ConstantData<SpritePositionInfo>(device);
            cameraInfo = new ConstantData<CameraInfo>(device);
            blurParam = new ConstantData<BlurParamInfo>(device);
            compositeInfo = new ConstantData<CompositeInfo>(device);

            initResources();

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

            draw(renderView, sceneColorTexture, sceneDepthTexture, camera, focus, pupil, renderMode, focusPoint);
        }

        protected virtual void initResources()
        {
        }

        protected virtual void draw(RenderTargetView renderView, ColorTexture sceneColorTexture, DepthTexture sceneDepthTexture,
            Camera camera, float focus, float pupil, RenderMode renderMode, System.Windows.Point focusPoint)
        {
        }
    }
}
