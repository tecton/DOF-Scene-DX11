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
using DOFScene.ConstantData;

namespace DOFScene
{
    // A container for the meshes loaded from the file
    class Model
    {
        List<TriangleMesh> m_meshes;

        //allocate data structs for per object constant buffers
        public PinholeObjectConstant objectConstant;

        Device device;

        public Model(Device _device)
        {
            m_meshes = new List<TriangleMesh>();
            device = _device;
            objectConstant = new PinholeObjectConstant(device);
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
            objectConstant.data.world = world;
            world.Invert();
            world.Transpose();
            objectConstant.data.worldInvTranspose = world;

            // Update transformation matrices
            objectConstant.data.worldViewProj = objectConstant.data.world * viewProj;

            //transpose matrices before sending them to the shader, because hlsl do vec * matrix
            objectConstant.data.world.Transpose();
            objectConstant.data.worldInvTranspose.Transpose();
            objectConstant.data.worldViewProj.Transpose();
        }

        public void Draw(DeviceContext context)
        {
            context.VertexShader.SetConstantBuffer(0, objectConstant.getBuffer());
            context.PixelShader.SetConstantBuffer(0, objectConstant.getBuffer());

            foreach (TriangleMesh mesh in m_meshes)
            {
                //set mesh specific data
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.vertexBuffer, mesh.vertexSize, 0));
                context.PixelShader.SetShaderResource(0, mesh.diffuseTextureView);

                objectConstant.data.material = new Material();
                objectConstant.data.material.Diffuse = mesh.diffuseColor;
                objectConstant.data.material.Ambient = mesh.ambientColor;
                objectConstant.data.material.Specular = mesh.specularColor;
                if (mesh.diffuseTexture != null)
                    objectConstant.data.textured = 1.0f;
                else
                    objectConstant.data.textured = 0.0f;

                objectConstant.Update(context);

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
    }
}
