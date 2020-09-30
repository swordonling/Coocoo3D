using Coocoo3D.Components;
using Coocoo3D.Core;
using Coocoo3D.FileFormat;
using Coocoo3D.MMDSupport;
using Coocoo3D.Present;
using Coocoo3D.RenderPipeline;
using Coocoo3DGraphics;
using Coocoo3DNativeInteroperable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Components
{
    public struct RPGPUAssets
    {
    }
    public class MMDRendererComponent
    {
        public MMDMesh mesh;
        public TwinBuffer meshParticleBuffer = new TwinBuffer();
        public DynamicMesh dynamicMesh = new DynamicMesh();
        public List<MMDMatLit> Materials = new List<MMDMatLit>();
        public List<MMDMatLit.InnerStruct> materialsBaseData = new List<MMDMatLit.InnerStruct>();
        public List<MMDMatLit.InnerStruct> computedMaterialsData = new List<MMDMatLit.InnerStruct>();
        public List<Texture2D> textures = new List<Texture2D>();

        public PObject POSkinning;
        public PObject PODraw;
        public PObject POParticleDraw;
        public ComputePO ParticleCompute;

        public Vector3[] meshPosData1;
        public Vector3[] meshPosData2;
        public GCHandle gch_meshPosData1;
        public GCHandle gch_meshPosData2;
        bool meshNeedUpdateA;
        bool meshNeedUpdateB;
        bool flip;
        bool prevflip;
        public float amountAB;

        public List<NMMD_MorphVertexDesc[]> vertexMorph1;
        public List<NMMD_MorphVertexDesc[]> vertexMorph2;

        public MMDRendererComponent()
        {
        }
        ~MMDRendererComponent()
        {
            if (gch_meshPosData1.IsAllocated) gch_meshPosData1.Free();
            if (gch_meshPosData2.IsAllocated) gch_meshPosData2.Free();
        }


        public void SetPose(MMDMorphStateComponent morphStateComponent)
        {
            ComputeVertexMorph(morphStateComponent);
            ComputeMaterialMorph(morphStateComponent);
        }

        public void ComputeVertexMorph(MMDMorphStateComponent morphStateComponent)
        {
            prevflip = flip;
            flip = ((int)(morphStateComponent.currentTimeA / MMDMorphStateComponent.c_frameInterval) & 1) == 1;
            amountAB = flip ? (1 - morphStateComponent.amountAB) : morphStateComponent.amountAB;
            if (prevflip != flip) morphStateComponent.FlipAB();
            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == NMMDE_MorphType.Vertex)
                {
                    NMMD_MorphVertexDesc[] morphVertexStructs2 = morphStateComponent.morphs[i].MorphVertexs;

                    NMMD_MorphVertexDesc[] morphVertexStructsA = vertexMorph1[i];
                    NMMD_MorphVertexDesc[] morphVertexStructsB = vertexMorph2[i];
                    if (!flip)
                    {
                        if (morphStateComponent.ComputedWeightNotEqualsPrevA(i, out float computedWeightA))
                        {
                            for (int j = 0; j < morphVertexStructsA.Length; j++)
                                morphVertexStructsA[j].Offset = morphVertexStructs2[j].Offset * computedWeightA;
                            meshNeedUpdateA = true;
                        }
                        if (morphStateComponent.ComputedWeightNotEqualsPrevB(i, out float computedWeightB))
                        {
                            for (int j = 0; j < morphVertexStructsB.Length; j++)
                                morphVertexStructsB[j].Offset = morphVertexStructs2[j].Offset * computedWeightB;
                            meshNeedUpdateB = true;
                        }
                    }
                    else
                    {
                        if (morphStateComponent.ComputedWeightNotEqualsPrevA(i, out float computedWeightA))
                        {
                            for (int j = 0; j < morphVertexStructsB.Length; j++)
                                morphVertexStructsB[j].Offset = morphVertexStructs2[j].Offset * computedWeightA;
                            meshNeedUpdateB = true;
                        }
                        if (morphStateComponent.ComputedWeightNotEqualsPrevB(i, out float computedWeightB))
                        {
                            for (int j = 0; j < morphVertexStructsA.Length; j++)
                                morphVertexStructsA[j].Offset = morphVertexStructs2[j].Offset * computedWeightB;
                            meshNeedUpdateA = true;
                        }
                    }
                }
            }
            if (meshNeedUpdateA)
            {
                mesh.CopyPosData(meshPosData1);
                ComputeMorphVertex(meshPosData1, vertexMorph1);
            }
            if (meshNeedUpdateB)
            {
                mesh.CopyPosData(meshPosData2);
                ComputeMorphVertex(meshPosData2, vertexMorph2);
            }
        }

        private static void ComputeMorphVertex(Vector3[] vector3s, List<NMMD_MorphVertexDesc[]> morphX)
        {
            for (int i = 0; i < morphX.Count; i++)
            {
                if (morphX[i] == null) continue;
                NMMD_MorphVertexDesc[] morphVertexStructs = morphX[i];
                for (int j = 0; j < morphVertexStructs.Length; j++)
                {
                    vector3s[morphVertexStructs[j].VertexIndex] += morphVertexStructs[j].Offset;
                }
            }
        }

        public void UpdateGPUResources(GraphicsContext graphicsContext)
        {
            if (meshNeedUpdateA)
            {
                graphicsContext.UpdateVerticesPos0(mesh, meshPosData1);
                meshNeedUpdateA = false;
            }
            if (meshNeedUpdateB)
            {
                graphicsContext.UpdateVerticesPos1(mesh, meshPosData2);
                meshNeedUpdateB = false;
            }
        }

        public void ComputeMaterialMorph(MMDMorphStateComponent morphStateComponent)
        {
            for (int i = 0; i < computedMaterialsData.Count; i++)
            {
                computedMaterialsData[i] = materialsBaseData[i];
            }
            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == NMMDE_MorphType.Material && morphStateComponent.computedWeights[i] != morphStateComponent.prevComputedWeights[i])
                {
                    MorphMaterialStruct[] morphMaterialStructs = morphStateComponent.morphs[i].MorphMaterials;
                    float computedWeight = morphStateComponent.computedWeights[i];
                    for (int j = 0; j < morphMaterialStructs.Length; j++)
                    {
                        MorphMaterialStruct morphMaterialStruct = morphMaterialStructs[j];
                        int k = morphMaterialStruct.MaterialIndex;
                        MMDMatLit.InnerStruct struct1 = computedMaterialsData[k];
                        if (morphMaterialStruct.MorphMethon == NMMDE_MorphMaterialMethon.Add)
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
                        else if (morphMaterialStruct.MorphMethon == NMMDE_MorphMaterialMethon.Mul)
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
        public NMMDE_DrawFlag DrawFlags;

        public InnerStruct innerStruct;
        public struct InnerStruct
        {
            public Vector4 DiffuseColor;
            public Vector4 SpecularColor;
            public Vector3 AmbientColor;
            public float EdgeSize;
            public Vector4 EdgeColor;

            public Vector4 Texture;
            public Vector4 SubTexture;
            public Vector4 ToonTexture;
            public uint IsTransparent;
            public float Metallic;
            public float Roughness;
            public float Emission;
            public float Subsurface;
            public float Specular;
            public float SpecularTint;
            public float Anisotropic;
            public float Sheen;
            public float SheenTint;
            public float Clearcoat;
            public float ClearcoatGloss;
        }
        public override string ToString()
        {
            return string.Format("{0}_{1}", Name, NameEN);
        }
    }
}
namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static void Reload(this MMDRendererComponent rendererComponent, ProcessingList processingList, PMXFormat modelResource)
        {
            rendererComponent.Materials.Clear();
            rendererComponent.mesh = modelResource.GetMesh();
            processingList.AddObject(rendererComponent.mesh);
            rendererComponent.meshParticleBuffer.Reload(rendererComponent.mesh.m_indexCount / 3 * 128);
            rendererComponent.dynamicMesh.Reload(rendererComponent.mesh.m_indexCount * 64, 64);
            processingList.AddObject(rendererComponent.meshParticleBuffer);
            processingList.AddObject(rendererComponent.dynamicMesh);
            rendererComponent.POSkinning = new PObject();
            rendererComponent.PODraw = new PObject();
            rendererComponent.ParticleCompute = null;
            rendererComponent.POParticleDraw = null;
            rendererComponent.meshPosData1 = new Vector3[rendererComponent.mesh.m_vertexCount];
            rendererComponent.meshPosData2 = new Vector3[rendererComponent.mesh.m_vertexCount];
            rendererComponent.gch_meshPosData1 = GCHandle.Alloc(rendererComponent.meshPosData1);
            rendererComponent.gch_meshPosData2 = GCHandle.Alloc(rendererComponent.meshPosData2);

            for (int i = 0; i < modelResource.Materials.Count; i++)
            {
                var mmdMat = modelResource.Materials[i];

                MMDMatLit mat = new MMDMatLit
                {
                    Name = mmdMat.Name,
                    NameEN = mmdMat.NameEN,
                    texIndex = mmdMat.TextureIndex,
                    indexCount = mmdMat.TriangeIndexNum,
                    innerStruct =
                    {
                        DiffuseColor = mmdMat.DiffuseColor,
                        SpecularColor = mmdMat.SpecularColor,
                        EdgeSize = mmdMat.EdgeScale,
                        EdgeColor = mmdMat.EdgeColor,
                        AmbientColor = new Vector3(MathF.Pow(mmdMat.AmbientColor.X, 2.2f), MathF.Pow(mmdMat.AmbientColor.Y, 2.2f), MathF.Pow(mmdMat.AmbientColor.Z, 2.2f)),
                        Roughness = 0.5f,
                    },
                    DrawFlags = mmdMat.DrawFlags,
                    toonIndex = mmdMat.ToonIndex,
                };
                rendererComponent.Materials.Add(mat);
                rendererComponent.materialsBaseData.Add(mat.innerStruct);
                rendererComponent.computedMaterialsData.Add(mat.innerStruct);
            }

            int morphCount = modelResource.Morphs.Count;
            rendererComponent.vertexMorph1 = new List<NMMD_MorphVertexDesc[]>();
            rendererComponent.vertexMorph2 = new List<NMMD_MorphVertexDesc[]>();
            for (int i = 0; i < morphCount; i++)
            {
                if (modelResource.Morphs[i].Type == NMMDE_MorphType.Vertex)
                {
                    NMMD_MorphVertexDesc[] morphVertexStructs = new NMMD_MorphVertexDesc[modelResource.Morphs[i].MorphVertexs.Length];
                    NMMD_MorphVertexDesc[] morphVertexStructs2 = modelResource.Morphs[i].MorphVertexs;
                    for (int j = 0; j < morphVertexStructs.Length; j++)
                    {
                        morphVertexStructs[j].VertexIndex = morphVertexStructs2[j].VertexIndex;
                    }
                    rendererComponent.vertexMorph1.Add(morphVertexStructs);
                    rendererComponent.vertexMorph2.Add(morphVertexStructs);
                }
                else
                {
                    rendererComponent.vertexMorph1.Add(null);
                    rendererComponent.vertexMorph2.Add(null);
                }
            }
        }
    }
}
