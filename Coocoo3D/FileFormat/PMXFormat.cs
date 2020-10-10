using Coocoo3D.MMDSupport;
using Coocoo3DNativeInteroperable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.FileFormat
{
    public partial class PMXFormat
    {
        public bool Ready;
        public Task LoadTask;

        public string Name;
        public string NameEN;
        public string Description;
        public string DescriptionEN;

        List<Vertex> Vertices = new List<Vertex>();
        List<int> TriangleIndexs = new List<int>();
        public List<MMDTexture> Textures = new List<MMDTexture>();
        public List<MMDMaterial> Materials = new List<MMDMaterial>();
        public List<Bone> Bones = new List<Bone>();
        public List<Morph> Morphs = new List<Morph>();
        public List<MMDEntry> Entries = new List<MMDEntry>();
        public List<MMDRigidBody> RigidBodies = new List<MMDRigidBody>();
        public List<MMDJoint> Joints = new List<MMDJoint>();

        public static PMXFormat Load(BinaryReader reader)
        {
            PMXFormat pmxFormat = new PMXFormat();
            pmxFormat.Reload(reader);
            return pmxFormat;
        }

        public void Reload(BinaryReader reader)
        {
            Vertices.Clear();
            TriangleIndexs.Clear();
            Textures.Clear();
            Materials.Clear();
            Bones.Clear();
            Morphs.Clear();
            Entries.Clear();
            RigidBodies.Clear();
            Joints.Clear();


            int fileHeader = reader.ReadInt32();
            if (fileHeader != 0x20584D50) throw new NotImplementedException("File is not Pmx format.");//' XMP'
            float version = reader.ReadSingle();
            byte flagsSize = reader.ReadByte();//useless

            bool isUtf8Encoding = reader.ReadByte() != 0;
            byte extraUVNumber = reader.ReadByte();
            byte vertexIndexSize = reader.ReadByte();
            byte textureIndexSize = reader.ReadByte();
            byte materialIndexSize = reader.ReadByte();
            byte boneIndexSize = reader.ReadByte();
            byte morphIndexSize = reader.ReadByte();
            byte rigidBodyIndexSize = reader.ReadByte();

            Encoding encoding = isUtf8Encoding ? Encoding.UTF8 : Encoding.Unicode;

            Name = ReadString(reader, encoding);
            NameEN = ReadString(reader, encoding);
            Description = ReadString(reader, encoding);
            DescriptionEN = ReadString(reader, encoding);

            int countOfVertex = reader.ReadInt32();
            Vertices.Capacity = countOfVertex;
            for (int i = 0; i < countOfVertex; i++)
            {
                Vertex vertex = new Vertex();
                vertex.Coordinate = ReadVector3(reader);
                vertex.Normal = ReadVector3(reader);
                vertex.UvCoordinate = ReadVector2(reader);
                if (extraUVNumber > 0)
                {
                    vertex.ExtraUvCoordinate = new Vector4[extraUVNumber];
                    for (int j = 0; j < extraUVNumber; j++)
                    {
                        vertex.ExtraUvCoordinate[j] = ReadVector4(reader);
                    }
                }
                int skinningType = reader.ReadByte();
                if (skinningType == (int)WeightDeformType.BDEF1)
                {
                    vertex.boneId[0] = ReadIndex(reader, boneIndexSize);
                    vertex.boneId[1] = -1;
                    vertex.boneId[2] = -1;
                    vertex.boneId[3] = -1;
                    vertex.weight[0] = 1;
                }
                else if (skinningType == (int)WeightDeformType.BDEF2)
                {
                    vertex.boneId[0] = ReadIndex(reader, boneIndexSize);
                    vertex.boneId[1] = ReadIndex(reader, boneIndexSize);
                    vertex.boneId[2] = -1;
                    vertex.boneId[3] = -1;
                    vertex.weight[0] = reader.ReadSingle();
                    vertex.weight[1] = 1.0f - vertex.weight[0];
                }
                else if (skinningType == (int)WeightDeformType.BDEF4)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        vertex.boneId[j] = ReadIndex(reader, boneIndexSize);
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        vertex.weight[j] = reader.ReadSingle();
                    }
                }
                else if (skinningType == (int)WeightDeformType.SDEF)
                {

                    vertex.boneId[0] = ReadIndex(reader, boneIndexSize);
                    vertex.boneId[1] = ReadIndex(reader, boneIndexSize);
                    vertex.boneId[2] = -1;
                    vertex.boneId[3] = -1;
                    vertex.weight[0] = reader.ReadSingle();
                    vertex.weight[1] = 1.0f - vertex.weight[0];
                    ReadVector3(reader);
                    ReadVector3(reader);
                    ReadVector3(reader);
                }
                else
                {

                }
                vertex.EdgeScale = reader.ReadSingle();
                Vertices.Add(vertex);
            }

            int countOfTriangleIndex = reader.ReadInt32();
            TriangleIndexs.Capacity = countOfTriangleIndex;
            for (int i = 0; i < countOfTriangleIndex; i++)
            {
                TriangleIndexs.Add(ReadIndexUnsigned(reader, vertexIndexSize));
            }

            int countOfTexture = reader.ReadInt32();
            Textures.Capacity = countOfTexture;
            for (int i = 0; i < countOfTexture; i++)
            {
                MMDTexture texture = new MMDTexture();
                texture.TexturePath = ReadString(reader, encoding);
                Textures.Add(texture);
            }

            int countOfMaterial = reader.ReadInt32();
            int triangleIndexBaseShift = 0;
            Materials.Capacity = countOfMaterial;
            for (int i = 0; i < countOfMaterial; i++)
            {
                MMDMaterial material = new MMDMaterial();
                material.Name = ReadString(reader, encoding);
                material.NameEN = ReadString(reader, encoding);
                material.DiffuseColor = ReadVector4(reader);
                material.SpecularColor = ReadVector4(reader);
                material.AmbientColor = ReadVector3(reader);
                material.DrawFlags = (NMMDE_DrawFlag)reader.ReadByte();
                material.EdgeColor = ReadVector4(reader);
                material.EdgeScale = reader.ReadSingle();

                material.TextureIndex = ReadIndex(reader, textureIndexSize);
                material.secondTextureIndex = ReadIndex(reader, textureIndexSize);
                material.secondTextureType = reader.ReadByte();
                material.UseToon = reader.ReadByte() != 0;
                if (material.UseToon) material.ToonIndex = reader.ReadByte();
                else material.ToonIndex = ReadIndex(reader, textureIndexSize);
                material.Meta = ReadString(reader, encoding);

                material.TriangeIndexStartNum = triangleIndexBaseShift;
                material.TriangeIndexNum = reader.ReadInt32();
                triangleIndexBaseShift += material.TriangeIndexNum;

                Materials.Add(material);
            }

            int countOfBone = reader.ReadInt32();
            Bones.Capacity = countOfBone;
            for (int i = 0; i < countOfBone; i++)
            {
                Bone bone = new Bone();
                bone.Name = ReadString(reader, encoding);
                bone.NameEN = ReadString(reader, encoding);
                bone.Position = ReadVector3(reader);
                bone.ParentIndex = ReadIndex(reader, boneIndexSize);
                bone.TransformLevel = reader.ReadInt32();
                bone.Flags = (NMMDE_BoneFlag)reader.ReadUInt16();
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.ChildUseId))
                {
                    bone.ChildId = ReadIndex(reader, boneIndexSize);
                }
                else
                {
                    bone.ChildOffset = ReadVector3(reader);
                }
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.RotAxisFixed))
                {
                    bone.RotAxisFixed = ReadVector3(reader);
                }
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.AcquireRotate) | bone.Flags.HasFlag(NMMDE_BoneFlag.AcquireTranslate))
                {
                    bone.AppendBoneIndex = ReadIndex(reader, boneIndexSize);
                    bone.AppendBoneRatio = reader.ReadSingle();
                }
                else
                {
                    bone.AppendBoneIndex = -1;
                }
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.UseLocalAxis))
                {
                    bone.LocalAxisX = ReadVector3(reader);
                    bone.LocalAxisZ = ReadVector3(reader);
                    bone.LocalAxisY = Vector3.Cross(bone.LocalAxisX, bone.LocalAxisZ);
                    bone.LocalAxisZ = Vector3.Cross(bone.LocalAxisX, bone.LocalAxisY);
                    bone.LocalAxisX = Vector3.Normalize(bone.LocalAxisX);
                    bone.LocalAxisY = Vector3.Normalize(bone.LocalAxisY);
                    bone.LocalAxisZ = Vector3.Normalize(bone.LocalAxisZ);
                }
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.ReceiveTransform))
                {
                    bone.ExportKey = reader.ReadInt32();
                }
                if (bone.Flags.HasFlag(NMMDE_BoneFlag.HasIK))
                {
                    BoneIK boneIK = new BoneIK();
                    boneIK.IKTargetIndex = ReadIndex(reader, boneIndexSize);
                    boneIK.CCDIterateLimit = reader.ReadInt32();
                    boneIK.CCDAngleLimit = reader.ReadSingle();
                    int countOfIKLinks = reader.ReadInt32();
                    boneIK.IKLinks = new BoneIKLink[countOfIKLinks];
                    for (int j = 0; j < countOfIKLinks; j++)
                    {
                        BoneIKLink boneIKLink = new BoneIKLink();
                        boneIKLink.LinkedIndex = ReadIndex(reader, boneIndexSize);
                        boneIKLink.HasLimit = reader.ReadByte() != 0;
                        if (boneIKLink.HasLimit)
                        {
                            boneIKLink.LimitMin = ReadVector3(reader);
                            boneIKLink.LimitMax = ReadVector3(reader);
                        }
                        boneIK.IKLinks[j] = boneIKLink;
                    }
                    bone.boneIK = boneIK;
                }
                Bones.Add(bone);
            }

            int countOfMorph = reader.ReadInt32();
            Morphs.Capacity = countOfMorph;
            for (int i = 0; i < countOfMorph; i++)
            {
                Morph morph = new Morph();
                morph.Name = ReadString(reader, encoding);
                morph.NameEN = ReadString(reader, encoding);
                morph.Category = (NMMDE_MorphCategory)reader.ReadByte();
                morph.Type = (NMMDE_MorphType)reader.ReadByte();

                int countOfMorphData = reader.ReadInt32();
                switch (morph.Type)
                {
                    case NMMDE_MorphType.Group:
                        morph.SubMorphs = new MorphSubMorphStruct[countOfMorphData];
                        for (int j = 0; j < countOfMorphData; j++)
                        {
                            MorphSubMorphStruct subMorph = new MorphSubMorphStruct();
                            subMorph.GroupIndex = ReadIndex(reader, morphIndexSize);
                            subMorph.Rate = reader.ReadSingle();
                            morph.SubMorphs[j] = subMorph;
                        }
                        break;
                    case NMMDE_MorphType.Vertex:
                        morph.MorphVertexs = new NMMD_MorphVertexDesc[countOfMorphData];
                        for (int j = 0; j < countOfMorphData; j++)
                        {
                            NMMD_MorphVertexDesc vertexStruct = new NMMD_MorphVertexDesc();
                            vertexStruct.VertexIndex = ReadIndexUnsigned(reader, vertexIndexSize);
                            vertexStruct.Offset = ReadVector3(reader);
                            morph.MorphVertexs[j] = vertexStruct;
                        }
                        Array.Sort(morph.MorphVertexs, _morphVertexCmp);//optimize for cpu L1 cache
                        break;
                    case NMMDE_MorphType.Bone:
                        morph.MorphBones = new NMMD_MorphBoneDesc[countOfMorphData];
                        for (int j = 0; j < countOfMorphData; j++)
                        {
                            NMMD_MorphBoneDesc morphBoneStruct = new NMMD_MorphBoneDesc();
                            morphBoneStruct.BoneIndex = ReadIndex(reader, boneIndexSize);
                            morphBoneStruct.Translation = ReadVector3(reader);
                            morphBoneStruct.Rotation = ReadQuaternion(reader);
                            morph.MorphBones[j] = morphBoneStruct;
                        }
                        break;
                    case NMMDE_MorphType.UV:
                    case NMMDE_MorphType.ExtUV1:
                    case NMMDE_MorphType.ExtUV2:
                    case NMMDE_MorphType.ExtUV3:
                    case NMMDE_MorphType.ExtUV4:
                        morph.MorphUVs = new MorphUVStruct[countOfMorphData];
                        for (int j = 0; j < countOfMorphData; j++)
                        {
                            MorphUVStruct morphUVStruct = new MorphUVStruct();
                            morphUVStruct.VertexIndex = ReadIndexUnsigned(reader, vertexIndexSize);
                            morphUVStruct.Offset = ReadVector4(reader);
                            morph.MorphUVs[j] = morphUVStruct;
                        }
                        break;
                    case NMMDE_MorphType.Material:
                        morph.MorphMaterials = new MorphMaterialStruct[countOfMaterial];
                        for (int j = 0; j < countOfMorphData; j++)
                        {
                            MorphMaterialStruct morphMaterial = new MorphMaterialStruct();
                            morphMaterial.MaterialIndex = ReadIndex(reader, materialIndexSize);
                            morphMaterial.MorphMethon = (NMMDE_MorphMaterialMethon)reader.ReadByte();
                            morphMaterial.Diffuse = ReadVector4(reader);
                            morphMaterial.Specular = ReadVector4(reader);
                            morphMaterial.Ambient = ReadVector3(reader);
                            morphMaterial.EdgeColor = ReadVector4(reader);
                            morphMaterial.EdgeSize = reader.ReadSingle();
                            morphMaterial.Texture = ReadVector4(reader);
                            morphMaterial.SubTexture = ReadVector4(reader);
                            morphMaterial.ToonTexture = ReadVector4(reader);
                            morph.MorphMaterials[j] = morphMaterial;
                        }
                        break;
                    default:
                        throw new NotImplementedException("Read morph fault.");
                }
                Morphs.Add(morph);
            }

            int countOfEntry = reader.ReadInt32();
            Entries.Capacity = countOfEntry;
            for (int i = 0; i < countOfEntry; i++)
            {
                MMDEntry entry = new MMDEntry();
                entry.Name = ReadString(reader, encoding);
                entry.NameEN = ReadString(reader, encoding);
                reader.ReadByte();//跳过一个字节
                int countOfElement = reader.ReadInt32();
                entry.elements = new EntryElement[countOfElement];
                for (int j = 0; j < countOfElement; j++)
                {
                    EntryElement element = new EntryElement();
                    element.Type = reader.ReadByte();
                    if (element.Type == 1)
                    {
                        element.Index = ReadIndex(reader, morphIndexSize);
                    }
                    else
                    {
                        element.Index = ReadIndex(reader, boneIndexSize);
                    }
                    entry.elements[j] = element;
                }
                Entries.Add(entry);
            }

            int countOfRigidBody = reader.ReadInt32();
            RigidBodies.Capacity = countOfRigidBody;
            for (int i = 0; i < countOfRigidBody; i++)
            {
                MMDRigidBody rigidBody = new MMDRigidBody();
                rigidBody.Name = ReadString(reader, encoding);
                rigidBody.NameEN = ReadString(reader, encoding);
                rigidBody.AssociatedBoneIndex = ReadIndex(reader, boneIndexSize);
                rigidBody.CollisionGroup = reader.ReadByte();
                rigidBody.CollisionMask = reader.ReadUInt16();
                rigidBody.Shape = (NMMDE_RigidBodyShape)reader.ReadByte();
                rigidBody.Dimemsions = ReadVector3(reader);
                rigidBody.Position = ReadVector3(reader);
                rigidBody.Rotation = ReadVector3(reader);
                rigidBody.Mass = reader.ReadSingle();
                rigidBody.TranslateDamp = reader.ReadSingle();
                rigidBody.RotateDamp = reader.ReadSingle();
                rigidBody.Restitution = reader.ReadSingle();
                rigidBody.Friction = reader.ReadSingle();
                rigidBody.Type = (NMMDE_RigidBodyType)reader.ReadByte();

                RigidBodies.Add(rigidBody);
            }

            int countOfJoint = reader.ReadInt32();
            Joints.Capacity = countOfJoint;
            for (int i = 0; i < countOfJoint; i++)
            {
                MMDJoint joint = new MMDJoint();
                joint.Name = ReadString(reader, encoding);
                joint.NameEN = ReadString(reader, encoding);
                joint.Type = reader.ReadByte();

                joint.AssociatedRigidBodyIndex1 = ReadIndex(reader, rigidBodyIndexSize);
                joint.AssociatedRigidBodyIndex2 = ReadIndex(reader, rigidBodyIndexSize);
                joint.Position = ReadVector3(reader);
                joint.Rotation = ReadVector3(reader);
                joint.PositionMinimum = ReadVector3(reader);
                joint.PositionMaximum = ReadVector3(reader);
                joint.RotationMinimum = ReadVector3(reader);
                joint.RotationMaximum = ReadVector3(reader);
                joint.PositionSpring = ReadVector3(reader);
                joint.RotationSpring = ReadVector3(reader);

                Joints.Add(joint);
            }
        }

        private int _morphVertexCmp(NMMD_MorphVertexDesc x, NMMD_MorphVertexDesc y)
        {
            return x.VertexIndex.CompareTo(y.VertexIndex);
        }

        enum WeightDeformType
        {
            BDEF1 = 0,
            BDEF2 = 1,
            BDEF4 = 2,
            SDEF = 3,
            QDEF = 4
        }

        private int ReadIndex(BinaryReader reader, int size)
        {
            if (size == 1) return reader.ReadSByte();
            if (size == 2) return reader.ReadInt16();
            return reader.ReadInt32();
        }
        private int ReadIndexUnsigned(BinaryReader reader, int size)
        {
            if (size == 1) return reader.ReadByte();
            if (size == 2) return reader.ReadUInt16();
            return reader.ReadInt32();
        }

        private Vector2 ReadVector2(BinaryReader reader)
        {
            Vector2 vector2 = new Vector2();
            vector2.X = reader.ReadSingle();
            vector2.Y = reader.ReadSingle();
            return vector2;
        }
        private Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 vector3 = new Vector3();
            vector3.X = reader.ReadSingle();
            vector3.Y = reader.ReadSingle();
            vector3.Z = reader.ReadSingle();
            return vector3;
        }
        private Vector4 ReadVector4(BinaryReader reader)
        {
            Vector4 vector4 = new Vector4();
            vector4.X = reader.ReadSingle();
            vector4.Y = reader.ReadSingle();
            vector4.Z = reader.ReadSingle();
            vector4.W = reader.ReadSingle();
            return vector4;
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
        private string ReadString(BinaryReader reader, Encoding encoding)
        {
            int size = reader.ReadInt32();
            return encoding.GetString(reader.ReadBytes(size));
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
