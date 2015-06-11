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
        public float pupil;
        public Vector2 focusPoint;
    };

    public class Scene
    {
        public Camera camera = new Camera();
        public float scale = 1.0f;

        protected Vector3 eyePos;
        protected Matrix viewProj;
        protected List<Model> models = new List<Model>();

        protected Size size;
        protected Device device;

        public Scene(Device device, Size size)
        {
            this.device = device;
            this.size = size;

            camera.width = size.Width;
            camera.height = size.Height;

            // focus at center of screen by default
            camera.focusPoint = new Vector2(size.Width / 2, size.Height / 2);

            init();
        }

        protected virtual void init() { }

        public virtual void UpdateLightingConstants(DeviceContext context, ConstantData<LightingDataInfo> lightingConstant) { }

        public virtual void Draw(DeviceContext context) { }
    }
}
