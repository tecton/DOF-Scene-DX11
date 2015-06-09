using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;

namespace DOFScene.Shaders
{
    abstract class ShaderResource : IDisposable
    {
        protected const string SHADER_DIR = "shaders/";
        protected bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                    disposeResource();
                }
            }
            //dispose unmanaged resources
            disposed = true;
        }

        protected abstract void disposeResource();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    class VertexShaderResource : ShaderResource
    {
        public ShaderBytecode byteCode;
        public VertexShader vs;

        public VertexShaderResource(Device device, string name)
        {
            byteCode = ShaderBytecode.CompileFromFile(SHADER_DIR + name, "VS", "vs_5_0");
            vs = new VertexShader(device, byteCode);
        }

        protected override void disposeResource()
        {
            byteCode.Dispose();
            vs.Dispose();
        }
    }

    class PixelShaderResource : ShaderResource
    {
        public ShaderBytecode byteCode;
        public PixelShader ps;

        public PixelShaderResource(Device device, string name)
        {
            byteCode = ShaderBytecode.CompileFromFile(SHADER_DIR + name, "PS", "ps_5_0");
            ps = new PixelShader(device, byteCode);
        }

        protected override void disposeResource()
        {
            byteCode.Dispose();
            ps.Dispose();
        }
    }
}
