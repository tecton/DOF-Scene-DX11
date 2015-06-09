using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOFScene.Textures
{
    abstract class TextureResource
    {
        public Texture2D texture;
        public ShaderResourceView srv;
    }
}
