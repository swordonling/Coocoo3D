using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.MMDSupport
{

    public struct BoneKeyFrame : IComparable<BoneKeyFrame>
    {
        public int Frame { get; set; }
        public Vector3 Translation { get => translation; set => translation = value; }
        public Vector3 translation;
        public Quaternion Rotation { get => rotation; set => rotation = value; }
        public Quaternion rotation;
        public Interpolator xInterpolator;
        public Interpolator yInterpolator;
        public Interpolator zInterpolator;
        public Interpolator rInterpolator;

        public int CompareTo(BoneKeyFrame other)
        {
            return Frame.CompareTo(other.Frame);
        }
    }

    public struct MorphKeyFrame : IComparable<MorphKeyFrame>
    {
        public int Frame;
        public float Weight;

        public int CompareTo(MorphKeyFrame other)
        {
            return Frame.CompareTo(other.Frame);
        }
    }

    public struct CameraKeyFrame : IComparable<CameraKeyFrame>
    {
        public int Frame;
        public float focalLength;
        public Vector3 position;
        public Vector3 rotation;
        public byte[] Interpolator;
        public int FOV;
        public bool orthographic;

        public int CompareTo(CameraKeyFrame other)
        {
            return Frame.CompareTo(other.Frame);
        }
    }

    public struct Interpolator
    {
        public float ax;
        public float bx;
        public float ay;
        public float by;
    }
}
