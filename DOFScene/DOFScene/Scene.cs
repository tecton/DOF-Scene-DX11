using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace DOFScene
{
    public struct Camera
    {
        public float nearPlaneZ;
        public float farPlaneZ;
        public float focusPlaneZ;
        public float lensRadius;
        public float fov; // rad
        public float width;
        public float height;
    };

    public class Scene
    {
        public Camera camera = new Camera();

        Vector3 eyePos;
        Matrix viewProj;
        List<Model> models;

        Size size;
        Device device;

        float dragonScale = 1.65f;

        public Scene(Device device, Size size)
        {
            this.device = device;
            this.size = size;
            init();
        }

        void init()
        {
            // Prepare matrices
            var dir = new Vector3((float)(-Math.Cos(3 / 180.0 * Math.PI) * Math.Sin(52 / 180.0 * Math.PI)),
                (float)-Math.Sin(3 / 180.0 * Math.PI),
                (float)(Math.Cos(3 / 180.0 * Math.PI) * Math.Cos(52 / 180.0 * Math.PI)));
            eyePos = new Vector3(2.3f, 0.05f, -0.5f);
            //eyePos = new Vector3(0.4f, 0.05f, -0.2f);
            var view = Matrix.LookAtLH(eyePos, eyePos + dir, Vector3.UnitY);
            //var view = Matrix.LookAtLH(new Vector3(0, 0, -2), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI * 30f / 180.0f, size.Width / (float)size.Height, 0.1f, 20.0f);
            viewProj = Matrix.Multiply(view, proj);

            camera.nearPlaneZ = -0.1f;
            camera.farPlaneZ = -20.0f;
            camera.focusPlaneZ = -0.75f;
            camera.lensRadius = 0.01f;
            camera.fov = (float)Math.PI * 30f / 180.0f;
            camera.width = size.Width;
            camera.height = size.Height;

            models = new List<Model>();
            // load model
            ObjModelLoader modelLoader = new ObjModelLoader(device);

            models.Add(modelLoader.Load("blue-dragon-small.obj"));
            models.Add(modelLoader.Load("red-dragon-small.obj"));
            models.Add(modelLoader.Load("ground.obj"));
        }

        public void UpdateFrameConstants(DeviceContext context, Buffer frameConstantBuffer)
        {
            PerFrameData pfData = new PerFrameData();
            pfData.eyePosition = eyePos;
            pfData.dirLight = new DirectionalLight();
            pfData.dirLight.Ambient = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            pfData.dirLight.Diffuse = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            pfData.dirLight.Specular = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            pfData.dirLight.Direction = new Vector3(0, -1.0f, 0);

            DataStream stream;
            DataBox databox = context.MapSubresource(frameConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);

            if (!databox.IsEmpty)
                stream.Write(pfData);
            context.UnmapSubresource(frameConstantBuffer, 0);
        }

        public void Draw(DeviceContext context, Buffer perObjectBuffer)
        {
            float dragonPos = -0.6f;
            for (int i = 0; i <= 10; ++i)
            {
                var worldPos = Matrix.RotationY((float)(-10.0 / 180.0 * Math.PI)) * Matrix.Scaling(dragonScale, dragonScale, dragonScale) * Matrix.Translation(dragonPos, 0, 0);
                models[i % 2].SetWorldMatrix(worldPos, viewProj);
                models[i % 2].Draw(context, perObjectBuffer);
                dragonPos += 0.26f;
            }
            models[2].SetWorldMatrix(Matrix.Translation(0, 0, 0), viewProj);
            models[2].Draw(context, perObjectBuffer);
        }
    }
}
