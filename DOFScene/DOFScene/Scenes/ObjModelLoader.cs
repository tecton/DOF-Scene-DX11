﻿using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ObjLoader.Loader.Loaders;

using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Data.VertexData;

namespace DOFScene
{
    class ObjModelLoader
    {
        Device device;
        LoadResult loadResult;
        String m_modelPath;

        public ObjModelLoader(Device _device)
        {
            device = _device;
        }

        //Load model
        public Model Load(String fileName)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            using (var fileStream = new FileStream(fileName, FileMode.Open))
            {
                loadResult = objLoader.Load(fileStream);
            }

            //use this directory path to load textures from
            m_modelPath = Path.GetDirectoryName(fileName);

            Model model = new Model(device);

            AddVertexData(model, loadResult);

            return model;
        }

        private int faceCountInModelGroup(Group group)
        {
            int count = 0;
            foreach (Face face in group.Faces)
            {
                if (face.Count != 3)
                    continue;
                count++;
            }
            return count;
        }

        //Create meshes and add vertex and index buffers
        private void AddVertexData(Model model, LoadResult loadResult)
        {
            foreach (Group group in loadResult.Groups)
            {
                //create new mesh to add to model
                TriangleMesh triangleMesh = new TriangleMesh();
                model.AddMesh(ref triangleMesh);

                //add it to the mesh
                triangleMesh.vertexCount = faceCountInModelGroup(group) * 3;

                triangleMesh.ambientColor = new Vector4(group.Material.AmbientColor.X, group.Material.AmbientColor.Y, group.Material.AmbientColor.Z, 1.0f);
                triangleMesh.diffuseColor = new Vector4(group.Material.DiffuseColor.X, group.Material.DiffuseColor.Y, group.Material.DiffuseColor.Z, 1.0f);
                triangleMesh.specularColor = new Vector4(group.Material.SpecularColor.X, group.Material.SpecularColor.Y, group.Material.SpecularColor.Z, group.Material.SpecularCoefficient);

                if (group.Material.DiffuseTextureMap != null)
                    triangleMesh.AddTextureDiffuse(device, group.Material.DiffuseTextureMap);

                //create data stream for vertices
                DataStream vertexStream = new DataStream(triangleMesh.vertexCount * triangleMesh.vertexSize, true, true);

                foreach (Face face in group.Faces)
                {
                    if (face.Count != 3)
                        continue;

                    for (int i = 0; i < face.Count; i++)
                    {
                        FaceVertex faceVertex = face[i];
                        //add position
                        {
                            Vertex v = loadResult.Vertices[faceVertex.VertexIndex - 1];
                            vertexStream.Write<Vector3>(new Vector3(v.X, v.Y, v.Z));
                        }
                        {
                            Normal n = loadResult.Normals[faceVertex.NormalIndex - 1];
                            vertexStream.Write<Vector3>(new Vector3(n.X, n.Y, n.Z));
                        }
                        if (faceVertex.TextureIndex > 0)
                        {
                            Texture t = loadResult.Textures[faceVertex.TextureIndex - 1];
                            vertexStream.Write<Vector2>(new Vector2(t.X, t.Y));
                        }
                        else
                        {
                            vertexStream.Write<Vector2>(new Vector2(0, 0));
                        }
                    }
                }

                vertexStream.Position = 0;

                //create new vertex buffer
                var vertexBuffer = new Buffer(device,
                                                vertexStream,
                                                new BufferDescription()
                                                {
                                                    BindFlags = BindFlags.VertexBuffer,
                                                    CpuAccessFlags = CpuAccessFlags.None,
                                                    OptionFlags = ResourceOptionFlags.None,
                                                    SizeInBytes = triangleMesh.vertexCount * triangleMesh.vertexSize,
                                                    Usage = ResourceUsage.Default
                                                }
                                             );
                triangleMesh.vertexBuffer = vertexBuffer;
            }
        }
    }
}
