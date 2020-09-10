using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Present
{
    public class Camera
    {
        public Matrix4x4 vMatrix;
        public Matrix4x4 pMatrix;
        public Matrix4x4 vpMatrix;

        public Vector3 LookAtPoint = new Vector3(0, 10, 0);
        public float Distance = 45;
        public Vector3 Angle;
        public float Fov = MathF.PI / 6;
        public float AspectRatio = 1;
        public Vector3 Pos { get => _pos; }
        Vector3 _pos;
        public CameraMotion cameraMotion = new CameraMotion();
        public bool CameraMotionOn = false;

        public void SetCameraMotion(float time)
        {
            var keyFrame = cameraMotion.GetCameraMotion(time);
            Distance = -keyFrame.distance;
            Angle = keyFrame.rotation;
            Fov = Math.Clamp(keyFrame.FOV, 0.1f, 179.9f) / 180 * MathF.PI;
            LookAtPoint = keyFrame.position;
        }

        public void RotateDelta(Vector3 delta)
        {
            Angle += delta;
        }

        public void MoveDelta(Vector3 delta)
        {
            Matrix4x4 rotateMatrix = Matrix4x4.CreateFromYawPitchRoll(-Angle.Y, -Angle.X, -Angle.Z);
            LookAtPoint += Vector3.Transform(delta, rotateMatrix);
        }
        /// <summary>获取摄像机矩阵前调用它。</summary>
        public void Update()
        {
            Matrix4x4 rotateMatrix = Matrix4x4.CreateFromYawPitchRoll(-Angle.Y, -Angle.X, -Angle.Z);
            var pos = Vector3.Transform(-Vector3.UnitZ * Distance, rotateMatrix * Matrix4x4.CreateTranslation(LookAtPoint));
            var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotateMatrix));
            vMatrix = Matrix4x4.CreateLookAt(pos, LookAtPoint, up);
            pMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov, AspectRatio, 0.1f, 1000f) * Matrix4x4.CreateScale(-1, 1, 1);
            vpMatrix = Matrix4x4.Multiply(vMatrix, pMatrix);
            _pos = pos;
        }
    }
}
