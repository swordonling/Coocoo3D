using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;
using Coocoo3D.MMDSupport;

namespace Coocoo3D.FileFormat
{
    public class VMDFormat
    {
        public static VMDFormat Load(BinaryReader reader)
        {
            VMDFormat vmd = new VMDFormat();
            vmd.Reload(reader);
            return vmd;
        }

        public void Reload(BinaryReader reader)
        {
            headerChars = reader.ReadBytes(30);
            var uName = reader.ReadBytes(20);
            var jpEncoding = CodePagesEncodingProvider.Instance.GetEncoding("shift_jis");
            Name = jpEncoding.GetString(uName);

            int numOfBone = reader.ReadInt32();
            for (int i = 0; i < numOfBone; i++)
            {
                uName = reader.ReadBytes(15);
                int j = 0;
                for (; j < uName.Length; j++)
                {
                    if (uName[j] == 0) break;
                }
                string nName = jpEncoding.GetString(uName, 0, j);
                if (!BoneKeyFrameSet.TryGetValue(nName, out List<BoneKeyFrame> keyFrames))
                {
                    keyFrames = new List<BoneKeyFrame>();
                    BoneKeyFrameSet.Add(nName, keyFrames);
                }
                BoneKeyFrame keyFrame = new BoneKeyFrame();
                keyFrame.Frame = reader.ReadInt32();
                keyFrame.translation = ReadVector3(reader);
                keyFrame.rotation = ReadQuaternion(reader);
                keyFrame.xInterpolator = ReadBoneInterpolator(reader);
                keyFrame.yInterpolator = ReadBoneInterpolator(reader);
                keyFrame.zInterpolator = ReadBoneInterpolator(reader);
                keyFrame.rInterpolator = ReadBoneInterpolator(reader);

                keyFrames.Add(keyFrame);
            }

            int numOfMorph = reader.ReadInt32();
            for (int i = 0; i < numOfMorph; i++)
            {
                uName = reader.ReadBytes(15);
                int j = 0;
                for (; j < uName.Length; j++)
                {
                    if (uName[j] == 0) break;
                }
                string nName = jpEncoding.GetString(uName, 0, j);
                if (!MorphKeyFrameSet.TryGetValue(nName, out List<MorphKeyFrame> keyFrames))
                {
                    keyFrames = new List<MorphKeyFrame>();
                    MorphKeyFrameSet.Add(nName, keyFrames);
                }
                MorphKeyFrame keyFrame = new MorphKeyFrame();
                keyFrame.Frame = reader.ReadInt32();
                keyFrame.Weight = reader.ReadSingle();

                keyFrames.Add(keyFrame);
            }

            int numOfCam = reader.ReadInt32();
            for (int i = 0; i < numOfCam; i++)
            {
                CameraKeyFrame keyFrame = new CameraKeyFrame();
                keyFrame.Frame = reader.ReadInt32();
                keyFrame.focalLength = reader.ReadSingle();
                keyFrame.position = ReadVector3(reader);
                keyFrame.rotation = ReadVector3(reader);
                keyFrame.Interpolator = reader.ReadBytes(24);
                keyFrame.FOV = reader.ReadInt32();
                keyFrame.orthographic = reader.ReadByte() != 0;

                CameraKeyFrames.Add(keyFrame);
            }

            foreach (var keyframes in BoneKeyFrameSet.Values)
            {
                keyframes.Sort();
            }
            foreach (var keyframes in MorphKeyFrameSet.Values)
            {
                keyframes.Sort();
            }
            CameraKeyFrames.Sort();
        }

        public void SaveToFile(BinaryWriter writer)
        {
            writer.Write(headerChars);
            var jpEncoding = Encoding.GetEncoding("shift_jis");
            byte[] sChars = jpEncoding.GetBytes(Name);
            byte[] sChars2 = new byte[20];
            Array.Copy(sChars, sChars2, Math.Min(sChars.Length, sChars2.Length));
            writer.Write(sChars2);

            int numOfBone = 0;
            foreach (var collection in BoneKeyFrameSet.Values)
            {
                numOfBone += collection.Count;
            }
            writer.Write(numOfBone);
            foreach (var NKPair in BoneKeyFrameSet)
            {
                string objName = NKPair.Key;
                sChars = jpEncoding.GetBytes(objName);
                sChars2 = new byte[15];
                Array.Copy(sChars, sChars2, Math.Min(sChars.Length, sChars2.Length));
                foreach (var keyFrame in NKPair.Value)
                {
                    writer.Write(sChars2);
                    writer.Write(keyFrame.Frame);
                    WriteVector3(writer, keyFrame.translation);
                    WriteQuaternion(writer, keyFrame.rotation);
                    WriteBoneInterpolator(writer, keyFrame.xInterpolator);
                    WriteBoneInterpolator(writer, keyFrame.yInterpolator);
                    WriteBoneInterpolator(writer, keyFrame.zInterpolator);
                    WriteBoneInterpolator(writer, keyFrame.rInterpolator);
                }
            }

            int numOfMorph = 0;
            foreach (var collection in MorphKeyFrameSet.Values)
            {
                numOfMorph += collection.Count;
            }
            writer.Write(numOfMorph);
            foreach (var NMPair in MorphKeyFrameSet)
            {
                string objName = NMPair.Key;
                sChars = jpEncoding.GetBytes(objName);
                sChars2 = new byte[15];
                Array.Copy(sChars, sChars2, Math.Min(sChars.Length, sChars2.Length));
                foreach (var keyFrame in NMPair.Value)
                {
                    writer.Write(sChars2);
                    writer.Write(keyFrame.Frame);
                    writer.Write(keyFrame.Weight);
                }
            }

            int numOfCam = CameraKeyFrames.Count;
            writer.Write(numOfCam);
            foreach (var keyframe in CameraKeyFrames)
            {
                writer.Write(keyframe.Frame);
                writer.Write(keyframe.focalLength);
                WriteVector3(writer, keyframe.position);
                WriteVector3(writer, keyframe.rotation);
                writer.Write(keyframe.Interpolator);
                writer.Write(keyframe.FOV);
                writer.Write(Convert.ToByte(keyframe.orthographic));
            }

        }

        public VMDFormat GetCopy()
        {
            VMDFormat vmd = new VMDFormat();

            return vmd;
        }
        public byte[] headerChars;
        public string Name;
        public Dictionary<string, List<BoneKeyFrame>> BoneKeyFrameSet { get; set; } = new Dictionary<string, List<BoneKeyFrame>>();
        public Dictionary<string, List<MorphKeyFrame>> MorphKeyFrameSet { get; set; } = new Dictionary<string, List<MorphKeyFrame>>();
        public List<CameraKeyFrame> CameraKeyFrames { get; set; } = new List<CameraKeyFrame>();

        private Interpolator ReadBoneInterpolator(BinaryReader reader)
        {
            const float c_is = 1.0f / 127.0f;
            var x = new Interpolator();
            x.ax = (((reader.ReadInt32() & 0xFF) ^ 0x80) - 0x80) * c_is;
            x.ay = (((reader.ReadInt32() & 0xFF) ^ 0x80) - 0x80) * c_is;
            x.bx = (((reader.ReadInt32() & 0xFF) ^ 0x80) - 0x80) * c_is;
            x.by = (((reader.ReadInt32() & 0xFF) ^ 0x80) - 0x80) * c_is;
            return x;
        }
        private Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 vector3 = new Vector3();
            vector3.X = reader.ReadSingle();
            vector3.Y = reader.ReadSingle();
            vector3.Z = reader.ReadSingle();
            return vector3;
        }
        private Quaternion ReadQuaternion(BinaryReader reader)
        {
            Quaternion quaternion = new Quaternion();
            quaternion.X = reader.ReadSingle();
            quaternion.Y = reader.ReadSingle();
            quaternion.Z = reader.ReadSingle();
            quaternion.W = reader.ReadSingle();
            return quaternion;
        }
        private void WriteBoneInterpolator(BinaryWriter writer, Interpolator interpolator)
        {
            writer.Write((((int)Math.Round(interpolator.ax * 127) + 0x80) ^ 0x80) & 0xFF);
            writer.Write((((int)Math.Round(interpolator.ay * 127) + 0x80) ^ 0x80) & 0xFF);
            writer.Write((((int)Math.Round(interpolator.bx * 127) + 0x80) ^ 0x80) & 0xFF);
            writer.Write((((int)Math.Round(interpolator.by * 127) + 0x80) ^ 0x80) & 0xFF);
        }
        private void WriteVector3(BinaryWriter writer, Vector3 vector3)
        {
            writer.Write(vector3.X);
            writer.Write(vector3.Y);
            writer.Write(vector3.Z);
        }
        private void WriteQuaternion(BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.X);
            writer.Write(quaternion.Y);
            writer.Write(quaternion.Z);
            writer.Write(quaternion.W);
        }
    }

}
