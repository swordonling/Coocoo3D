using Coocoo3D.Components;
using Coocoo3D.FileFormat;
using Coocoo3D.MMDSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Components
{
    public class MMDMotionComponent
    {
        public Dictionary<string, List<BoneKeyFrame>> BoneKeyFrameSet { get; set; } = new Dictionary<string, List<BoneKeyFrame>>();
        public Dictionary<string, List<MorphKeyFrame>> MorphKeyFrameSet { get; set; } = new Dictionary<string, List<MorphKeyFrame>>();

        const float c_framePreSecond = 30;
        public static BoneKeyFrame GetBoneMotion(List<BoneKeyFrame> keyframeSet, float time)
        {
            float frame = Math.Max(time * c_framePreSecond, 0);
            int left = 0;
            int right = keyframeSet.Count - 1;
            if (left == right) return keyframeSet[left];
            if (keyframeSet[right].Frame < frame) return keyframeSet[right];

            while (right - left > 1)
            {
                int mid = (right + left) / 2;
                if (keyframeSet[mid].Frame > frame)
                    right = mid;
                else
                    left = mid;
            }
            BoneKeyFrame keyFrameLeft = keyframeSet[left];
            BoneKeyFrame keyFrameRight = keyframeSet[right];
            float _getAmount(Interpolator interpolator, float _a)
            {
                if (interpolator.ax == interpolator.ay && interpolator.bx == interpolator.by)
                    return _a;
                var _curve = Utility.CubicBezierCurve.Load(interpolator.ax, interpolator.ay, interpolator.bx, interpolator.by);
                return _curve.Sample(_a);
            }
            var ri = keyFrameRight.rInterpolator;
            float amount = _getAmount(ri, (frame - keyFrameLeft.Frame) / (keyFrameRight.Frame - keyFrameLeft.Frame));


            BoneKeyFrame newKeyFrame = new BoneKeyFrame();
            newKeyFrame.Frame = (int)MathF.Round(frame);
            newKeyFrame.rotation = Quaternion.Slerp(keyFrameLeft.rotation, keyFrameRight.rotation, amount);
            newKeyFrame.translation = Vector3.Lerp(keyFrameLeft.translation, keyFrameRight.translation, amount);
            return newKeyFrame;

        }
        public static MorphKeyFrame GetMorphMotion(List<MorphKeyFrame> keyframeSet, float time)
        {
            float frame = Math.Max(time * c_framePreSecond, 0);
            int left = 0;
            int right = keyframeSet.Count - 1;
            if (left == right) return keyframeSet[left];
            if (keyframeSet[right].Frame < frame) return keyframeSet[right];

            while (right - left > 1)
            {
                int mid = (right + left) / 2;
                if (keyframeSet[mid].Frame > frame)
                    right = mid;
                else
                    left = mid;
            }
            MorphKeyFrame keyFrameLeft = keyframeSet[left];
            MorphKeyFrame keyFrameRight = keyframeSet[right];
            float amount = (frame - keyFrameLeft.Frame) / (keyFrameRight.Frame - keyFrameLeft.Frame);


            MorphKeyFrame newKeyFrame = new MorphKeyFrame();
            newKeyFrame.Frame = (int)MathF.Round(frame);
            newKeyFrame.Weight = (1 - amount) * keyFrameLeft.Weight +  amount* keyFrameRight.Weight;
            return newKeyFrame;
        }
    }
}


namespace Coocoo3D.FileFormat
{
    public static partial class VMDFormatExtension
    {
        public static void ReloadEmpty(this MMDMotionComponent motionComponent)
        {
            motionComponent.BoneKeyFrameSet.Clear();
            motionComponent.MorphKeyFrameSet.Clear();
        }
        public static void Reload(this MMDMotionComponent motionComponent, VMDFormat vmd)
        {
            motionComponent.BoneKeyFrameSet.Clear();
            motionComponent.MorphKeyFrameSet.Clear();

            foreach (var pair in vmd.BoneKeyFrameSet)
            {
                motionComponent.BoneKeyFrameSet.Add(pair.Key, new List<BoneKeyFrame>(pair.Value));
            }
            foreach (var pair in vmd.MorphKeyFrameSet)
            {
                motionComponent.MorphKeyFrameSet.Add(pair.Key, new List<MorphKeyFrame>(pair.Value));
            }
        }
    }
}