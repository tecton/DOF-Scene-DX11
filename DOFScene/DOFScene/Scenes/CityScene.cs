//using SharpDX;
//using SharpDX.Direct3D11;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Buffer = SharpDX.Direct3D11.Buffer;

//namespace DOFScene
//{
//    class CityScene : Scene
//    {
//        Vector3 eyePos;
//        Matrix viewProj;
//        List<Model> models;
//        List<Matrix> positions;

//        Size size;
//        Device device;

//        float dragonScale = 2.0f;

//        float downAngle = 3f;
//        float rightAngle = 52f;
//        float cameraX = 2.3f;
//        float cameraY = 0.05f;
//        float cameraZ = -0.5f;
//        float fov = 27f;

//        public CityScene(Device device, Size size) : base(device, size)
//        {
//            this.device = device;
//            this.size = size;
//            init();
//        }

//        void init()
//        {
//            // Prepare matrices
//            camera.lensRadius = 0.08f;
//            camera.fov = (float)Math.PI * fov / 180.0f;
//            camera.width = size.Width;
//            camera.height = size.Height;

//            models = new List<Model>();
//            // load model
//            ObjModelLoader modelLoader = new ObjModelLoader(device);

//            models.Add(modelLoader.Load("box.obj"));

//            loadPositions("microcity.txt");
//        }

//        void loadPositions(string filename)
//        {
//            positions = new List<Matrix>();
//            using (StreamReader sr = new StreamReader(filename))
//            {
//                while (sr.Peek() >= 0)
//                {
//                    string[] bits = sr.ReadLine().Split(' ');
//                    float[] values = new float[16];
//                    for (int i = 0; i < 16; ++i)
//                        values[i] = float.Parse(bits[i]);
//                    positions.Add(new Matrix(values));
//                }
//            }
//        }

//        public void UpdateFrameConstants(DeviceContext context, Buffer frameConstantBuffer)
//        {
//            scale = 1.0f;
//            eyePos = new Vector3(0, 18, -18);
//            var view = Matrix.LookAtLH(eyePos, new Vector3(0, 0, 0), Vector3.UnitY);
//            //eyePos = new Vector3(-1.42702f, 3.30238f, -1.79759f);
//            //var view = Matrix.LookAtLH(eyePos,
//            //    new Vector3(-0.023598f, 9.69691f, -4.68208f),
//            //    new Vector3(-0.00016145f, 0.388419f, 0.921483f));
//            var proj = Matrix.PerspectiveFovLH((float)Math.PI * fov / 180.0f, size.Width / (float)size.Height, 0.1f * scale, 200.0f * scale);
//            viewProj = Matrix.Multiply(view, proj);

//            camera.nearPlaneZ = -0.1f * scale;
//            camera.farPlaneZ = -200.0f * scale;
//            camera.focusPlaneZ = -2.7f * scale;

//            PerFrameData pfData = new PerFrameData();
//            pfData.eyePosition = eyePos;
//            pfData.dirLight = new DirectionalLight();
//            pfData.dirLight.Ambient = new Vector4(150.0f, 150.0f, 150.0f, 1.0f); //new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
//            pfData.dirLight.Diffuse = new Vector4(10, 10, 10, 1.0f); //new Vector4(1.9f, 1.9f, 1.9f, 1.0f);
//            pfData.dirLight.Specular = new Vector4(1, 1, 1, 1.0f); //new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
//            pfData.dirLight.Direction = new Vector3(0, -1.0f, 0);

//            DataStream stream;
//            DataBox databox = context.MapSubresource(frameConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

//            if (!databox.IsEmpty)
//                stream.Write(pfData);
//            context.UnmapSubresource(frameConstantBuffer, 0);
//        }

//        public void Draw(DeviceContext context, Buffer perObjectBuffer)
//        {
//            //float dragonPos = -0.6f * scale;
//            //for (int i = 0; i <= 10; ++i)
//            //{
//            //    var worldPos = Matrix.RotationY((float)(-15.0 / 180.0 * Math.PI)) * Matrix.Scaling(1.8f * scale, dragonScale * scale, dragonScale * scale) * Matrix.Translation(dragonPos, 0, 0);
//            //    models[i % 2].SetWorldMatrix(worldPos, viewProj);
//            //    models[i % 2].Draw(context, perObjectBuffer);
//            //    dragonPos += 0.26f * scale;
//            //}
//            //models[2].SetWorldMatrix(Matrix.Scaling(scale, scale, scale) * Matrix.Translation(0, 0, 0), viewProj);
//            //models[2].Draw(context, perObjectBuffer);
//            foreach (Matrix m in positions)
//            {
//                models[0].SetWorldMatrix(m, viewProj);
//                models[0].Draw(context, perObjectBuffer);
//            }
//        }
//    }
//}
