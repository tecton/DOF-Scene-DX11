using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DOFScene.Textures
{
    class DepthTexture : TextureResource
    {
        public DepthStencilView dsv;

        public DepthTexture(SharpDX.Direct3D11.Device device, System.Drawing.Size size)
        {
            texture = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.R24G8_Typeless,
                ArraySize = 1,
                MipLevels = 1,
                Width = size.Width,
                Height = size.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            dsv = new DepthStencilView(device, texture, new DepthStencilViewDescription()
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
            srv = new ShaderResourceView(device, texture, srvd);
        }
    }
}
