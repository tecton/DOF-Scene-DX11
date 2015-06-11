using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace DOFScene
{
    public class TriangleMesh
    {
        public int vertexSize = 32;

        public Buffer vertexBuffer;

        public int vertexCount;

        public Vector4 ambientColor;

        public Vector4 diffuseColor;

        public Vector4 specularColor;

        public Texture2D diffuseTexture;

        public ShaderResourceView diffuseTextureView;

        //add texture and texture view for the shader
        public void AddTextureDiffuse(Device device, string path)
        {
            diffuseTexture = Texture2D.FromFile<Texture2D>(device, path);
            diffuseTextureView = new ShaderResourceView(device, diffuseTexture);
        }

        //dispose D3D related resources
        public void Dispose()
        {
            vertexBuffer.Dispose();
        }
    }

}
