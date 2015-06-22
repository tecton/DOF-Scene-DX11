using DOFScene.ConstantData;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace DOFScene
{
    class MicroCityScene : Scene
    {
        float fov = 13.5f;
        List<Matrix> positions;

        public MicroCityScene(Device device, Size size) : base(device, size)
        {
        }

        protected override void init()
        {
            camera.lensRadius = 0.001f;
            camera.fov = (float)Math.PI * fov / 180.0f;

            ObjModelLoader modelLoader = new ObjModelLoader(device);

            models.Add(modelLoader.Load("box.obj"));

            loadPositions("microcity.txt");
        }

        void loadPositions(string filename)
        {
            positions = new List<Matrix>();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() >= 0)
                {
                    string[] bits = sr.ReadLine().Split(' ');
                    float[] values = new float[16];
                    for (int i = 0; i < 16; ++i)
                        values[i] = float.Parse(bits[i]);
                    positions.Add(new Matrix(values));
                }
            }
        }

        public override void UpdateLightingConstants(DeviceContext context, ConstantData<LightingDataInfo> lightingConstant)
        {
            scale = 1.0f;
            //eyePos = new Vector3(0, -2, -8);
            //var view = Matrix.LookAtLH(eyePos, new Vector3(0, 4, 2), Vector3.UnitY);
            eyePos = new Vector3(1.42702f, -3.30238f, -1.79759f);
            var view = Matrix.LookAtLH(eyePos,
                new Vector3(-0.023598f, 9.69691f, 4.68208f),
                new Vector3(-0.00016145f, 0.388419f, -0.921483f));
            var proj = Matrix.PerspectiveFovLH((float)Math.PI * fov / 180.0f, size.Width / (float)size.Height, 0.1f * scale, 200.0f * scale);
            viewProj = Matrix.Multiply(view, proj);

            camera.nearPlaneZ = -0.1f * scale;
            camera.farPlaneZ = -200.0f * scale;
            camera.focusPlaneZ = -2.7f * scale;

            lightingConstant.data.eyePosition = eyePos;
            lightingConstant.data.dirLight = new DirectionalLight();
            lightingConstant.data.dirLight.Ambient = new Vector4(150.0f, 150.0f, 150.0f, 1.0f); //new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            lightingConstant.data.dirLight.Diffuse = new Vector4(10, 10, 10, 1.0f); //new Vector4(1.9f, 1.9f, 1.9f, 1.0f);
            lightingConstant.data.dirLight.Specular = new Vector4(1, 1, 1, 1.0f); //new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            lightingConstant.data.dirLight.Direction = new Vector3(0, 0, 1);

            lightingConstant.Update(context);
        }

        public override void Draw(DeviceContext context)
        {
            //models[0].SetWorldMatrix(Matrix.Translation(0, 0, 0), viewProj);
            //models[0].Draw(context);
            foreach (Matrix m in positions)
            {
                models[0].SetWorldMatrix(m, viewProj);
                models[0].Draw(context);
            }
        }
    }
}
