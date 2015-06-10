using SharpDX;
using SharpDX.Direct3D11;

namespace DOFScene.ConstantData
{
    public struct DirectionalLight
    {
        public Vector4 Ambient;
        public Vector4 Diffuse;
        public Vector4 Specular;
        public Vector3 Direction;
        public float padding;
    };

    public struct Material
    {
        public Vector4 Ambient;
        public Vector4 Diffuse;
        public Vector4 Specular; // w = SpecPower
    };

    #region constant data struct used in shader

    public struct ObjectDataInfo
    {
        public Matrix world;
        public Matrix worldInvTranspose;
        public Matrix worldViewProj;
        public Material material;
        public float textured;
        public Vector3 padding;
    };

    public struct LightingDataInfo
    {
        public DirectionalLight dirLight;
        public Vector3 eyePosition;
        public float padding;
    };

    public struct SpritePositionInfo
    {
        public Matrix worldViewProj;
    };

    // used in pinhole camera coc
    public struct CameraInfo
    {
        public Vector4 clipInfo;
        public float focusPlaneZ;
        public Vector3 frustum;
        public Vector4 focusPoint;
    };

    // used in pinhole camera blur
    public struct BlurParamInfo
    {
        public Vector2 textureSize;
        public Vector2 invTextureSize;
        public float maxCoCRadiusPixels;
        public float nearBlurRadiusPixels;
        public float invNearBlurRadiusPixels;
        public float padding;
    };

    // used in composite shader
    public struct CompositeInfo
    {
        public int renderMode;
        public Vector3 focusPosition;
    };

    // used in vision hidden layer1
    public struct EyeParamInfo
    {
        public float cameraFarZ;
        public float pupil;
        public Vector2 padding;
    };

    #endregion

    public class ConstantData<T> where T : struct
    {
        Buffer buffer;

        public Buffer getBuffer() { return buffer; }

        public T data = new T();

        public ConstantData(Device device)
        {
            buffer = new Buffer(device, Utilities.SizeOf<T>(), ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
        }

        public void Update(DeviceContext context)
        {
            DataStream stream;
            DataBox databox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(data);
            context.UnmapSubresource(buffer, 0);
        }
    }

    public class SpriteConstantData
    {
        Buffer buffer;

        public Buffer getBuffer() { return buffer; }

        VertexBufferBinding vertexBufferBinding;

        int spriteVertexSize = 20;

        public SpriteConstantData(Device device, System.Drawing.Size size)
        {
            float w = (float)size.Width * 0.5f, h = (float)size.Height * 0.5f;
            buffer = Buffer.Create(device, BindFlags.VertexBuffer, new[]
                               {
                                      // 3D coordinates UV Texture coordinates
                                      -w, -h, 1.0f,     0.0f, 1.0f,
                                      -w,  h, 1.0f,     0.0f, 0.0f,
                                       w,  h, 1.0f,     1.0f, 0.0f,
                                      -w, -h, 1.0f,     0.0f, 1.0f,
                                       w,  h, 1.0f,     1.0f, 0.0f,
                                       w, -h, 1.0f,     1.0f, 1.0f,
                                });
            vertexBufferBinding = new VertexBufferBinding(buffer, spriteVertexSize, 0);
        }

        public void Update(DeviceContext context)
        {
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        }
    }
}
