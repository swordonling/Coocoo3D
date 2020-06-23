using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3DGraphics;

namespace Coocoo3D.Present
{
    public class Lighting : ISceneObject, INotifyPropertyChanged
    {
        public string Name = "";
        public LightingType LightingType;
        public Vector3 Rotation;
        public Vector3 Position;
        public Vector4 Color;

        public Matrix4x4 vpMatrix;
        public Matrix4x4 rotateMatrix;
        public event PropertyChangedEventHandler PropertyChanged;

        public void PropChange(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public override string ToString()
        {
            return Name;
        }

        public void UpdateLightingData(GraphicsContext graphicsContext, float ExtendRange, Camera camera)
        {
            if (LightingType == LightingType.Directional)
            {
                Vector3 lookat = camera.LookAtPoint + Vector3.UnitY * 8;
                bool extendY = (camera.Angle.X % MathF.PI + MathF.PI) % MathF.PI < MathF.PI / 4;
                if (extendY)
                    lookat += Vector3.Normalize((camera.LookAtPoint - camera.Pos) * new Vector3(1, 0, 1)) * ExtendRange;

                
                rotateMatrix = Matrix4x4.CreateFromYawPitchRoll(-Rotation.Y, Rotation.X, camera.Angle.Y);
                var pos = Vector3.Transform(-Vector3.UnitZ * 128, rotateMatrix);
                var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotateMatrix));
                Matrix4x4 vMatrix = Matrix4x4.CreateLookAt(pos + lookat, lookat, up);
                Matrix4x4 pMatrix;
                if (!extendY)
                    pMatrix = Matrix4x4.CreateOrthographic(camera.Distance + ExtendRange, camera.Distance + ExtendRange, 0.0f, 512) * Matrix4x4.CreateScale(-1, 1, 1);
                else
                    pMatrix = Matrix4x4.CreateOrthographic(camera.Distance + ExtendRange * 2, camera.Distance + ExtendRange * 2, 0.0f, 512) * Matrix4x4.CreateScale(-1, 1, 1);
                vpMatrix = Matrix4x4.Multiply(vMatrix, pMatrix);

            }
        }
    }
    public enum LightingType
    {
        Directional
    }
}
