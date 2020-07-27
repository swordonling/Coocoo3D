using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.MMDSupport
{
    public class Vertex
    {
        public Vector3 Normal { get => innerStruct.Normal; set { innerStruct.Normal = value; } }
        public Vector2 UvCoordinate { get => innerStruct.UvCoordinate; set { innerStruct.UvCoordinate = value; } }
        public Vector4[] ExtraUvCoordinate;
        public int[] boneId = new int[4];
        public float[] weight = new float[4];
        public float EdgeScale { get => innerStruct.EdgeScale; set { innerStruct.EdgeScale = value; } }
        public Vector3 Coordinate;
        public VertexStruct innerStruct;

        public struct VertexStruct
        {
            public Vector3 Normal;
            public Vector2 UvCoordinate;
            public float EdgeScale;
        }
        public override string ToString()
        {
            return Coordinate.ToString();
        }
    }

    public class MMDMaterial
    {
        public string Name;
        public string NameEN;
        public Vector4 DiffuseColor;
        public Vector4 SpecularColor;
        public Vector3 AmbientColor;
        public DrawFlags DrawFlags;
        public Vector4 EdgeColor;
        public float EdgeScale;
        public int TextureIndex;
        public int secondTextureIndex;
        public byte secondTextureType;
        public bool UseToon;
        public int ToonIndex;
        public string Meta;
        public int TriangeIndexStartNum;
        public int TriangeIndexNum;
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
    public enum DrawFlags
    {
        DrawDoubleFace = 0x1,
        DrawGroundShadow = 0x2,
        CastSelfShadow = 0x4,
        DrawSelfShadow = 0x8,
        DrawEdge = 0x10,
    }

    public class MMDTexture
    {
        public string TexturePath;
        public override string ToString()
        {
            return string.Format("{0}", TexturePath);
        }
    }

    public class Bone
    {
        public string Name;
        public string NameEN;
        public Vector3 Position;
        public int ParentIndex;
        public int TransformLevel;
        public BoneFlags Flags;
        public int ChildId;
        public Vector3 ChildOffset;
        public Vector3 RotAxisFixed;

        public int AppendBoneIndex;
        public float AppendBoneRatio;

        public Vector3 LocalAxisX;
        public Vector3 LocalAxisY;
        public Vector3 LocalAxisZ;

        public int ExportKey;

        public BoneIK boneIK;
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }

    public class BoneIK
    {
        public int IKTargetIndex;
        public int CCDIterateLimit;
        public float CCDAngleLimit;
        public BoneIKLink[] IKLinks;
    }

    public class BoneIKLink
    {
        public int LinkedIndex;
        public bool HasLimit;
        public Vector3 LimitMin;
        public Vector3 LimitMax;
        public BoneIKLink GetCopy()
        {
            return new BoneIKLink()
            {
                LinkedIndex = LinkedIndex,
                HasLimit = HasLimit,
                LimitMin = LimitMin,
                LimitMax = LimitMax
            };
        }
    }

    public enum BoneFlags
    {
        ChildUseId = 0x1,
        Rotatable = 0x2,
        Movable = 0x4,
        Visible = 0x8,
        Controllable = 0x10,
        HasIK = 0x20,
        AcquireRotate = 0x100,
        AcquireTranslate = 0x200,
        RotAxisFixed = 0x400,
        UseLocalAxis = 0x800,
        PostPhysics = 0x1000,
        ReceiveTransform = 0x2000
    }

    public class Morph
    {
        public string Name;
        public string NameEN;
        public MorphCategory Category;
        public MorphType Type;

        public MorphSubMorphStruct[] SubMorphs;
        public MorphVertexStruct[] MorphVertexs;
        public MorphBoneStruct[] MorphBones;
        public MorphUVStruct[] MorphUVs;
        public MorphMaterialStruct[] MorphMaterials;

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }

    public struct MorphSubMorphStruct
    {
        public int GroupIndex;
        public float Rate;
    }
    public struct MorphVertexStruct
    {
        public int VertexIndex;
        public Vector3 Offset;
    }
    public struct MorphBoneStruct
    {
        public int BoneIndex;
        public Vector3 Translation;
        public Quaternion Rotation;
    }
    public struct MorphUVStruct
    {
        public int VertexIndex;
        public Vector4 Offset;
    }
    public struct MorphMaterialStruct
    {
        public int MaterialIndex;
        public MorphMaterialMorphMethon MorphMethon;
        public Vector4 Diffuse;
        public Vector4 Specular;
        public Vector3 Ambient;
        public Vector4 EdgeColor;
        public float EdgeSize;
        public Vector4 Texture;
        public Vector4 SubTexture;
        public Vector4 ToonTexture;
    }

    public enum MorphCategory
    {
        System = 0,
        Eyebrow = 1,
        Eye = 2,
        Mouth = 3,
        Other = 4
    }
    public enum MorphType
    {
        Group = 0,
        Vertex = 1,
        Bone = 2,
        UV = 3,
        ExtUV1 = 4,
        ExtUV2 = 5,
        ExtUV3 = 6,
        ExtUV4 = 7,
        Material = 8
    }
    public enum MorphMaterialMorphMethon
    {
        Mul = 0,
        Add = 1
    }

    public class MMDEntry
    {
        public string Name;
        public string NameEN;
        public EntryElement[] elements;
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
    public struct EntryElement
    {
        public byte Type;
        public int Index;
        public override string ToString()
        {
            return string.Format("{0},{1}", Type, Index);
        }
    }

    public class MMDRigidBody
    {
        public string Name;
        public string NameEN;
        public int AssociatedBoneIndex;
        public byte CollisionGroup;
        public ushort CollisionMask;
        public RigidBodyShape Shape;
        public Vector3 Dimemsions;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Mass;
        public float TranslateDamp;
        public float RotateDamp;
        public float Restitution;
        public float Friction;
        public RigidBodyType Type;
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
        public MMDRigidBody GetCopy()
        {
            return new MMDRigidBody()
            {
                Name = Name,
                NameEN = NameEN,
                AssociatedBoneIndex = AssociatedBoneIndex,
                CollisionGroup = CollisionGroup,
                CollisionMask = CollisionMask,
                Shape = Shape,
                Dimemsions = Dimemsions,
                Position = Position,
                Rotation = Rotation,
                Mass = Mass,
                TranslateDamp = TranslateDamp,
                RotateDamp = RotateDamp,
                Restitution = Restitution,
                Friction = Friction,
                Type = Type,
            };
        }
    }
    public enum RigidBodyShape
    {
        Sphere = 0,
        Box = 1,
        Capsule = 2
    }
    public enum RigidBodyType
    {
        Kinematic = 0,
        Physics = 1,
        PhysicsStrict = 2,
        PhysicsGhost = 3
    }

    public class MMDJoint
    {
        public string Name;
        public string NameEN;
        public byte Type;
        public int AssociatedRigidBodyIndex1;
        public int AssociatedRigidBodyIndex2;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 PositionMinimum;
        public Vector3 PositionMaximum;
        public Vector3 RotationMinimum;
        public Vector3 RotationMaximum;
        public Vector3 PositionSpring;
        public Vector3 RotationSpring;
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
        public MMDJoint GetCopy()
        {
            return new MMDJoint()
            {
                Name = Name,
                NameEN = NameEN,
                Type = Type,
                AssociatedRigidBodyIndex1 = AssociatedRigidBodyIndex1,
                AssociatedRigidBodyIndex2 = AssociatedRigidBodyIndex2,
                Position = Position,
                Rotation = Rotation,
                PositionMinimum = PositionMinimum,
                PositionMaximum = PositionMaximum,
                RotationMinimum = RotationMinimum,
                RotationMaximum = RotationMaximum,
                PositionSpring = PositionSpring,
                RotationSpring = RotationSpring,
            };
        }
    }

}
