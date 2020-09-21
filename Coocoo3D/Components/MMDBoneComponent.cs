﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3D.MMDSupport;
using Coocoo3D.Components;
using Coocoo3DGraphics;
using Coocoo3DPhysics;
using Coocoo3DNativeInteroperable;

namespace Coocoo3D.Components
{
    public class MMDBoneComponent
    {
        public const int c_boneMatricesCount = 1020;
        public Matrix4x4[] boneMatricesData = new Matrix4x4[c_boneMatricesCount];
        public Matrix4x4[] boneMatricesData1 = new Matrix4x4[c_boneMatricesCount];
        GCHandle gch_boneMatricesData;
        GCHandle gch_boneMatricesData1;

        public List<BoneEntity> bones = new List<BoneEntity>();
        public Dictionary<string, BoneEntity> stringBoneMap = new Dictionary<string, BoneEntity>();

        public List<NMMD_MorphBoneDesc[]> boneMorphCache;
        public List<Physics3DRigidBody> physics3DRigidBodys = new List<Physics3DRigidBody>();
        public List<Physics3DJoint> physics3DJoints = new List<Physics3DJoint>();
        public List<NMMD_JointDesc> jointDescs = new List<NMMD_JointDesc>();
        public List<NMMD_RigidBodyDesc> rigidBodyDescs = new List<NMMD_RigidBodyDesc>();

        public Matrix4x4 LocalToWorld = Matrix4x4.Identity;
        public Matrix4x4 WorldToLocal = Matrix4x4.Identity;

        public Dictionary<int, List<List<int>>> IKNeedUpdateMatIndexs;
        public List<int> AppendNeedUpdateMatIndexs = new List<int>();
        public List<int> PhysicsNeedUpdateMatIndexs = new List<int>();

        public MMDBoneComponent()
        {
            gch_boneMatricesData = GCHandle.Alloc(boneMatricesData);
            gch_boneMatricesData1 = GCHandle.Alloc(boneMatricesData1);
        }
        ~MMDBoneComponent()
        {
            gch_boneMatricesData.Free();
            gch_boneMatricesData1.Free();
        }

        public void ComputeMatricesData()
        {
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].GetTransformMatrixG(bones);
            }
        }
        public void WriteMatriticesData()
        {
            for (int i = 0; i < bones.Count; i++)
            {
                boneMatricesData[i] = Matrix4x4.Transpose(bones[i].GeneratedTransform);
            }
        }
        public void SetPose(MMDMotionComponent motionComponent, MMDMorphStateComponent morphStateComponent, float time)
        {
            foreach (var pair in stringBoneMap)
            {
                var keyframe = motionComponent.GetBoneMotion(pair.Key, time);
                pair.Value.rotation = keyframe.rotation;
                pair.Value.dynamicPosition = keyframe.translation;
            }
            UpdateAllMatrix();

            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == NMMDE_MorphType.Bone && morphStateComponent.computedWeights[i] != morphStateComponent.prevComputedWeights[i])
                {
                    NMMD_MorphBoneDesc[] morphBoneStructs0 = boneMorphCache[i];
                    NMMD_MorphBoneDesc[] morphBoneStructs1 = morphStateComponent.morphs[i].MorphBones;
                    float computedWeight = morphStateComponent.computedWeights[i];
                    for (int j = 0; j < morphBoneStructs0.Length; j++)
                    {
                        morphBoneStructs0[j].Rotation = Quaternion.Slerp(Quaternion.Identity, morphBoneStructs1[j].Rotation, computedWeight);
                        morphBoneStructs0[j].Translation = morphBoneStructs1[j].Translation * computedWeight;
                    }
                }
            }

            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].IKTargetIndex != -1)
                {
                    IK(i, bones);
                }
            }
            for (int i = 0; i < boneMorphCache.Count; i++)
            {
                if (boneMorphCache[i] == null) continue;
                NMMD_MorphBoneDesc[] morphBoneStructs = boneMorphCache[i];
                for (int j = 0; j < morphBoneStructs.Length; j++)
                {
                    NMMD_MorphBoneDesc morphBoneStruct = morphBoneStructs[j];
                    bones[morphBoneStruct.BoneIndex].rotation *= morphBoneStruct.Rotation;
                    bones[morphBoneStruct.BoneIndex].dynamicPosition += morphBoneStruct.Translation;
                }
            }

            //for (int i = 0; i < bones.Count; i++)
            //{
            //    var bone = bones[i];
            //    if (bone.AppendTranslation)
            //    {
            //        bone.dynamicPosition += Vector3.Transform(bones[bone.AppendParentIndex].dynamicPosition, bones[bone.AppendParentIndex].GetParentRotation(bones)) * bone.AppendRatio;
            //    }
            //    if (bone.AppendRotation)
            //    {
            //        bone.rotation *= Quaternion.Slerp(Quaternion.Identity, bones[bone.AppendParentIndex].rotation, bone.AppendRatio);
            //    }
            //}
        }

        public void SetPhysicsPose(Physics3DScene physics3DScene)
        {
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type != 0) continue;
                int index = desc.AssociatedBoneIndex;
                var mat1 = bones[index].GeneratedTransform * LocalToWorld;
                Matrix4x4.Decompose(mat1, out _, out var rot, out _);

                physics3DScene.MoveRigidBody(physics3DRigidBodys[i], Vector3.Transform(desc.Position, mat1), rot * desc.Rotation);
            }
        }

        public void SetPoseAfterPhysics(Physics3DScene physics3DScene)
        {
            Matrix4x4.Decompose(WorldToLocal, out _, out var q1, out var t1);
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type == 0) continue;
                int index = desc.AssociatedBoneIndex;
                if (index == -1) continue;
                bones[index]._generatedTransform = Matrix4x4.CreateTranslation(-bones[index].staticPosition) * Matrix4x4.CreateFromQuaternion(Translate(physics3DScene.GetRigidBodyRotation(physics3DRigidBodys[i]) / desc.Rotation * q1))
                    * Matrix4x4.CreateTranslation(Vector3.Transform(physics3DScene.GetRigidBodyPosition(physics3DRigidBodys[i]), WorldToLocal) - desc.Position + bones[index].staticPosition);

                //Matrix4x4 x4 = physics3DScene.GetRigidBodyTransform(physics3DRigidBodys[i]) * WorldToLocal;
                //bones[index]._generatedTransform = Matrix4x4.CreateTranslation(-desc.Position) * x4;
            }

            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bone.AppendTranslation || bone.AppendRotation)
                {
                    var mat1 = bones[bone.AppendParentIndex].GeneratedTransform;
                    Matrix4x4.Decompose(mat1, out _, out var rot, out var tran);
                    if (bone.AppendTranslation)
                    {
                        bone.dynamicPosition += tran * bone.AppendRatio;
                    }
                    if (bone.AppendRotation)
                    {
                        bone.rotation *= Quaternion.Slerp(Quaternion.Identity, bones[bone.AppendParentIndex].rotation, bone.AppendRatio);
                    }
                }
            }
            UpdateMatrixs(PhysicsNeedUpdateMatIndexs);
            UpdateMatrixs(AppendNeedUpdateMatIndexs);
        }

        void IK(int index, List<BoneEntity> bones)
        {
            int ikTargetIndex = bones[index].IKTargetIndex;
            if (ikTargetIndex == -1) return;
            var entity = bones[index];
            var entitySource = bones[ikTargetIndex];

            entity.GetPosRot2(out var posTarget, out var rot0);


            var ax = IKNeedUpdateMatIndexs[index];
            int h1 = entity.CCDIterateLimit / 2;
            Vector3 posSource = entitySource.GetPos2();
            if ((posTarget - posSource).LengthSquared() < 1e-8f) return;
            for (int i = 0; i < entity.CCDIterateLimit; i++)
            {
                bool axis_lim = i < h1;
                for (int j = 0; j < entity.boneIKLinks.Length; j++)
                {
                    posSource = entitySource.GetPos2();
                    BoneEntity itEntity = bones[entity.boneIKLinks[j].LinkedIndex];

                    itEntity.GetPosRot2(out var itPosition, out var itRot);

                    Vector3 targetDirection = Vector3.Normalize(itPosition - posTarget);
                    Vector3 ikDirection = Vector3.Normalize(itPosition - posSource);
                    float dotV = Vector3.Dot(targetDirection, ikDirection);
                    dotV = Math.Clamp(dotV, -1, 1);

                    Matrix4x4 matXi = Matrix4x4.Transpose(itEntity.GeneratedTransform);
                    Vector3 ikRotateAxis = SafeNormalize(Vector3.TransformNormal(Vector3.Cross(targetDirection, ikDirection), matXi));

                    var itl = entity.boneIKLinks[j];
                    if (axis_lim)
                        switch (itl.FixTypes)
                        {
                            case AxisFixType.FixX:
                                ikRotateAxis.X = ikRotateAxis.X >= 0 ? 1 : -1;
                                ikRotateAxis.Y = 0;
                                ikRotateAxis.Z = 0;
                                break;
                            case AxisFixType.FixY:
                                ikRotateAxis.Y = ikRotateAxis.Y >= 0 ? 1 : -1;
                                ikRotateAxis.X = 0;
                                ikRotateAxis.Z = 0;
                                break;
                            case AxisFixType.FixZ:
                                ikRotateAxis.Z = ikRotateAxis.Z >= 0 ? 1 : -1;
                                ikRotateAxis.X = 0;
                                ikRotateAxis.Y = 0;
                                break;
                        }

                    float ikAngle = Math.Min((float)Math.Acos(dotV), entity.CCDAngleLimit * (i + 1));

                    itEntity.rotation = Quaternion.Normalize(itEntity.rotation * Quaternion.CreateFromAxisAngle(ikRotateAxis, -ikAngle));

                    if (itl.HasLimit)
                    {
                        Vector3 angle = Vector3.Zero;
                        switch (entity.boneIKLinks[j].TransformOrder)
                        {
                            case IKTransformOrder.Zxy:
                                {
                                    angle = QuaternionToZxy(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle.Z) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle.X) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle.Y));
                                    break;
                                }
                            case IKTransformOrder.Xyz:
                                {
                                    angle = QuaternionToXyz(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle.X) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle.Y) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle.Z));
                                    break;
                                }
                            case IKTransformOrder.Yzx:
                                {
                                    angle = QuaternionToYzx(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle.Y) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle.Z) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle.X));
                                    break;
                                }
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    UpdateMatrixs(ax[j]);
                }
                posSource = entitySource.GetPos2();
                if ((posTarget - posSource).LengthSquared() < 1e-8f) return;
            }
        }

        public void ResetPhysics(Physics3DScene physics3DScene)
        {
            ComputeMatricesData();
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type == 0) continue;
                int index = desc.AssociatedBoneIndex;
                if (index == -1) continue;
                var mat1 = bones[index].GeneratedTransform * LocalToWorld;
                Matrix4x4.Decompose(mat1, out _, out var rot, out _);
                physics3DScene.ResetRigidBody(physics3DRigidBodys[i], Vector3.Transform(desc.Position, mat1), rot * desc.Rotation);
            }
        }

        public void BakeSequenceProcessMatrixsIndex()
        {
            IKNeedUpdateMatIndexs = new Dictionary<int, List<List<int>>>();
            bool[] testArray = new bool[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                int ikTargetIndex = bones[i].IKTargetIndex;
                if (ikTargetIndex != -1)
                {
                    List<List<int>> ax = new List<List<int>>();
                    var entity = bones[i];
                    var entitySource = bones[ikTargetIndex];
                    for (int j = 0; j < entity.boneIKLinks.Length; j++)
                    {
                        List<int> bx = new List<int>();

                        Array.Clear(testArray, 0, bones.Count);
                        testArray[entity.boneIKLinks[j].LinkedIndex] = true;
                        for (int k = 0; k < bones.Count; k++)
                        {
                            if (bones[k].ParentIndex != -1)
                            {
                                testArray[k] |= testArray[bones[k].ParentIndex];
                                if (testArray[k])
                                {
                                    bx.Add(k);
                                }
                            }
                        }
                        ax.Add(bx);
                    }
                    IKNeedUpdateMatIndexs[i] = ax;
                }
            }
            Array.Clear(testArray, 0, bones.Count);
            AppendNeedUpdateMatIndexs.Clear();
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bones[i].ParentIndex != -1)
                    testArray[i] |= testArray[bones[i].ParentIndex];
                testArray[i] |= bone.AppendTranslation || bone.AppendRotation;
                if (testArray[i])
                {
                    AppendNeedUpdateMatIndexs.Add(i);
                }
            }
            Array.Clear(testArray, 0, bones.Count);
            PhysicsNeedUpdateMatIndexs.Clear();
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bones[i].ParentIndex == -1)
                    continue;
                var parent = bones[bones[i].ParentIndex];
                testArray[i] |= testArray[bones[i].ParentIndex];
                testArray[i] |= parent.IsPhysicsFreeBone;
                if (testArray[i])
                {
                    PhysicsNeedUpdateMatIndexs.Add(i);
                }
            }
        }

        void UpdateAllMatrix()
        {
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].GetTransformMatrixG(bones);
            }
        }
        void UpdateMatrixs(List<int> indexs)
        {
            for (int i = 0; i < indexs.Count; i++)
            {
                bones[indexs[i]].GetTransformMatrixG(bones);
            }
        }
        public void TransformToNew(Physics3DScene physics3DScene, Vector3 position, Quaternion rotation)
        {
            LocalToWorld = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
            Matrix4x4.Invert(LocalToWorld, out WorldToLocal);
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type != 0) continue;
                int index = desc.AssociatedBoneIndex;
                var bone = bones[index];
                Matrix4x4.Decompose(bone.GeneratedTransform, out _, out Quaternion rot, out Vector3 trans);
                Vector3 pos = Vector3.Transform(bone.staticPosition, bone.GeneratedTransform * LocalToWorld);
                physics3DScene.MoveRigidBody(physics3DRigidBodys[i], pos, rot);
            }
        }

        public void AddPhysics(Physics3DScene physics3DScene)
        {
            for (int j = 0; j < rigidBodyDescs.Count; j++)
            {
                var desc = rigidBodyDescs[j];
                physics3DScene.AddRigidBody(physics3DRigidBodys[j], desc.Position, desc.Rotation, desc.Dimemsions, desc.Mass, desc.Restitution, desc.Friction, desc.TranslateDamp, desc.RotateDamp, (byte)desc.Shape, (byte)desc.Type, desc.CollisionGroup, desc.CollisionMask);
            }
            for (int j = 0; j < jointDescs.Count; j++)
            {
                var desc = jointDescs[j];
                physics3DScene.AddJoint(physics3DJoints[j], desc.Position, MMDBoneComponent.ToQuaternion(desc.Rotation), physics3DRigidBodys[desc.AssociatedRigidBodyIndex1], physics3DRigidBodys[desc.AssociatedRigidBodyIndex2],
                    desc.PositionMinimum, desc.PositionMaximum, desc.RotationMinimum, desc.RotationMaximum, desc.PositionSpring, desc.RotationSpring);
            }
        }

        public void RemovePhysics(Physics3DScene physics3DScene)
        {
            for (int j = 0; j < physics3DRigidBodys.Count; j++)
            {
                physics3DScene.RemoveRigidBody(physics3DRigidBodys[j]);
            }
            for (int j = 0; j < physics3DJoints.Count; j++)
            {
                physics3DScene.RemoveJoint(physics3DJoints[j]);
            }
        }

        #region helpers
        static Vector3 QuaternionToXyz(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei - jk), 1 - 2.0f * (ii + jj));
            result.Y = (float)Math.Asin(2.0f * (ej + ik));
            result.Z = (float)Math.Atan2(2.0f * (ek - ij), 1 - 2.0f * (jj + kk));
            return result;
        }
        static Vector3 QuaternionToXzy(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei + jk), 1 - 2.0f * (ii + kk));
            result.Y = (float)Math.Atan2(2.0f * (ej + ik), 1 - 2.0f * (jj + kk));
            result.Z = (float)Math.Asin(2.0f * (ek - ij));
            return result;
        }
        static Vector3 QuaternionToYxz(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Asin(2.0f * (ei - jk));
            result.Y = (float)Math.Atan2(2.0f * (ej + ik), 1 - 2.0f * (ii + jj));
            result.Z = (float)Math.Atan2(2.0f * (ek + ij), 1 - 2.0f * (ii + kk));
            return result;
        }
        static Vector3 QuaternionToYzx(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei - jk), 1 - 2.0f * (ii + kk));
            result.Y = (float)Math.Atan2(2.0f * (ej - ik), 1 - 2.0f * (jj + kk));
            result.Z = (float)Math.Asin(2.0f * (ek + ij));
            return result;
        }
        static Vector3 QuaternionToZxy(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Asin(2.0f * (ei + jk));
            result.Y = (float)Math.Atan2(2.0f * (ej - ik), 1 - 2.0f * (ii + jj));
            result.Z = (float)Math.Atan2(2.0f * (ek - ij), 1 - 2.0f * (ii + kk));
            return result;
        }
        static Vector3 QuaternionToZyx(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei + jk), 1 - 2.0f * (ii + jj));
            result.Y = (float)Math.Asin(2.0f * (ej - ik));
            result.Z = (float)Math.Atan2(2.0f * (ek + ij), 1 - 2.0f * (jj + kk));
            return result;
        }

        static Quaternion XyzToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(sx * sy * cz + cx * cy * sz);
            return result;
        }
        static Quaternion XzyToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(cx * cy * sz + sx * sy * cz);
            return result;
        }
        static Quaternion YxzToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }
        static Quaternion YzxToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }
        static Quaternion ZxyToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz + sx * sy * cz);
            return result;
        }
        static Quaternion ZYXToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }


        public static Quaternion ToQuaternion(Vector3 angle)
        {
            return Quaternion.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z);
        }

        public static Quaternion Translate(Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        private Vector3 LimitAngle(Vector3 angle, bool axis_lim, Vector3 low, Vector3 high)
        {
            if (!axis_lim)
            {
                return Vector3.Clamp(angle, low, high);
            }
            Vector3 vecL1 = 2.0f * low - angle;
            Vector3 vecH1 = 2.0f * high - angle;
            if (angle.X < low.X)
            {
                angle.X = (vecL1.X <= high.X) ? vecL1.X : low.X;
            }
            else if (angle.X > high.X)
            {
                angle.X = (vecH1.X >= low.X) ? vecH1.X : high.X;
            }
            if (angle.Y < low.Y)
            {
                angle.Y = (vecL1.Y <= high.Y) ? vecL1.Y : low.Y;
            }
            else if (angle.Y > high.Y)
            {
                angle.Y = (vecH1.Y >= low.Y) ? vecH1.Y : high.Y;
            }
            if (angle.Z < low.Z)
            {
                angle.Z = (vecL1.Z <= high.Z) ? vecL1.Z : low.Z;
            }
            else if (angle.Z > high.Z)
            {
                angle.Z = (vecH1.Z >= low.Z) ? vecH1.Z : high.Z;
            }
            return angle;
        }

        Vector3 SafeNormalize(Vector3 vector3)
        {
            float dp3 = Math.Max(0.00001f, Vector3.Dot(vector3, vector3));
            return vector3 / MathF.Sqrt(dp3);
        }
        #endregion
        public static NMMD_RigidBodyDesc GetRigidBodyDesc(MMDRigidBody rigidBody)
        {
            NMMD_RigidBodyDesc desc = new NMMD_RigidBodyDesc();
            desc.AssociatedBoneIndex = rigidBody.AssociatedBoneIndex;
            desc.CollisionGroup = rigidBody.CollisionGroup;
            desc.CollisionMask = rigidBody.CollisionMask;
            desc.Shape = rigidBody.Shape;
            desc.Dimemsions = rigidBody.Dimemsions;
            desc.Position = rigidBody.Position;
            desc.Rotation = ToQuaternion(rigidBody.Rotation);
            desc.Mass = rigidBody.Mass;
            desc.TranslateDamp = rigidBody.TranslateDamp;
            desc.RotateDamp = rigidBody.RotateDamp;
            desc.Restitution = rigidBody.Restitution;
            desc.Friction = rigidBody.Friction;
            desc.Type = rigidBody.Type;
            return desc;
        }
        public static NMMD_JointDesc GetJointDesc(MMDJoint joint)
        {
            NMMD_JointDesc desc = new NMMD_JointDesc();
            desc.Type = joint.Type;
            desc.AssociatedRigidBodyIndex1 = joint.AssociatedRigidBodyIndex1;
            desc.AssociatedRigidBodyIndex2 = joint.AssociatedRigidBodyIndex2;
            desc.Position = joint.Position;
            desc.Rotation = joint.Rotation;
            desc.PositionMinimum = joint.PositionMinimum;
            desc.PositionMaximum = joint.PositionMaximum;
            desc.RotationMinimum = joint.RotationMinimum;
            desc.RotationMaximum = joint.RotationMaximum;
            desc.PositionSpring = joint.PositionSpring;
            desc.RotationSpring = joint.RotationSpring;
            return desc;
        }
    }
    public class BoneEntity
    {
        public int index;
        public Vector3 dynamicPosition;
        public Vector3 relativePosition;
        public Vector3 staticPosition;
        public Quaternion rotation;

        public Matrix4x4 _generatedTransform = Matrix4x4.Identity;
        public Matrix4x4 GeneratedTransform { get => _generatedTransform; }

        public int ParentIndex = -1;
        public string Name;
        public string NameEN;

        public int IKTargetIndex = -1;
        public int CCDIterateLimit = 0;
        public float CCDAngleLimit = 0;
        public IKLink[] boneIKLinks;

        public int AppendParentIndex = -1;
        public float AppendRatio;
        public bool AppendRotation;
        public bool AppendTranslation;
        public bool IsPhysicsFreeBone;
        public NMMDE_BoneFlag Flags;

        public Quaternion GetRotation(List<BoneEntity> list)
        {
            if (ParentIndex != -1)
            {
                var parnet = list[ParentIndex];
                return parnet.GetRotation(list) * rotation;
            }
            else
                return rotation;
        }
        public Quaternion GetParentRotation(List<BoneEntity> list)
        {
            if (ParentIndex != -1)
            {
                var parnet = list[ParentIndex];
                return Quaternion.Normalize(parnet.GetRotation(list));
            }
            else
                return Quaternion.Identity;
        }

        /// <summary>在调用之前确保它的父级已经更新。一般从前向后调用即可。</summary>
        public void GetTransformMatrixG(List<BoneEntity> list)
        {
            if (ParentIndex != -1)
            {
                _generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                   Matrix4x4.CreateFromQuaternion(rotation) *
                   Matrix4x4.CreateTranslation(staticPosition + dynamicPosition) * list[ParentIndex]._generatedTransform;
            }
            else
            {
                _generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                   Matrix4x4.CreateFromQuaternion(rotation) *
                   Matrix4x4.CreateTranslation(staticPosition + dynamicPosition);
            }
        }
        public Vector3 GetPos2()
        {
            return Vector3.Transform(staticPosition, _generatedTransform);
        }

        public void GetPosRot2(out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.Transform(staticPosition, _generatedTransform);
            Matrix4x4.Decompose(_generatedTransform, out _, out rot, out _);
        }
        public BoneEntity GetCopy()
        {
            return new BoneEntity()
            {
                index = index,
                dynamicPosition = dynamicPosition,
                relativePosition = relativePosition,
                staticPosition = staticPosition,
                rotation = rotation,
                ParentIndex = ParentIndex,
                CCDAngleLimit = CCDAngleLimit,
                CCDIterateLimit = CCDIterateLimit,
                Name = Name,
                NameEN = NameEN,
                IKTargetIndex = IKTargetIndex,
                boneIKLinks = boneIKLinks,
                _generatedTransform = _generatedTransform,
                Flags = Flags,
                AppendParentIndex = AppendParentIndex,
                AppendRatio = AppendRatio,
                AppendRotation = AppendRotation,
                AppendTranslation = AppendTranslation,
            };
        }
        public struct IKLink
        {
            public int LinkedIndex;
            public bool HasLimit;
            public Vector3 LimitMin;
            public Vector3 LimitMax;
            public IKTransformOrder TransformOrder;
            public AxisFixType FixTypes;
        }
        public override string ToString()
        {
            return string.Format("{0}_{1}", Name, NameEN);
        }
    }
    public enum IKTransformOrder
    {
        Yzx = 0,
        Zxy = 1,
        Xyz = 2,
    }

    public enum AxisFixType
    {
        FixNone,
        FixX,
        FixY,
        FixZ,
        FixAll
    }

}


namespace Coocoo3D.FileFormat
{
    public static partial class PMXFormatExtension
    {
        public static MMDBoneComponent LoadBoneComponent(PMXFormat source)
        {
            MMDBoneComponent boneComponent = new MMDBoneComponent();
            boneComponent.Reload(source);
            return boneComponent;
        }

        public static void Reload(this MMDBoneComponent boneComponent, PMXFormat modelResource)
        {
            boneComponent.bones.Clear();
            boneComponent.stringBoneMap.Clear();
            boneComponent.physics3DRigidBodys.Clear();
            var _bones = modelResource.Bones;
            for (int i = 0; i < _bones.Count; i++)
            {
                var _bone = _bones[i];
                BoneEntity boneEntity = new BoneEntity();
                boneEntity.ParentIndex = (_bone.ParentIndex >= 0 && _bone.ParentIndex < _bones.Count) ? _bone.ParentIndex : -1;
                boneEntity.staticPosition = _bone.Position;
                boneEntity.rotation = Quaternion.Identity;
                boneEntity.index = i;

                boneEntity.Name = _bone.Name;
                boneEntity.NameEN = _bone.NameEN;
                boneEntity.Flags = _bone.Flags;

                if (boneEntity.ParentIndex != -1)
                {
                    boneEntity.relativePosition = _bone.Position - _bones[boneEntity.ParentIndex].Position;
                }
                else
                {
                    boneEntity.relativePosition = _bone.Position;
                }
                if (_bone.Flags.HasFlag(NMMDE_BoneFlag.HasIK))
                {
                    boneEntity.IKTargetIndex = _bone.boneIK.IKTargetIndex;
                    boneEntity.CCDIterateLimit = _bone.boneIK.CCDIterateLimit;
                    boneEntity.CCDAngleLimit = _bone.boneIK.CCDAngleLimit;
                    boneEntity.boneIKLinks = new BoneEntity.IKLink[_bone.boneIK.IKLinks.Length];
                    for (int j = 0; j < boneEntity.boneIKLinks.Length; j++)
                    {
                        var ikLink = new BoneEntity.IKLink();
                        ikLink.HasLimit = _bone.boneIK.IKLinks[j].HasLimit;
                        ikLink.LimitMax = _bone.boneIK.IKLinks[j].LimitMax;
                        ikLink.LimitMin = _bone.boneIK.IKLinks[j].LimitMin;
                        ikLink.LinkedIndex = _bone.boneIK.IKLinks[j].LinkedIndex;


                        Vector3 tempMin = ikLink.LimitMin;
                        Vector3 tempMax = ikLink.LimitMax;
                        ikLink.LimitMin = Vector3.Min(tempMin, tempMax);
                        ikLink.LimitMax = Vector3.Max(tempMin, tempMax);

                        if (ikLink.LimitMin.X > -Math.PI * 0.5 && ikLink.LimitMax.X < Math.PI * 0.5)
                            ikLink.TransformOrder = IKTransformOrder.Zxy;
                        else if (ikLink.LimitMin.Y > -Math.PI * 0.5 && ikLink.LimitMax.Y < Math.PI * 0.5)
                            ikLink.TransformOrder = IKTransformOrder.Xyz;
                        else
                            ikLink.TransformOrder = IKTransformOrder.Yzx;
                        const float epsilon = 1e-6f;
                        if (ikLink.HasLimit)
                        {
                            if (Math.Abs(ikLink.LimitMin.X) < epsilon &&
                                Math.Abs(ikLink.LimitMax.X) < epsilon
                                && Math.Abs(ikLink.LimitMin.Y) < epsilon &&
                                Math.Abs(ikLink.LimitMax.Y) < epsilon
                                && Math.Abs(ikLink.LimitMin.Z) < epsilon &&
                                Math.Abs(ikLink.LimitMax.Z) < epsilon)
                            {
                                ikLink.FixTypes = AxisFixType.FixAll;
                            }
                            else if (Math.Abs(ikLink.LimitMin.Y) < epsilon &&
                                     Math.Abs(ikLink.LimitMax.Y) < epsilon
                                     && Math.Abs(ikLink.LimitMin.Z) < epsilon &&
                                     Math.Abs(ikLink.LimitMax.Z) < epsilon)
                            {
                                ikLink.FixTypes = AxisFixType.FixX;
                            }
                            else if (Math.Abs(ikLink.LimitMin.X) < epsilon &&
                                     Math.Abs(ikLink.LimitMin.X) < epsilon &&
                                     Math.Abs(ikLink.LimitMin.Z) < epsilon &&
                                     Math.Abs(ikLink.LimitMax.Z) < epsilon)
                            {
                                ikLink.FixTypes = AxisFixType.FixY;
                            }
                            else if (Math.Abs(ikLink.LimitMin.X) < epsilon &&
                                     Math.Abs(ikLink.LimitMin.X) < epsilon
                                     && Math.Abs(ikLink.LimitMin.Y) < epsilon &&
                                     Math.Abs(ikLink.LimitMax.Y) < epsilon)
                            {
                                ikLink.FixTypes = AxisFixType.FixZ;
                            }
                        }

                        boneEntity.boneIKLinks[j] = ikLink;
                    }
                }
                if (_bone.AppendBoneIndex >= 0 && _bone.AppendBoneIndex < _bones.Count)
                {
                    boneEntity.AppendParentIndex = _bone.AppendBoneIndex;
                    boneEntity.AppendRatio = _bone.AppendBoneRatio;
                    boneEntity.AppendRotation = _bone.Flags.HasFlag(NMMDE_BoneFlag.AcquireRotate);
                    boneEntity.AppendTranslation = _bone.Flags.HasFlag(NMMDE_BoneFlag.AcquireTranslate);
                }
                else
                {
                    boneEntity.AppendParentIndex = -1;
                    boneEntity.AppendRatio = 0;
                    boneEntity.AppendRotation = false;
                    boneEntity.AppendTranslation = false;
                }
                boneComponent.bones.Add(boneEntity);
                if (boneComponent.stringBoneMap.ContainsKey(_bone.Name)) continue;
                boneComponent.stringBoneMap.Add(_bone.Name, boneEntity);
            }

            boneComponent.BakeSequenceProcessMatrixsIndex();

            var rigidBodys = modelResource.RigidBodies;
            for (int i = 0; i < rigidBodys.Count; i++)
            {
                var rigidBodyData = rigidBodys[i];
                Physics3DRigidBody physics3DRigidBody = new Physics3DRigidBody();
                boneComponent.physics3DRigidBodys.Add(physics3DRigidBody);
                boneComponent.rigidBodyDescs.Add(MMDBoneComponent.GetRigidBodyDesc(rigidBodyData));
                if (rigidBodyData.Type != NMMDE_RigidBodyType.Kinematic && rigidBodyData.AssociatedBoneIndex != -1)
                    boneComponent.bones[rigidBodyData.AssociatedBoneIndex].IsPhysicsFreeBone = true;
            }
            var joints = modelResource.Joints;
            for (int i = 0; i < joints.Count; i++)
            {
                boneComponent.jointDescs.Add(MMDBoneComponent.GetJointDesc(joints[i]));
                boneComponent.physics3DJoints.Add(new Physics3DJoint());
            }

            int morphCount = modelResource.Morphs.Count;
            boneComponent.boneMorphCache = new List<NMMD_MorphBoneDesc[]>();
            for (int i = 0; i < morphCount; i++)
            {
                if (modelResource.Morphs[i].Type == NMMDE_MorphType.Bone)
                {
                    NMMD_MorphBoneDesc[] morphBoneStructs = new NMMD_MorphBoneDesc[modelResource.Morphs[i].MorphBones.Length];
                    NMMD_MorphBoneDesc[] morphBoneStructs2 = modelResource.Morphs[i].MorphBones;
                    for (int j = 0; j < morphBoneStructs.Length; j++)
                    {
                        morphBoneStructs[j].BoneIndex = morphBoneStructs2[j].BoneIndex;
                        morphBoneStructs[j].Rotation = Quaternion.Identity;
                    }
                    boneComponent.boneMorphCache.Add(morphBoneStructs);
                }
                else
                {
                    boneComponent.boneMorphCache.Add(null);
                }
            }
        }
    }
}
