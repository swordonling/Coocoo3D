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
    public class PresentData
    {
        public Matrix4x4 wpMatrix { get => innerStruct.wpMatrix; set => innerStruct.wpMatrix = value; }
        public Matrix4x4 pwMatrix { get => innerStruct.pwMatrix; set => innerStruct.pwMatrix = value; }
        public Vector3 CameraPosition { get => innerStruct.CameraPosition; set => innerStruct.CameraPosition = value; }
        public float AspectRatio { get => innerStruct.AspectRatio; set => innerStruct.AspectRatio = value; }
        public float PlayTime { get => innerStruct.PlayTime; set => innerStruct.PlayTime = value; }
        public float DeltaTime { get => innerStruct.DeltaTime; set => innerStruct.DeltaTime = value; }
        public InnerStruct innerStruct;
        public ConstantBufferStatic DataBuffer = new ConstantBufferStatic();


        public void UpdateCameraData(Camera camera)
        {
            wpMatrix = Matrix4x4.Transpose(camera.vpMatrix);
            Matrix4x4.Invert(camera.vpMatrix, out innerStruct.pwMatrix);
            pwMatrix = Matrix4x4.Transpose(pwMatrix);
            CameraPosition = camera.Pos;
            AspectRatio = camera.AspectRatio;
        }
        public void UpdateCameraData(Lighting lighting)
        {
            wpMatrix = Matrix4x4.Transpose(lighting.vpMatrix);
            Matrix4x4.Invert(lighting.vpMatrix, out innerStruct.pwMatrix);
            pwMatrix = Matrix4x4.Transpose(pwMatrix);
            CameraPosition = lighting.Rotation;
            AspectRatio = 1;
        }

        public void Reload(DeviceResources deviceResources,int presentDataSize)
        {
            DataBuffer.Reload(deviceResources, presentDataSize);
        }
        public void Unload()
        {
            DataBuffer.Unload();
        }
        public struct InnerStruct
        {
            public Matrix4x4 wpMatrix;
            public Matrix4x4 pwMatrix;
            public Vector3 CameraPosition;
            public float AspectRatio;
            public float PlayTime;
            public float DeltaTime;
            public int RandomValue1;
            public int RandomValue2;
        }
    }
}
