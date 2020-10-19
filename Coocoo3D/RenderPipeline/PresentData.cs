using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class PresentData
    {
        public float PlayTime { get => innerStruct.PlayTime; set => innerStruct.PlayTime = value; }
        public float DeltaTime { get => innerStruct.DeltaTime; set => innerStruct.DeltaTime = value; }
        public InnerStruct innerStruct;
        public ConstantBuffer DataBuffer = new ConstantBuffer();


        public void UpdateCameraData(Present.CameraData camera)
        {
            innerStruct.wpMatrix = Matrix4x4.Transpose(camera.vpMatrix);
            Matrix4x4.Invert(camera.vpMatrix, out innerStruct.pwMatrix);
            innerStruct.pwMatrix = Matrix4x4.Transpose(innerStruct.pwMatrix);
            innerStruct.CameraPosition = camera.Pos;
            innerStruct.AspectRatio = camera.AspectRatio;
        }

        public void Reload(DeviceResources deviceResources, int presentDataSize)
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
            public InShaderSettings inShaderSettings;
        }
    }
}
