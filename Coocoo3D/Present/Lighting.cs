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
    public enum LightingType : uint
    {
        Directional = 0,
        Point = 1,
    }
    public struct LightingData
    {
        public LightingType LightingType;
        public Vector3 Rotation;
        public Vector4 Color;
        public Matrix4x4 vpMatrix;
        public Matrix4x4 rotateMatrix;
    }
    public struct LightingData2
    {
        public LightingType LightingType;
        public Vector3 Rotation;
        public Vector4 Color;
        public Matrix4x4 vpMatrix;
        public Matrix4x4 rotateMatrix;
    }
    public class Lighting : ISceneObject, INotifyPropertyChanged
    {
        public string Name = "";
        public LightingType LightingType;
        public Vector3 Rotation;
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

        public void UpdateLightingData(float ExtendRange, Camera camera)
        {
            if (LightingType == LightingType.Directional)
            {
                Vector3 lookat = camera.LookAtPoint + Vector3.UnitY * 8;
                bool extendY = ((camera.Angle.X + MathF.PI / 4) % MathF.PI + MathF.PI) % MathF.PI < MathF.PI / 2;


                rotateMatrix = Matrix4x4.CreateFromYawPitchRoll(-Rotation.Y, Rotation.X, camera.Angle.Z);
                var pos = Vector3.Transform(-Vector3.UnitZ * 128, rotateMatrix);
                var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotateMatrix));
                Matrix4x4 vMatrix = Matrix4x4.CreateLookAt(pos + lookat, lookat, up);
                Matrix4x4 pMatrix;

                float a = MathF.Abs((camera.Angle.X % MathF.PI + MathF.PI) % MathF.PI - MathF.PI / 2) / (MathF.PI / 4) - 0.5f;
                a = Math.Clamp(a * a - 0.25f, 0, 1);
                float dist = MathF.Abs(camera.Distance);
                if (extendY)
                    lookat += Vector3.Normalize((camera.LookAtPoint - camera.Pos) * new Vector3(1, 0, 1)) * ExtendRange * 3 * a;
                if (!extendY)
                    pMatrix = Matrix4x4.CreateOrthographic(dist + ExtendRange, dist + ExtendRange, 0.0f, 512) * Matrix4x4.CreateScale(-1, 1, 1);
                else
                {
                    pMatrix = Matrix4x4.CreateOrthographic(dist + ExtendRange * (4 * a + 1), dist + ExtendRange * (4 * a + 1), 0.0f, 512) * Matrix4x4.CreateScale(-1, 1, 1);
                }
                vpMatrix = Matrix4x4.Multiply(vMatrix, pMatrix);

            }
            else if (LightingType == LightingType.Point)
            {

            }
        }
        public LightingData GetLightingData()
        {
            return new LightingData()
            {
                Color = Color,
                LightingType = LightingType,
                rotateMatrix = rotateMatrix,
                Rotation = Rotation,
                vpMatrix = vpMatrix,
            };
        }
    }
}
