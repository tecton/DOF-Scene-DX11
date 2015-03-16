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

    public struct PerObjectData
    {
        public Matrix world;
        public Matrix worldInvTranspose;
        public Matrix worldViewProj;
        public Material material;
        public float textured;
        public Vector3 padding;
    };

    public struct PerFrameData
    {
        public DirectionalLight dirLight;
        public Vector3 eyePosition;
        public float padding;
    };

    // A container for the meshes loaded from the file
    class Model
    {
        List<TriangleMesh> m_meshes;

        //allocate data structs for per object constant buffers
        public PerObjectData poData = new PerObjectData();

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
            poData.world = world;
            world.Invert();
            world.Transpose();
            poData.worldInvTranspose = world;

            // Update transformation matrices
            poData.worldViewProj = poData.world * viewProj;

            //transpose matrices before sending them to the shader, because hlsl do vec * matrix
            poData.world.Transpose();
            poData.worldInvTranspose.Transpose();
            poData.worldViewProj.Transpose();
        }

        public void Draw(DeviceContext context, Buffer objectBuffer)
        {
            foreach (TriangleMesh mesh in m_meshes)
            {
                //set mesh specific data
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.vertexBuffer, mesh.vertexSize, 0));
                context.PixelShader.SetShaderResource(0, mesh.diffuseTextureView);

                DataStream stream;
                DataBox databox;

                poData.material = new Material();
                poData.material.Diffuse = mesh.diffuseColor;
                poData.material.Ambient = mesh.ambientColor;
                poData.material.Specular = mesh.specularColor;
                if (mesh.diffuseTexture != null)
                    poData.textured = 1.0f;
                else
                    poData.textured = 0.0f;

                databox = context.MapSubresource(objectBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

                if (!databox.IsEmpty)
                    stream.Write(poData);
                context.UnmapSubresource(objectBuffer, 0);

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
