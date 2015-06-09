using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
