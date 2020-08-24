using Coocoo3D.Components;
using Coocoo3D.MMDSupport;
using Coocoo3DNativeInteroperable;
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
        public IReadOnlyList<Morph> morphs;
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
        private static void ComputeWeight1(IReadOnlyList<Morph> morphs, float[] weightInit, float[] computedWeights, float[] prevComputedWeights)
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                prevComputedWeights[i] = computedWeights[i];
                computedWeights[i] = 0;
            }
            for (int i = 0; i < morphs.Count; i++)
            {
                Morph morph = morphs[i];
                if (morph.Type == NMMDE_MorphType.Group)
                    ComputeWeightGroup(morphs, morph, weightInit[i], computedWeights);
                else
                    computedWeights[i] += weightInit[i];
            }
        }
        private static void ComputeWeightGroup(IReadOnlyList<Morph> morphs, Morph morph, float rate, float[] computedWeights)
        {
            for (int i = 0; i < morph.SubMorphs.Length; i++)
            {
                MorphSubMorphStruct subMorphStruct = morph.SubMorphs[i];
                Morph subMorph = morphs[subMorphStruct.GroupIndex];
                if (subMorph.Type == NMMDE_MorphType.Group)
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
                if (morphs[i].Type == NMMDE_MorphType.Vertex)
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
}
namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static MMDMorphStateComponent LoadMorphStateComponent(PMXFormat pmx)
        {
            MMDMorphStateComponent component = new MMDMorphStateComponent();
            component.Reload(pmx);
            return component;
        }
        public static void Reload(this MMDMorphStateComponent component, PMXFormat pmx)
        {
            component.stringMorphIndexMap.Clear();
            int morphCount = pmx.Morphs.Count;
            component.morphs = pmx.Morphs;
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