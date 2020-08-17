using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3D.Utility;
using Coocoo3DGraphics;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.RenderPipeline
{
    public enum MiscProcessType
    {
        GenerateIrradianceMap,
    }
    public struct MiscProcessPair<T0, T1>
    {
        public MiscProcessPair(T0 t0, T1 t1, MiscProcessType type)
        {
            this.t0 = t0;
            this.t1 = t1;
            this.Type = type;
        }
        public T0 t0;
        public T1 t1;
        public MiscProcessType Type;
    }
    public class MiscProcessContext
    {
        public List<MiscProcessPair<TextureCube, RenderTextureCube>> miscProcessPairs = new List<MiscProcessPair<TextureCube, RenderTextureCube>>();
        public void Add(MiscProcessPair<TextureCube, RenderTextureCube> pair)
        {
            lock (miscProcessPairs)
            {
                miscProcessPairs.Add(pair);
            }
        }

        public void MoveToAnother(MiscProcessContext context)
        {
            miscProcessPairs.MoveTo_CC(context.miscProcessPairs);
        }

        public void Clear()
        {
            miscProcessPairs.Clear();
        }

        public GraphicsContext graphicsContext;
    }
    public class MiscProcess
    {
        public ComputePO computePO = new ComputePO();
        GraphicsSignature rootSignature = new GraphicsSignature();
        public bool Ready = false;
        public ConstantBuffer constantBuffer1 = new ConstantBuffer();
        XYZData _XyzData;
        public byte[] cpuBuffer1 = new byte[512];
        public GCHandle handle1;
        public MiscProcess()
        {
            handle1 = GCHandle.Alloc(cpuBuffer1);
        }
        ~MiscProcess()
        {
            handle1.Free();
        }
        public async Task ReloadAssets(DeviceResources deviceResources)
        {
            rootSignature.ReloadCompute(deviceResources, new GraphicSignatureDesc[]
            {
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.SRVTable,
                GraphicSignatureDesc.UAVTable,
            });
            constantBuffer1.Reload(deviceResources, 512);
            computePO.Reload(deviceResources, rootSignature, await ReadAllBytes("ms-appx:///Coocoo3DGraphics/G_IrradianceMap0.cso"));
            float fovAngle = MathF.PI / 2;
            Matrix4x4 fov1 = Matrix4x4.CreatePerspectiveFieldOfView(fovAngle, 1, 0.01f, 1000);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY) * fov1, out var PX);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY) * fov1, out var NX);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ) * fov1, out var PY);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ) * fov1, out var NY);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY) * fov1, out var PZ);
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY) * fov1, out var NZ);
            _XyzData.PX = Matrix4x4.Transpose(PX);
            _XyzData.NX = Matrix4x4.Transpose(NX);
            _XyzData.PY = Matrix4x4.Transpose(PY);
            _XyzData.NY = Matrix4x4.Transpose(NY);
            _XyzData.PZ = Matrix4x4.Transpose(PZ);
            _XyzData.NZ = Matrix4x4.Transpose(NZ);

            Ready = true;
        }
        public void Process(MiscProcessContext context)
        {
            if (!Ready) return;
            for (int i = 0; i < context.miscProcessPairs.Count; i++)
            {
                if (context.miscProcessPairs[i].Type == MiscProcessType.GenerateIrradianceMap)
                {
                    var texture0 = context.miscProcessPairs[i].t0;
                    var texture1 = context.miscProcessPairs[i].t1;
                    _XyzData.x1 = (int)texture1.m_width;
                    _XyzData.y1 = (int)texture1.m_height;
                    IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(cpuBuffer1, 0);
                    Marshal.StructureToPtr(_XyzData, ptr1, true);
                    //context.graphicsContext.Copy(texture0, texture1);
                    context.graphicsContext.UpdateResource(constantBuffer1, cpuBuffer1, 512);
                    context.graphicsContext.SetRootSignatureCompute(rootSignature);
                    context.graphicsContext.SetPObject(computePO);
                    context.graphicsContext.SetComputeCBVR(constantBuffer1, 0);
                    context.graphicsContext.SetComputeSRVT(texture0, 1);
                    context.graphicsContext.SetComputeUAVT(texture1, 2);
                    context.graphicsContext.Dispatch((int)(texture1.m_width + 31) / 32, (int)(texture1.m_height + 31) / 32, 6);
                    //context.graphicsContext.Dispatch((int)(texture1.m_width + 31) / 32, (int)(texture1.m_height + 31) / 32, 6);
                    //context.graphicsContext.Copy(texture0, texture1);
                }
            }
            context.Clear();
        }

        public struct XYZData
        {
            public Matrix4x4 PX;
            public Matrix4x4 NX;
            public Matrix4x4 PY;
            public Matrix4x4 NY;
            public Matrix4x4 PZ;
            public Matrix4x4 NZ;
            public int x1;
            public int y1;
        }

        protected async Task<byte[]> ReadAllBytes(string uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            var stream = await file.OpenReadAsync();
            DataReader dataReader = new DataReader(stream);
            await dataReader.LoadAsync((uint)stream.Size);
            byte[] data = new byte[stream.Size];
            dataReader.ReadBytes(data);
            stream.Dispose();
            dataReader.Dispose();
            return data;
        }
    }
}
