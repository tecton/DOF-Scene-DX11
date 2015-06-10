using DOFScene.ConstantData;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;

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
        public float scale = 1.0f;

        Vector3 eyePos;
        Matrix viewProj;
        List<Model> models;

        Size size;
        Device device;

        float dragonScale = 2.0f;

        float downAngle = 3f;
        float rightAngle = 52f;
        float cameraX = 2.3f;
        float cameraY = 0.05f;
        float cameraZ = -0.5f;
        float fov = 33f;

        public Scene(Device device, Size size)
        {
            this.device = device;
            this.size = size;
            init();
        }

        void init()
        {
            // Prepare matrices
            //var dir = new Vector3((float)(-Math.Cos(downAngle / 180.0 * Math.PI) * Math.Sin(rightAngle / 180.0 * Math.PI)),
            //    (float)-Math.Sin(downAngle / 180.0 * Math.PI),
            //    (float)(Math.Cos(downAngle / 180.0 * Math.PI) * Math.Cos(rightAngle / 180.0 * Math.PI)));
            //eyePos = new Vector3(cameraX, cameraY, cameraZ);
            //var view = Matrix.LookAtLH(eyePos, eyePos + dir, Vector3.UnitY);
            //var proj = Matrix.PerspectiveFovLH((float)Math.PI * fov / 180.0f, size.Width / (float)size.Height, 0.1f, 20.0f);
            //viewProj = Matrix.Multiply(view, proj);

            //camera.nearPlaneZ = -0.1f;
            //camera.farPlaneZ = -20.0f;
            //camera.focusPlaneZ = -0.75f;
            camera.lensRadius = 0.01f;
            camera.fov = (float)Math.PI * fov / 180.0f;
            camera.width = size.Width;
            camera.height = size.Height;

            models = new List<Model>();
            // load model
            ObjModelLoader modelLoader = new ObjModelLoader(device);

            models.Add(modelLoader.Load("blue-dragon-small.obj"));
            models.Add(modelLoader.Load("red-dragon-small.obj"));
            models.Add(modelLoader.Load("ground.obj"));
        }

        public void UpdateFrameConstants(DeviceContext context, ConstantData<LightingDataInfo> lightingConstant)
        {
            var dir = new Vector3((float)(-Math.Cos(downAngle / 180.0 * Math.PI) * Math.Sin(rightAngle / 180.0 * Math.PI)),
                (float)-Math.Sin(downAngle / 180.0 * Math.PI),
                (float)(Math.Cos(downAngle / 180.0 * Math.PI) * Math.Cos(rightAngle / 180.0 * Math.PI)));
            eyePos = new Vector3(cameraX * scale, cameraY * scale, cameraZ * scale);
            var view = Matrix.LookAtLH(eyePos, eyePos + dir, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI * fov / 180.0f, size.Width / (float)size.Height, 0.1f * scale, 20.0f * scale);
            viewProj = Matrix.Multiply(view, proj);

            camera.nearPlaneZ = -0.1f * scale;
            camera.farPlaneZ = -20.0f * scale;
            camera.focusPlaneZ = -0.75f * scale;

            lightingConstant.data.eyePosition = eyePos;
            lightingConstant.data.dirLight = new DirectionalLight();
            lightingConstant.data.dirLight.Ambient = new Vector4(150.0f, 150.0f, 150.0f, 1.0f); //new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            lightingConstant.data.dirLight.Diffuse = new Vector4(10, 10, 10, 1.0f); //new Vector4(1.9f, 1.9f, 1.9f, 1.0f);
            lightingConstant.data.dirLight.Specular = new Vector4(1, 1, 1, 1.0f); //new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            lightingConstant.data.dirLight.Direction = new Vector3(0, -1.0f, 0);

            lightingConstant.Update(context);
        }

        public void Draw(DeviceContext context)
        {
            float dragonPos = -0.6f * scale;
            for (int i = 0; i <= 10; ++i)
            {
                var worldPos = Matrix.RotationY((float)(-15.0 / 180.0 * Math.PI)) * Matrix.Scaling(1.8f * scale, dragonScale * scale, dragonScale * scale) * Matrix.Translation(dragonPos, 0, 0);
                models[i % 2].SetWorldMatrix(worldPos, viewProj);
                models[i % 2].Draw(context);
                dragonPos += 0.26f * scale;
            }
            models[2].SetWorldMatrix(Matrix.Scaling(scale, scale, scale) * Matrix.Translation(0, 0, 0), viewProj);
            models[2].Draw(context);
        }
    }
}
