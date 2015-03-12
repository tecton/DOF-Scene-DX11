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
    //data to pass to the vertex and pixel shader
    public struct VertexShaderData
    {
        public Matrix worldViewProj;
        public Matrix world;
    };

    public struct PixelShaderData
    {
        public Vector4 diffuseColor;
        public float useTexture;
        public float useLight;
        public Vector2 padding;
        public Vector4 lightPos;
        public Vector4 lightAmbient;
    };

    // A container for the meshes loaded from the file
    class Model
    {
        List<TriangleMesh> m_meshes;

        //allocate data structs for the vertex and pixel constant buffers
        public VertexShaderData vsData = new VertexShaderData();

        Device device;

        public Model()
        {
            m_meshes = new List<TriangleMesh>();
        }

        public void AddMesh(ref TriangleMesh mesh)
        {
            m_meshes.Add(mesh);
        }

        public void RemoveMesh(ref TriangleMesh mesh)
        {
            m_meshes.Remove(mesh);
        }

        public void SetWorldMatrix(Matrix world, Matrix viewProj)
        {
            vsData.world = world;

            // Update transformation matrices
            vsData.worldViewProj = vsData.world * viewProj;

            //transpose matrices before sending them to the shader
            vsData.world.Transpose();
            vsData.worldViewProj.Transpose();
        }

        public void Draw(DeviceContext context, Buffer vcBuffer, Buffer pcBuffer)
        {
            foreach (TriangleMesh mesh in m_meshes)
            {
                //set mesh specific data
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.vertexBuffer, mesh.vertexSize, 0));
                context.PixelShader.SetShaderResource(0, mesh.diffuseTextureView);

                DataStream stream;
                DataBox databox = context.MapSubresource(vcBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

                if (!databox.IsEmpty)
                    stream.Write(vsData);
                context.UnmapSubresource(vcBuffer, 0);

                PixelShaderData psData = new PixelShaderData();
                psData.diffuseColor = mesh.diffuseColor != null ? mesh.diffuseColor : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                psData.useLight = 1.0f;
                if (mesh.diffuseTexture != null)
                    psData.useTexture = 1.0f;
                else
                    psData.useTexture = 0.0f;
                //set light position
                psData.lightPos = new Vector4(0, 5, 0, 0);
                psData.lightAmbient = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);

                databox = context.MapSubresource(pcBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

                if (!databox.IsEmpty)
                    stream.Write(psData);
                context.UnmapSubresource(pcBuffer, 0);

                //draw
                context.Draw(mesh.vertexCount, 0);
            }
        }

        public void Dispose()
        {
            foreach (TriangleMesh mesh in m_meshes)
            {
                mesh.Dispose();
            }
        }

        internal void setShader(Device device)
        {
            this.device = device;
        }
    }
}
