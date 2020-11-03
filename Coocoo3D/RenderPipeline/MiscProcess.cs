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

    public struct P_Env_Data
    {
        public TextureCube source;
        public RenderTextureCube IrradianceMap;
        public RenderTextureCube EnvMap;
        public int Level;
    }
    public class MiscProcessContext
    {
        public List<P_Env_Data> miscProcessPairs = new List<P_Env_Data>();
        public void Add(P_Env_Data pair)
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
        const int c_bufferSize = 256;
        public ComputePO IrradianceMap0 = new ComputePO();
        public ComputePO EnvironmentMap0 = new ComputePO();
        public ComputePO ClearIrradianceMap = new ComputePO();
        GraphicsSignature rootSignature = new GraphicsSignature();
        public bool Ready = false;
        public const int c_maxIteration = 32;
        public ConstantBuffer[] constantBuffers = new ConstantBuffer[c_maxIteration];
        XYZData _XyzData;
        public byte[] cpuBuffer1 = new byte[c_bufferSize];
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
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.SRVTable,
                GraphicSignatureDesc.UAVTable,
            });
            for (int i = 0; i < constantBuffers.Length; i++)
            {
                if (constantBuffers[i] == null) constantBuffers[i] = new ConstantBuffer();
                constantBuffers[i].Reload(deviceResources, c_bufferSize);
            }
            IrradianceMap0.Reload(deviceResources, rootSignature, await ReadFile("ms-appx:///Coocoo3DGraphics/G_IrradianceMap0.cso"));
            EnvironmentMap0.Reload(deviceResources, rootSignature, await ReadFile("ms-appx:///Coocoo3DGraphics/G_PreFilterEnv.cso"));
            ClearIrradianceMap.Reload(deviceResources, rootSignature, await ReadFile("ms-appx:///Coocoo3DGraphics/G_ClearIrradianceMap.cso"));

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
                var texture0 = context.miscProcessPairs[i].source;
                var texture1 = context.miscProcessPairs[i].IrradianceMap;
                var texture2 = context.miscProcessPairs[i].EnvMap;
                IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(cpuBuffer1, 0);
                _XyzData.x1 = (int)texture1.m_width;
                _XyzData.y1 = (int)texture1.m_height;
                _XyzData.x2 = (int)texture2.m_width;
                _XyzData.y2 = (int)texture2.m_height;
                _XyzData.Quality = context.miscProcessPairs[i].Level;
                int itCount = context.miscProcessPairs[i].Level;

                for (int j = 0; j < itCount; j++)
                {
                    _XyzData.Batch = j;

                    Marshal.StructureToPtr(_XyzData, ptr1, true);
                    context.graphicsContext.UpdateResource(constantBuffers[j], cpuBuffer1, c_bufferSize, 0);
                }

                context.graphicsContext.SetRootSignatureCompute(rootSignature);
                context.graphicsContext.SetComputeCBVR(constantBuffers[0], 0);
                context.graphicsContext.SetComputeSRVT(texture0, 2);
                context.graphicsContext.SetPObject(ClearIrradianceMap);

                int pow2a = 1;
                for (int j = 0; j < texture1.m_mipLevels; j++)
                {
                    context.graphicsContext.SetComputeUAVT(texture1, j, 3);
                    context.graphicsContext.Dispatch((int)(texture1.m_width + 7) / 8 / pow2a, (int)(texture1.m_height + 7) / 8 / pow2a, 6);
                    pow2a *= 2;
                }
                pow2a = 1;
                for (int j = 0; j < texture2.m_mipLevels; j++)
                {
                    context.graphicsContext.SetComputeUAVT(texture2, j, 3);
                    context.graphicsContext.Dispatch((int)(texture2.m_width + 7) / 8 / pow2a, (int)(texture2.m_height + 7) / 8 / pow2a, 6);
                    pow2a *= 2;
                }
                context.graphicsContext.SetPObject(IrradianceMap0);

                pow2a = 1;
                for (int j = 0; j < texture1.m_mipLevels; j++)
                {
                    for (int k = 0; k < itCount; k++)
                    {
                        context.graphicsContext.SetComputeUAVT(texture1, j, 3);
                        context.graphicsContext.SetComputeCBVR(constantBuffers[k], 0);
                        context.graphicsContext.Dispatch((int)(texture1.m_width + 7) / 8 / pow2a, (int)(texture1.m_height + 7) / 8 / pow2a, 6);
                    }
                    pow2a *= 2;
                }

                context.graphicsContext.SetComputeSRVT(texture0, 2);
                context.graphicsContext.SetPObject(EnvironmentMap0);
                pow2a = 1;
                for (int j = 0; j < texture2.m_mipLevels; j++)
                {
                    for (int k = 0; k < itCount; k++)
                    {
                        context.graphicsContext.SetComputeUAVT(texture2, j, 3);
                        context.graphicsContext.SetComputeCBVR(constantBuffers[k], 0);
                        context.graphicsContext.SetComputeCBVR(constantBuffers[j], 1);
                        context.graphicsContext.Dispatch((int)(texture2.m_width + 7) / 8 / pow2a, (int)(texture2.m_height + 7) / 8 / pow2a, 6);
                    }
                    pow2a *= 2;
                }
            }
            context.graphicsContext.EndCommand();
            context.graphicsContext.Execute();
            context.Clear();
        }

        public struct XYZData
        {
            public int x1;
            public int y1;
            public int Quality;
            public int Batch;
            public int x2;
            public int y2;
        }

        protected async Task<IBuffer> ReadFile(string uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            return await FileIO.ReadBufferAsync(file);
        }
    }
}
