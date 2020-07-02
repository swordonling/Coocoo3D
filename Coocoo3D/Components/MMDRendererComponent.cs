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
        public List<MMDMatLit.InnerStruct> materialsBaseData = new List<MMDMatLit.InnerStruct>();
        public List<MMDMatLit.InnerStruct> computedMaterialsData = new List<MMDMatLit.InnerStruct>();
        public List<Texture2D> texs;
        public PObject pObject;
        public ConstantBuffer EntityDataBuffer = new ConstantBuffer();
        byte[] rcDataUploadBuffer = new byte[c_transformMatrixDataSize + c_lightingDataSize + MMDMatLit.c_materialDataSize];
        GCHandle gch_rcDataUploadBuffer;
        public const int c_transformMatrixDataSize = 64;
        public const int c_lightingDataSize = 384;

        const int c_offsetTransformMatrixData = 0;
        const int c_offsetLightingData = c_transformMatrixDataSize;
        const int c_offsetMaterialData = c_transformMatrixDataSize + c_lightingDataSize;
        public Vector3[] meshPosDataUploadBuffer;
        public GCHandle gch_meshPosDataUploadBuffer;
        bool meshNeedUpdate;

        public List<MorphVertexStruct[]> vertexMorphCache;

        public MMDRendererComponent()
        {
            gch_rcDataUploadBuffer = GCHandle.Alloc(gch_rcDataUploadBuffer);
        }
        ~MMDRendererComponent()
        {
            if (gch_rcDataUploadBuffer.IsAllocated) gch_rcDataUploadBuffer.Free();
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
            ComputeMaterialMorph(morphStateComponent);
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

        private void ComputeMaterialMorph(MMDMorphStateComponent morphStateComponent)
        {
            for (int i = 0; i < computedMaterialsData.Count; i++)
            {
                computedMaterialsData[i] = materialsBaseData[i];
            }
            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == MorphType.Material && morphStateComponent.computedWeights[i] != morphStateComponent.prevComputedWeights[i])
                {
                    MorphMaterialStruct[] morphMaterialStructs = morphStateComponent.morphs[i].MorphMaterials;
                    float computedWeight = morphStateComponent.computedWeights[i];
                    for (int j = 0; j < morphMaterialStructs.Length; j++)
                    {
                        MorphMaterialStruct morphMaterialStruct = morphMaterialStructs[j];
                        int k = morphMaterialStruct.MaterialIndex;
                        MMDMatLit.InnerStruct struct1 = computedMaterialsData[k];
                        if (morphMaterialStruct.MorphMethon == MorphMaterialMorphMethon.Add)
                        {
                            struct1.AmbientColor += morphMaterialStruct.Ambient * computedWeight;
                            struct1.DiffuseColor += morphMaterialStruct.Diffuse * computedWeight;
                            struct1.EdgeColor += morphMaterialStruct.EdgeColor * computedWeight;
                            struct1.EdgeSize += morphMaterialStruct.EdgeSize * computedWeight;
                            struct1.SpecularColor += morphMaterialStruct.Specular * computedWeight;
                            struct1.SubTexture += morphMaterialStruct.SubTexture * computedWeight;
                            struct1.Texture += morphMaterialStruct.Texture * computedWeight;
                            struct1.ToonTexture += morphMaterialStruct.ToonTexture * computedWeight;
                        }
                        else if (morphMaterialStruct.MorphMethon == MorphMaterialMorphMethon.Mul)
                        {
                            struct1.AmbientColor = Vector3.Lerp(struct1.AmbientColor, struct1.AmbientColor * morphMaterialStruct.Ambient, computedWeight);
                            struct1.DiffuseColor = Vector4.Lerp(struct1.DiffuseColor, struct1.DiffuseColor * morphMaterialStruct.Diffuse, computedWeight);
                            struct1.EdgeColor = Vector4.Lerp(struct1.EdgeColor, struct1.EdgeColor * morphMaterialStruct.EdgeColor, computedWeight);
                            struct1.EdgeSize = struct1.EdgeSize * morphMaterialStruct.EdgeSize * computedWeight + struct1.EdgeSize * (1 - computedWeight);
                            struct1.SpecularColor = Vector4.Lerp(struct1.SpecularColor, struct1.SpecularColor * morphMaterialStruct.Specular, computedWeight);
                            struct1.SubTexture = Vector4.Lerp(struct1.SubTexture, struct1.SubTexture * morphMaterialStruct.SubTexture, computedWeight);
                            struct1.Texture = Vector4.Lerp(struct1.Texture, struct1.Texture * morphMaterialStruct.Texture, computedWeight);
                            struct1.ToonTexture = Vector4.Lerp(struct1.ToonTexture, struct1.ToonTexture * morphMaterialStruct.ToonTexture, computedWeight);
                        }

                        computedMaterialsData[k] = struct1;
                        Materials[k].innerStruct = struct1;
                    }
                }
            }
        }

        public void UpdateGPUResources(GraphicsContext graphicsContext, Matrix4x4 world, IList<Lighting> lightings)
        {
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);
            for (int j = 0; j < c_lightingDataSize; j += 4)
            {
                Marshal.WriteInt32(pBufferData + j, 0);
            }
            int mainLightIndex = -1;
            int lightCount = 0;
            for (int i = 0; i < lightings.Count; i++)
            {
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[i].rotateMatrix), pBufferData, true);
                    Marshal.StructureToPtr((uint)lightings[i].LightingType, pBufferData + 12, true);
                    Marshal.StructureToPtr(lightings[i].Color, pBufferData + 16, true);
                    Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[i].vpMatrix), pBufferData + 32, true);
                    mainLightIndex = i;
                    lightCount++;
                    pBufferData += 96;
                    break;
                }
            }
            for (int i = 0; i < lightings.Count; i++)
            {
                if (i != mainLightIndex)
                {
                    if (lightings[i].LightingType == LightingType.Directional)
                        Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[i].rotateMatrix), pBufferData, true);
                    else
                        Marshal.StructureToPtr(lightings[i].Rotation * 180 / MathF.PI, pBufferData, true);
                    Marshal.StructureToPtr((uint)lightings[i].LightingType, pBufferData + 12, true);
                    Marshal.StructureToPtr(lightings[i].Color, pBufferData + 16, true);
                    Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[i].vpMatrix), pBufferData + 32, true);
                    lightCount++;
                    pBufferData += 96;
                    if (lightCount >= 4)
                        break;
                }
            }

            pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetTransformMatrixData);
            Marshal.StructureToPtr(Matrix4x4.Transpose(world), pBufferData, true);

            graphicsContext.UpdateResource(EntityDataBuffer, rcDataUploadBuffer, c_transformMatrixDataSize + c_lightingDataSize, c_offsetTransformMatrixData);


            for (int i = 0; i < Materials.Count; i++)
            {
                IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetMaterialData);
                Marshal.StructureToPtr(Materials[i].innerStruct, ptr, true);
                graphicsContext.UpdateResource(Materials[i].matBuf, rcDataUploadBuffer, MMDMatLit.c_materialDataSize, c_offsetMaterialData);
            }

            if (meshNeedUpdate)
            {
                graphicsContext.UpdateVertices2(mesh, meshPosDataUploadBuffer);
                meshNeedUpdate = false;
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
    public class MMDMatLit
    {
        public const int c_materialDataSize = 256;

        public Texture2D tex;
        public string Name;
        public string NameEN;
        public int indexCount;
        public int texIndex;
        public int toonIndex;
        public DrawFlags DrawFlags;
        public Vector4 DiffuseColor { get => innerStruct.DiffuseColor; set => innerStruct.DiffuseColor = value; }
        public Vector4 SpecularColor { get => innerStruct.SpecularColor; set => innerStruct.SpecularColor = value; }
        public Vector3 AmbientColor { get => innerStruct.AmbientColor; set => innerStruct.AmbientColor = value; }
        public float EdgeScale { get => innerStruct.EdgeSize; set => innerStruct.EdgeSize = value; }
        public Vector4 EdgeColor { get => innerStruct.EdgeColor; set => innerStruct.EdgeColor = value; }
        public ConstantBuffer matBuf = new ConstantBuffer();

        public InnerStruct innerStruct;
        public struct InnerStruct
        {
            public Vector4 DiffuseColor;
            public Vector4 SpecularColor;
            public Vector3 AmbientColor;
            public float EdgeSize;
            public Vector4 EdgeColor;
            public uint notUse1;
            public uint notUse2;
            public uint notUse3;

            public Vector4 Texture;
            public Vector4 SubTexture;
            public Vector4 ToonTexture;
        }
    }
}
namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static void Reload(this MMDRendererComponent rendererComponent, DeviceResources deviceResources, MainCaches mainCaches, PMXFormat modelResource)
        {
            rendererComponent.ReloadBase();
            rendererComponent.EntityDataBuffer.Reload(deviceResources, MMDRendererComponent.c_transformMatrixDataSize + MMDRendererComponent.c_lightingDataSize);
            rendererComponent.mesh = modelResource.GetMesh();
            mainCaches.AddMeshToLoadList(rendererComponent.mesh);
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
                mat.matBuf.Reload(deviceResources, MMDMatLit.c_materialDataSize);
                rendererComponent.Materials.Add(mat);
                rendererComponent.materialsBaseData.Add(mat.innerStruct);
                rendererComponent.computedMaterialsData.Add(mat.innerStruct);
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
