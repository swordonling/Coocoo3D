using Coocoo3DNativeInteroperable;
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
        public NMMDE_DrawFlag DrawFlags;
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
        public NMMDE_BoneFlag Flags;
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

    public class Morph
    {
        public string Name;
        public string NameEN;
        public NMMDE_MorphCategory Category;
        public NMMDE_MorphType Type;

        public MorphSubMorphStruct[] SubMorphs;
        public NMMD_MorphVertexDesc[] MorphVertexs;
        public NMMD_MorphBoneDesc[] MorphBones;
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
    public struct MorphUVStruct
    {
        public int VertexIndex;
        public Vector4 Offset;
    }
    public struct MorphMaterialStruct
    {
        public int MaterialIndex;
        public NMMDE_MorphMaterialMethon MorphMethon;
        public Vector4 Diffuse;
        public Vector4 Specular;
        public Vector3 Ambient;
        public Vector4 EdgeColor;
        public float EdgeSize;
        public Vector4 Texture;
        public Vector4 SubTexture;
        public Vector4 ToonTexture;
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
        public NMMDE_RigidBodyShape Shape;
        public Vector3 Dimemsions;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Mass;
        public float TranslateDamp;
        public float RotateDamp;
        public float Restitution;
        public float Friction;
        public NMMDE_RigidBodyType Type;
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
