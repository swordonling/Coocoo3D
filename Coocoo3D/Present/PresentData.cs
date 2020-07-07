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
        public Matrix4x4 vpMatrix { get => innerStruct.vpMatrix; set => innerStruct.vpMatrix = value; }
        public Vector3 CameraPosition { get => innerStruct.CameraPosition; set => innerStruct.CameraPosition = value; }
        public float AspectRatio { get => innerStruct.AspectRatio; set => innerStruct.AspectRatio = value; }
        public float PlayTime { get => innerStruct.PlayTime; set => innerStruct.PlayTime = value; }
        public float DeltaTime { get => innerStruct.DeltaTime; set => innerStruct.DeltaTime = value; }
        public InnerStruct innerStruct;

        public const int c_presentDataSize = 256;
        public byte[] PresentDataUploadBuffer = new byte[c_presentDataSize];
        GCHandle gch_PresentDataUploadBuffer;
        public ConstantBuffer DataBuffer = new ConstantBuffer();

        
        public void UpdateCameraData(Camera camera)
        {
            vpMatrix = Matrix4x4.Transpose(camera.vpMatrix);
            CameraPosition = camera.Pos;
            AspectRatio = camera.AspectRatio;
        }
        public void UpdateCameraData(Lighting lighting)
        {
            vpMatrix = Matrix4x4.Transpose(lighting.vpMatrix);
            CameraPosition = lighting.Rotation;
            AspectRatio = 1;
        }

        public void UpdateBuffer(GraphicsContext graphicsContext)
        {
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(PresentDataUploadBuffer, 0);
            Marshal.StructureToPtr(innerStruct, pBufferData, true);
            graphicsContext.UpdateResource(DataBuffer, PresentDataUploadBuffer, c_presentDataSize);
        }

        public void Reload(DeviceResources deviceResources)
        {
            DataBuffer.Reload(deviceResources, c_presentDataSize);
        }
        public void Unload()
        {
            DataBuffer.Unload();
        }
        public PresentData()
        {
            gch_PresentDataUploadBuffer = GCHandle.Alloc(PresentDataUploadBuffer);
        }
        ~PresentData()
        {
            gch_PresentDataUploadBuffer.Free();
        }
        public struct InnerStruct
        {
            public Matrix4x4 vpMatrix;
            public Vector3 CameraPosition;
            public float AspectRatio;
            public float PlayTime;
            public float DeltaTime;
        }
    }
}
