﻿using System;
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
        GenerateIrradianceMap = 65536,
        GenerateIrradianceMapQ1 = 65537,
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
        public ComputePO IrradianceMap0 = new ComputePO();
        public ComputePO ClearIrradianceMap = new ComputePO();
        GraphicsSignature rootSignature = new GraphicsSignature();
        public bool Ready = false;
        public const int c_maxIteration = 16;
        public ConstantBuffer[] constantBuffers = new ConstantBuffer[c_maxIteration];
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
            for (int i = 0; i < constantBuffers.Length; i++)
            {
                if (constantBuffers[i] == null) constantBuffers[i] = new ConstantBuffer();
                constantBuffers[i].Reload(deviceResources, 512);
            }
            IrradianceMap0.Reload(deviceResources, rootSignature, await ReadAllBytes("ms-appx:///Coocoo3DGraphics/G_IrradianceMap0.cso"));
            ClearIrradianceMap.Reload(deviceResources, rootSignature, await ReadAllBytes("ms-appx:///Coocoo3DGraphics/G_ClearIrradianceMap.cso"));
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
            if (context.miscProcessPairs.Count == 0) return;
            context.graphicsContext.BeginCommand();
            context.graphicsContext.SetDescriptorHeapDefault();
            for (int i = 0; i < context.miscProcessPairs.Count; i++)
            {
                if (context.miscProcessPairs[i].Type.HasFlag(MiscProcessType.GenerateIrradianceMap))
                {
                    var texture0 = context.miscProcessPairs[i].t0;
                    var texture1 = context.miscProcessPairs[i].t1;
                    IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(cpuBuffer1, 0);
                    void UpdateGPUBuffer(int bufIndex)
                    {
                        Marshal.StructureToPtr(_XyzData, ptr1, true);
                        context.graphicsContext.UpdateResource(constantBuffers[bufIndex], cpuBuffer1, 512);
                    }
                    _XyzData.x1 = (int)texture1.m_width;
                    _XyzData.y1 = (int)texture1.m_height;
                    _XyzData.Quality = ((int)context.miscProcessPairs[i].Type - 65536) * (c_maxIteration - 1);
                    _XyzData.Batch = 0;
                    UpdateGPUBuffer(0);
                    if (context.miscProcessPairs[i].Type == MiscProcessType.GenerateIrradianceMapQ1)
                    {
                        for (int j = 1; j < c_maxIteration; j++)
                        {
                            _XyzData.Batch = j;
                            UpdateGPUBuffer(j);
                        }
                    }

                    context.graphicsContext.SetRootSignatureCompute(rootSignature);
                    context.graphicsContext.SetComputeCBVR(constantBuffers[0], 0);
                    context.graphicsContext.SetComputeSRVT(texture0, 1);
                    context.graphicsContext.SetComputeUAVT(texture1, 2);
                    context.graphicsContext.SetPObject(ClearIrradianceMap);
                    context.graphicsContext.Dispatch((int)(texture1.m_width + 7) / 8, (int)(texture1.m_height + 7) / 8, 6);

                    context.graphicsContext.SetPObject(IrradianceMap0);
                    if (context.miscProcessPairs[i].Type == MiscProcessType.GenerateIrradianceMapQ1)
                    {
                        for (int j = 0; j < c_maxIteration; j++)
                        {
                            context.graphicsContext.SetComputeUAVT(texture1, 2);
                            context.graphicsContext.SetComputeCBVR(constantBuffers[j], 0);
                            context.graphicsContext.Dispatch((int)(texture1.m_width + 7) / 8, (int)(texture1.m_height + 7) / 8, 6);
                        }
                    }
                    else
                    {
                        context.graphicsContext.SetComputeUAVT(texture1, 2);
                        context.graphicsContext.Dispatch((int)(texture1.m_width + 7) / 8, (int)(texture1.m_height + 7) / 8, 6);
                    }
                    //context.graphicsContext.Dispatch((int)(texture1.m_width + 31) / 32, (int)(texture1.m_height + 31) / 32, 6);
                    //context.graphicsContext.Copy(texture0, texture1);
                }
            }
            context.graphicsContext.EndCommand();
            context.graphicsContext.Execute();
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
            public int Quality;
            public int Batch;
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
