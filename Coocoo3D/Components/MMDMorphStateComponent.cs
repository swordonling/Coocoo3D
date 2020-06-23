using Coocoo3D.Components;
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
        public IReadOnlyList<Morph> morphs;
        public float[] weightInput;
        public float[] computedWeights;
        public float[] prevComputedWeights;
        public Dictionary<string, int> stringMorphIndexMap = new Dictionary<string, int>();
        public void SetPose(MMDMotionComponent motionComponent, float time)
        {
            foreach (var pair in stringMorphIndexMap)
            {
                ref float weight = ref weightInput[pair.Value];
                if (motionComponent.MorphKeyFrameSet.TryGetValue(pair.Key, out var value))
                {
                    var keyframe = MMDMotionComponent.GetMorphMotion(value, time);
                    weight = keyframe.Weight;
                }
                else
                {
                    weight = 0;
                }
            }
        }
        public void ComputeWeight()
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                prevComputedWeights[i] = computedWeights[i];
                computedWeights[i] = 0;
            }
            for (int i = 0; i < morphs.Count; i++)
            {
                Morph morph = morphs[i];
                if (morph.Type == MorphType.Group)
                    ComputeWeightGroup(morph, weightInput[i]);
                else
                    computedWeights[i] += weightInput[i];
            }
        }
        private void ComputeWeightGroup(Morph morph, float rate)
        {
            for (int i = 0; i < morph.SubMorphs.Length; i++)
            {
                MorphSubMorphStruct subMorphStruct = morph.SubMorphs[i];
                Morph subMorph = morphs[subMorphStruct.GroupIndex];
                if (subMorph.Type == MorphType.Group)
                    ComputeWeightGroup(subMorph, rate * subMorphStruct.Rate);
                else
                    computedWeights[subMorphStruct.GroupIndex] += rate * subMorphStruct.Rate;
            }
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
            component.weightInput = new float[morphCount];
            component.computedWeights = new float[morphCount];
            component.prevComputedWeights = new float[morphCount];
            for (int i = 0; i < morphCount; i++)
            {
                component.stringMorphIndexMap.Add(pmx.Morphs[i].Name, i);
            }
        }
    }
}