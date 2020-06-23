using Coocoo3D.Components;
using Coocoo3D.Core;
using Coocoo3D.FileFormat;
using Coocoo3D.MMDSupport;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Components
{
    public class MMDRendererComponent
    {
        public MMDMesh mesh;
        public List<MMDMatLit> Materials = new List<MMDMatLit>();
        public List<Texture2D> texs;
        public PObject pObject;
        public ConstantBuffer lightingDataBuffer = new ConstantBuffer();
        public ConstantBuffer transformDataBuffer = new ConstantBuffer();
        byte[] DataUploadBuffer = new byte[c_transformMatrixDataSize * 2 + c_lightingDataSize + MMDMatLit.c_materialDataSize];
        GCHandle gch_DataUploadBuffer;
        public const int c_transformMatrixDataSize = 64;
        public const int c_lightingDataSize = 384;

        const int c_offsetTransformMatrixData = 0;
        const int c_offsetLightingData = c_transformMatrixDataSize * 2;
        const int c_offsetMaterialData = c_transformMatrixDataSize * 2 + c_lightingDataSize;
        public Vector3[] meshPosDataUploadBuffer;
        public GCHandle gch_meshPosDataUploadBuffer;
        bool meshNeedUpdate;

        public List<MorphVertexStruct[]> vertexMorphCache;

        public MMDRendererComponent()
        {
            gch_DataUploadBuffer = GCHandle.Alloc(gch_DataUploadBuffer);
        }
        ~MMDRendererComponent()
        {
            if (gch_DataUploadBuffer.IsAllocated) gch_DataUploadBuffer.Free();
            if (gch_meshPosDataUploadBuffer.IsAllocated) gch_meshPosDataUploadBuffer.Free();
        }

        //重新加载不依赖其他实例的资源，仅用于简化代码。
        public void ReloadBase()
        {
            Materials.Clear();
        }

        public void SetPose(MMDMorphStateComponent morphStateComponent)
        {
            ComputeVertexMorph(morphStateComponent);
        }

        private void ComputeVertexMorph(MMDMorphStateComponent morphStateComponent)
        {
            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == MorphType.Vertex && morphStateComponent.computedWeights[i] != morphStateComponent.prevComputedWeights[i])
                {
                    MorphVertexStruct[] morphVertexStructs = vertexMorphCache[i];
                    MorphVertexStruct[] morphVertexStructs2 = morphStateComponent.morphs[i].MorphVertexs;
                    float computedWeight = morphStateComponent.computedWeights[i];
                    for (int j = 0; j < morphVertexStructs.Length; j++)
                    {
                        morphVertexStructs[j].Offset = morphVertexStructs2[j].Offset * computedWeight;
                    }
                    meshNeedUpdate = true;
                }
            }
            if (!meshNeedUpdate) return;
            IntPtr vptr = Marshal.UnsafeAddrOfPinnedArrayElement(meshPosDataUploadBuffer, 0);
            Marshal.Copy(mesh.m_verticeData2, 0, vptr, mesh.m_verticeData2.Length);

            for (int i = 0; i < vertexMorphCache.Count; i++)
            {
                if (vertexMorphCache[i] == null) continue;
                MorphVertexStruct[] morphVertexStructs = vertexMorphCache[i];
                for (int j = 0; j < morphVertexStructs.Length; j++)
                {
                    MorphVertexStruct morphVertexStruct = morphVertexStructs[j];
                    meshPosDataUploadBuffer[morphVertexStruct.VertexIndex] += morphVertexStruct.Offset;
                }
            }
        }

        public void UpdateGPUResources(GraphicsContext graphicsContext, Matrix4x4 world, IList<Lighting> lightings)
        {
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(DataUploadBuffer, c_offsetLightingData);
            for (int j = 0; j < c_lightingDataSize; j += 4)
            {
                Marshal.WriteInt32(pBufferData + j, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                if (i < lightings.Count)
                {
                    Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[i].rotateMatrix), pBufferData, true);
                    Marshal.StructureToPtr(lightings[i].Color, pBufferData + 16, true);
                    Marshal.StructureToPtr(Matrix4x4.Transpose( lightings[i].vpMatrix), pBufferData + 32, true);
                }
                pBufferData += 96;
            }
            graphicsContext.UpdateResource(lightingDataBuffer, DataUploadBuffer, c_offsetLightingData);


            pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(DataUploadBuffer, c_offsetTransformMatrixData);
            Marshal.StructureToPtr(Matrix4x4.Transpose(world), pBufferData, true);

            if (!MemEqual(DataUploadBuffer, 0, DataUploadBuffer, c_transformMatrixDataSize, c_transformMatrixDataSize))
            {
                graphicsContext.UpdateResource(transformDataBuffer, DataUploadBuffer, c_offsetTransformMatrixData);
                Array.Copy(DataUploadBuffer, 0, DataUploadBuffer, c_transformMatrixDataSize, c_transformMatrixDataSize);
            }
            if (meshNeedUpdate)
            {
                graphicsContext.UpdateVertices2(mesh, meshPosDataUploadBuffer);
                meshNeedUpdate = false;
            }
            for (int i = 0; i < Materials.Count; i++)
            {
                if (!Materials[i].GpuUsable)
                {
                    IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(DataUploadBuffer, c_offsetMaterialData);
                    Marshal.StructureToPtr(Materials[i].innerStruct, ptr, true);
                    graphicsContext.UpdateResource(Materials[i].matBuf, DataUploadBuffer, c_offsetMaterialData);
                    Materials[i].GpuUsable = true;
                }
            }
        }


        public void RenderDepth(GraphicsContext graphicsContext, MMDBoneComponent boneComponent, PresentData presentData)
        {
            graphicsContext.VSSetConstantBuffer(boneComponent.boneMatrices, 1);
            graphicsContext.VSSetConstantBuffer(transformDataBuffer, 0);
            graphicsContext.GSSetConstantBuffer(transformDataBuffer, 0);
            graphicsContext.VSSetConstantBuffer(presentData.DataBuffer, 3);
            graphicsContext.GSSetConstantBuffer(presentData.DataBuffer, 3);
            graphicsContext.SetMesh(mesh);
            graphicsContext.SetPObjectDepthOnly(pObject);

            int indexStartLocation = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(DrawFlags.DrawDoubleFace))
                    graphicsContext.SetCullMode(CullMode.none);
                else
                    graphicsContext.SetCullMode(CullMode.back);

                graphicsContext.DrawIndexed(Materials[i].indexCount, indexStartLocation, 0);
                indexStartLocation += Materials[i].indexCount;
            }
        }

        public void Render(GraphicsContext graphicsContext, DefaultResources defaultResources, MMDBoneComponent boneComponent, PresentData presentData)
        {
            graphicsContext.VSSetConstantBuffer(boneComponent.boneMatrices, 1);
            graphicsContext.PSSetConstantBuffer(lightingDataBuffer, 1);
            graphicsContext.VSSetConstantBuffer(transformDataBuffer, 0);
            graphicsContext.GSSetConstantBuffer(transformDataBuffer, 0);
            graphicsContext.PSSetConstantBuffer(transformDataBuffer, 0);
            graphicsContext.VSSetConstantBuffer(presentData.DataBuffer, 3);
            graphicsContext.GSSetConstantBuffer(presentData.DataBuffer, 3);
            graphicsContext.PSSetConstantBuffer(presentData.DataBuffer, 3);
            graphicsContext.SetBlendState(defaultResources.BlendStateAlpha);
            graphicsContext.SetMesh(mesh);
            graphicsContext.SetPObject(pObject);

            int indexStartLocation = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(DrawFlags.DrawDoubleFace))
                    graphicsContext.SetCullMode(CullMode.none);
                else
                    graphicsContext.SetCullMode(CullMode.back);
                if (texs != null)
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    if (tex1 != null)
                        graphicsContext.PSSetSRV(tex1, 0);
                    else
                        graphicsContext.PSSetSRV(defaultResources.TextureError, 0);
                    if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    {
                        Texture2D tex2 = texs[Materials[i].toonIndex];
                        if (tex2 != null)
                            graphicsContext.PSSetSRV(tex2, 1);
                        else
                            graphicsContext.PSSetSRV(defaultResources.TextureError, 1);
                    }
                    else
                        graphicsContext.PSSetSRV(defaultResources.TextureError, 1);
                }
                else
                {
                    graphicsContext.PSSetSRV(defaultResources.TextureError, 1);
                    graphicsContext.PSSetSRV(defaultResources.TextureError, 0);
                }
                graphicsContext.PSSetConstantBuffer(Materials[i].matBuf, 2);
                graphicsContext.DrawIndexed(Materials[i].indexCount, indexStartLocation, 0);
                indexStartLocation += Materials[i].indexCount;
            }
        }

        bool MemEqual(byte[] a, int aIndex, byte[] b, int bIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a[i + aIndex] != b[i + bIndex])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static MMDRendererComponent LoadRendererComponent(DeviceResources deviceResources, PMXFormat modelResource)
        {
            MMDRendererComponent rendererComponent = new MMDRendererComponent();
            rendererComponent.Reload(deviceResources, modelResource);
            return rendererComponent;
        }

        public static void Reload(this MMDRendererComponent rendererComponent, DeviceResources deviceResources, PMXFormat modelResource)
        {
            rendererComponent.ReloadBase();
            rendererComponent.lightingDataBuffer.Reload(deviceResources, MMDRendererComponent.c_lightingDataSize);
            rendererComponent.transformDataBuffer.Reload(deviceResources, MMDRendererComponent.c_transformMatrixDataSize);
            rendererComponent.mesh = modelResource.GetMesh(deviceResources);
            rendererComponent.meshPosDataUploadBuffer = new Vector3[rendererComponent.mesh.m_vertexCount];
            rendererComponent.gch_meshPosDataUploadBuffer = GCHandle.Alloc(rendererComponent.meshPosDataUploadBuffer);

            for (int i = 0; i < modelResource.Materials.Count; i++)
            {
                var mmdMat = modelResource.Materials[i];

                MMDMatLit mat = new MMDMatLit
                {
                    Name = mmdMat.Name,
                    NameEN = mmdMat.NameEN,
                    texIndex = mmdMat.TextureIndex,
                    indexCount = mmdMat.TriangeIndexNum,
                    DiffuseColor = mmdMat.DiffuseColor,
                    SpecularColor = mmdMat.SpecularColor,
                    EdgeScale = mmdMat.EdgeScale,
                    EdgeColor = mmdMat.EdgeColor,
                    DrawFlags = mmdMat.DrawFlags,
                    toonIndex = mmdMat.ToonIndex,
                };

                mat.AmbientColor = new Vector3(MathF.Pow(mmdMat.AmbientColor.X, 2.2f), MathF.Pow(mmdMat.AmbientColor.Y, 2.2f), MathF.Pow(mmdMat.AmbientColor.Z, 2.2f));
                mat.matBuf = ConstantBuffer.Load(deviceResources, MMDMatLit.c_materialDataSize);
                rendererComponent.Materials.Add(mat);
            }

            int morphCount = modelResource.Morphs.Count;
            rendererComponent.vertexMorphCache = new List<MorphVertexStruct[]>();
            for (int i = 0; i < morphCount; i++)
            {
                if (modelResource.Morphs[i].Type == MorphType.Vertex)
                {
                    MorphVertexStruct[] morphVertexStructs = new MorphVertexStruct[modelResource.Morphs[i].MorphVertexs.Length];
                    MorphVertexStruct[] morphVertexStructs2 = modelResource.Morphs[i].MorphVertexs;
                    for (int j = 0; j < morphVertexStructs.Length; j++)
                    {
                        morphVertexStructs[j].VertexIndex = morphVertexStructs2[j].VertexIndex;
                    }
                    rendererComponent.vertexMorphCache.Add(morphVertexStructs);
                }
                else
                {
                    rendererComponent.vertexMorphCache.Add(null);
                }
            }
        }
    }
}
