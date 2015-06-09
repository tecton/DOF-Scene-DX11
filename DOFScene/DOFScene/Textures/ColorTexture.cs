using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DOFScene.Textures
{
    class ColorTexture : TextureResource
    {
        public RenderTargetView rtv;

        public ColorTexture(SharpDX.Direct3D11.Device device, System.Drawing.Size size)
        {
            texture = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R32G32B32A32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = size.Width,
                Height = size.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            rtv = new RenderTargetView(device, texture);
        }
    }
}
