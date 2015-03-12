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
    public class Scene
    {
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
            var pos = new Vector3(2.3f, 0.05f, -0.5f);
            var view = Matrix.LookAtLH(pos, pos + dir, Vector3.UnitY);
            //var view = Matrix.LookAtLH(new Vector3(0, 0, -2), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI * 30f / 180.0f, size.Width / (float)size.Height, 0.1f, 4000.0f);
            viewProj = Matrix.Multiply(view, proj);

            models = new List<Model>();
            // load model
            ObjModelLoader modelLoader = new ObjModelLoader(device);

            models.Add(modelLoader.Load("blue-dragon-small.obj"));
            models.Add(modelLoader.Load("red-dragon-small.obj"));
            models.Add(modelLoader.Load("ground.obj"));
        }

        public void Draw(DeviceContext context, Buffer vcBuffer, Buffer pcBuffer)
        {
            float dragonPos = -0.6f;
            for (int i = 0; i <= 10; ++i)
            {
                models[i % 2].SetWorldMatrix(Matrix.RotationY((float)(-10.0 / 180.0 * Math.PI)) * Matrix.Scaling(dragonScale, dragonScale, dragonScale) * Matrix.Translation(dragonPos, 0, 0), viewProj);
                models[i % 2].Draw(context, vcBuffer, pcBuffer);
                dragonPos += 0.26f;
            }
            models[2].SetWorldMatrix(Matrix.Translation(0, 0, 0), viewProj);
            models[2].Draw(context, vcBuffer, pcBuffer);
        }
    }
}
