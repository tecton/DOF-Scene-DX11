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

    public struct ObjectData
    {
        public Matrix world;
        public Matrix worldInvTranspose;
        public Matrix worldViewProj;
        public Material material;
        public float textured;
        public Vector3 padding;
    };

    public struct LightingData
    {
        public DirectionalLight dirLight;
        public Vector3 eyePosition;
        public float padding;
    };

    public abstract class ConstantData
    {
        protected Buffer buffer;

        public Buffer getBuffer() { return buffer; }

        public abstract void Update(DeviceContext context);
    }

    public class PinholeLightingConstant : ConstantData
    {
        public LightingData data = new LightingData();

        public PinholeLightingConstant(Device device)
        {
            buffer = new Buffer(device, Utilities.SizeOf<LightingData>(), ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
        }

        public override void Update(DeviceContext context)
        {
            DataStream stream;
            DataBox databox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(data);
            context.UnmapSubresource(buffer, 0);
        }
    }

    public class PinholeObjectConstant : ConstantData
    {
        public ObjectData data = new ObjectData();

        public PinholeObjectConstant(Device device)
        {
            buffer = new Buffer(device, Utilities.SizeOf<ObjectData>(), ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
        }

        public override void Update(DeviceContext context)
        {
            DataStream stream;
            DataBox databox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(data);
            context.UnmapSubresource(buffer, 0);
        }
    }
}
