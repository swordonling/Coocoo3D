﻿using Coocoo3D.Components;
using Coocoo3D.MMDSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Components
{
    public class MMDMorphStateComponent
    {
        public List<MorphDesc> morphs = new List<MorphDesc>();
        public float[] WeightInit;
        public float[] WeightInitA;
        public float[] WeightInitB;
        public float[] computedWeights;
        public float[] prevComputedWeights;
        public float[] CWFrameA;
        public float[] CWFrameB;
        public float[] PrevCWFrameA;
        public float[] PrevCWFrameB;
        public float currentTimeA;
        public float currentTimeB;
        public float amountAB;

        public const float c_frameInterval = 1 / 30.0f;
        public Dictionary<string, int> stringMorphIndexMap = new Dictionary<string, int>();
        public void SetPose(MMDMotionComponent motionComponent, float time)
        {
            currentTimeA = MathF.Floor(time / c_frameInterval) * c_frameInterval;
            currentTimeB = currentTimeA + c_frameInterval;
            foreach (var pair in stringMorphIndexMap)
            {
                //var keyframe = motionComponent.GetMorphMotion(pair.Key, time);
                //weightInit[pair.Value]= keyframe.Weight;

                WeightInit[pair.Value] = motionComponent.GetMorphWeight(pair.Key, time);
                motionComponent.GetABWeight(pair.Key, time, out WeightInitA[pair.Value], out WeightInitB[pair.Value]);
            }
            amountAB = Math.Max((time - currentTimeA) / c_frameInterval, 0);
        }
        public void ComputeWeight()
        {
            ComputeWeight1(morphs, WeightInit, computedWeights, prevComputedWeights);
            ComputeWeight1(morphs, WeightInitA, CWFrameA, PrevCWFrameA);
            ComputeWeight1(morphs, WeightInitB, CWFrameB, PrevCWFrameB);
        }
        private static void ComputeWeight1(IReadOnlyList<MorphDesc> morphs, float[] weightInit, float[] computedWeights, float[] prevComputedWeights)
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                prevComputedWeights[i] = computedWeights[i];
                computedWeights[i] = 0;
            }
            for (int i = 0; i < morphs.Count; i++)
            {
                MorphDesc morph = morphs[i];
                if (morph.Type == MorphType.Group)
                    ComputeWeightGroup(morphs, morph, weightInit[i], computedWeights);
                else
                    computedWeights[i] += weightInit[i];
            }
        }
        private static void ComputeWeightGroup(IReadOnlyList<MorphDesc> morphs, MorphDesc morph, float rate, float[] computedWeights)
        {
            for (int i = 0; i < morph.SubMorphs.Length; i++)
            {
                MorphSubMorphDesc subMorphStruct = morph.SubMorphs[i];
                MorphDesc subMorph = morphs[subMorphStruct.GroupIndex];
                if (subMorph.Type == MorphType.Group)
                    ComputeWeightGroup(morphs, subMorph, rate * subMorphStruct.Rate, computedWeights);
                else
                    computedWeights[subMorphStruct.GroupIndex] += rate * subMorphStruct.Rate;
            }
        }

        public bool ComputedWeightNotEqualsPrev(int index, out float weight)
        {
            weight = computedWeights[index];
            return computedWeights[index] != prevComputedWeights[index];
        }
        public void FlipAB()
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                if (morphs[i].Type == MorphType.Vertex)
                {
                    float a = PrevCWFrameA[i];
                    PrevCWFrameA[i] = PrevCWFrameB[i];
                    PrevCWFrameB[i] = a;
                }
            }
        }
        public bool ComputedWeightNotEqualsPrevA(int index, out float weight)
        {
            weight = CWFrameA[index];
            return CWFrameA[index] != PrevCWFrameA[index];
        }
        public bool ComputedWeightNotEqualsPrevB(int index, out float weight)
        {
            weight = CWFrameB[index];
            return CWFrameB[index] != PrevCWFrameB[index];
        }
    }

    public enum MorphCategory
    {
        System = 0,
        Eyebrow = 1,
        Eye = 2,
        Mouth = 3,
        Other = 4,
    };
    public enum MorphMaterialMethon
    {
        Mul = 0,
        Add = 1,
    };

    public struct MorphSubMorphDesc
    {
        public int GroupIndex;
        public float Rate;
    }
    public struct MorphMaterialDesc
    {
        public int MaterialIndex;
        public MorphMaterialMethon MorphMethon;
        public Vector4 Diffuse;
        public Vector4 Specular;
        public Vector3 Ambient;
        public Vector4 EdgeColor;
        public float EdgeSize;
        public Vector4 Texture;
        public Vector4 SubTexture;
        public Vector4 ToonTexture;
    }
    public struct MorphUVDesc
    {
        public int VertexIndex;
        public Vector4 Offset;
    }

    public class MorphDesc
    {
        public string Name;
        public string NameEN;
        public MorphCategory Category;
        public MorphType Type;

        public MorphSubMorphDesc[] SubMorphs;
        public MorphVertexDesc[] MorphVertexs;
        public MorphBoneDesc[] MorphBones;
        public MorphUVDesc[] MorphUVs;
        public MorphMaterialDesc[] MorphMaterials;

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}
namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static MorphSubMorphDesc GetMorphSubMorphDesc(PMX_MorphSubMorphDesc desc)
        {
            return new MorphSubMorphDesc()
            {
                GroupIndex = desc.GroupIndex,
                Rate = desc.Rate,
            };
        }
        public static MorphMaterialDesc GetMorphMaterialDesc(PMX_MorphMaterialDesc desc)
        {
            return new MorphMaterialDesc()
            {
                Ambient = desc.Ambient,
                Diffuse = desc.Diffuse,
                EdgeColor = desc.EdgeColor,
                EdgeSize = desc.EdgeSize,
                MaterialIndex = desc.MaterialIndex,
                MorphMethon = (MorphMaterialMethon)desc.MorphMethon,
                Specular = desc.Specular,
                SubTexture = desc.SubTexture,
                Texture = desc.Texture,
                ToonTexture = desc.ToonTexture,
            };
        }
        public static MorphVertexDesc GetMorphVertexDesc(PMX_MorphVertexDesc desc)
        {
            return new MorphVertexDesc()
            {
                Offset = desc.Offset,
                VertexIndex = desc.VertexIndex,
            };
        }
        public static MorphUVDesc GetMorphUVDesc(PMX_MorphUVDesc desc)
        {
            return new MorphUVDesc()
            {
                Offset = desc.Offset,
                VertexIndex = desc.VertexIndex,
            };
        }
        public static MorphBoneDesc GetMorphBoneDesc(PMX_MorphBoneDesc desc)
        {
            return new MorphBoneDesc()
            {
                BoneIndex = desc.BoneIndex,
                Rotation = desc.Rotation,
                Translation = desc.Translation,
            };
        }

        public static MorphDesc GetMorphDesc(PMX_Morph desc)
        {
            MorphSubMorphDesc[] subMorphDescs = null;
            if (desc.SubMorphs != null)
            {
                subMorphDescs = new MorphSubMorphDesc[desc.SubMorphs.Length];
                for (int i = 0; i < desc.SubMorphs.Length; i++)
                {
                    subMorphDescs[i] = GetMorphSubMorphDesc(desc.SubMorphs[i]);
                }
            }

            MorphMaterialDesc[] morphMaterialDescs = null;
            if (desc.MorphMaterials != null)
            {
                morphMaterialDescs = new MorphMaterialDesc[desc.MorphMaterials.Length];
                for (int i = 0; i < desc.MorphMaterials.Length; i++)
                {
                    morphMaterialDescs[i] = GetMorphMaterialDesc(desc.MorphMaterials[i]);
                }
            }
            MorphVertexDesc[] morphVertexDescs = null;
            if (desc.MorphVertexs != null)
            {
                morphVertexDescs = new MorphVertexDesc[desc.MorphVertexs.Length];
                for (int i = 0; i < desc.MorphVertexs.Length; i++)
                {
                    morphVertexDescs[i] = GetMorphVertexDesc(desc.MorphVertexs[i]);
                }
            }
            MorphUVDesc[] morphUVDescs = null;
            if (desc.MorphUVs != null)
            {
                morphUVDescs = new MorphUVDesc[desc.MorphUVs.Length];
                for (int i = 0; i < desc.MorphUVs.Length; i++)
                {
                    morphUVDescs[i] = GetMorphUVDesc(desc.MorphUVs[i]);
                }
            }
            MorphBoneDesc[] morphBoneDescs = null;
            if (desc.MorphBones != null)
            {
                morphBoneDescs = new MorphBoneDesc[desc.MorphBones.Length];
                for (int i = 0; i < desc.MorphBones.Length; i++)
                {
                    morphBoneDescs[i] = GetMorphBoneDesc(desc.MorphBones[i]);
                }
            }

            return new MorphDesc()
            {
                Name = desc.Name,
                NameEN = desc.NameEN,
                Category = (MorphCategory)desc.Category,
                Type = (MorphType)desc.Type,
                MorphBones = morphBoneDescs,
                MorphMaterials = morphMaterialDescs,
                MorphUVs = morphUVDescs,
                MorphVertexs = morphVertexDescs,
                SubMorphs = subMorphDescs,
            };
        }
        public static MMDMorphStateComponent LoadMorphStateComponent(PMXFormat pmx)
        {
            MMDMorphStateComponent component = new MMDMorphStateComponent();
            component.Reload(pmx);
            return component;
        }
        public static void Reload(this MMDMorphStateComponent component, PMXFormat pmx)
        {
            component.stringMorphIndexMap.Clear();
            component.morphs.Clear();
            int morphCount = pmx.Morphs.Count;
            for (int i = 0; i < pmx.Morphs.Count; i++)
            {
                component.morphs.Add(GetMorphDesc(pmx.Morphs[i]));
            }
            component.WeightInit = new float[morphCount];
            component.WeightInitA = new float[morphCount];
            component.WeightInitB = new float[morphCount];
            component.computedWeights = new float[morphCount];
            component.prevComputedWeights = new float[morphCount];
            component.CWFrameA = new float[morphCount];
            component.CWFrameB = new float[morphCount];
            component.PrevCWFrameA = new float[morphCount];
            component.PrevCWFrameB = new float[morphCount];
            for (int i = 0; i < morphCount; i++)
            {
                component.stringMorphIndexMap.Add(pmx.Morphs[i].Name, i);
            }
        }
    }
}