using DOFScene.Textures;
using SharpDX.Direct3D11;

namespace DOFScene.Renderers
{
    abstract class Renderer
    {
        protected Device device;
        protected DeviceContext context;
        protected System.Drawing.Size displaySize;

        public abstract void Init(Device device, DeviceContext context, System.Drawing.Size size);
    }
}
