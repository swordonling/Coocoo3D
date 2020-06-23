using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3D.MMDSupport;
using Coocoo3D.Components;
using Coocoo3DGraphics;

namespace Coocoo3D.Components
{
    public class MMDBoneComponent
    {
        public bool GpuUsable = false;
        public const int c_boneMatrixDataSize = 65536;
        public byte[] boneMatricesData = new byte[c_boneMatrixDataSize];
        GCHandle gch_boneMatricesData;
        public ConstantBuffer boneMatrices = new ConstantBuffer();

        public List<BoneEntity> bones = new List<BoneEntity>();
        public Dictionary<string, BoneEntity> stringBoneMap = new Dictionary<string, BoneEntity>();

        public List<MorphBoneStruct[]> boneMorphCache;

        public MMDBoneComponent()
        {
            gch_boneMatricesData = GCHandle.Alloc(boneMatricesData);
        }
        ~MMDBoneComponent()
        {
            gch_boneMatricesData.Free();
        }

        public void ComputeMatricesData()
        {
            Matrix4x4 matrix4X4 = Matrix4x4.Identity;
            int size = 64;
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(boneMatricesData, 0);
            for (int i = 0; i < bones.Count; i++)
            {
                matrix4X4 = Matrix4x4.Transpose(bones[i].GetTransformMatrixG(bones));
                Marshal.StructureToPtr(matrix4X4, ptr, true);

                ptr += size;
            }
        }
        public void SetPose(MMDMotionComponent motionComponent, MMDMorphStateComponent morphStateComponent, float time)
        {
            foreach (var pair in stringBoneMap)
            {
                if (motionComponent.BoneKeyFrameSet.TryGetValue(pair.Key, out var value))
                {
                    var keyframe = MMDMotionComponent.GetBoneMotion(value, time);
                    pair.Value.rotation = keyframe.rotation;
                    pair.Value.dynamicPosition = keyframe.translation;
                }
                else
                {
                    pair.Value.rotation = Quaternion.Identity;
                    pair.Value.dynamicPosition = Vector3.Zero;
                }
            }
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].IKTargetIndex != -1)
                {
                    IK(i, bones);
                }
            }

            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == MorphType.Bone && morphStateComponent.computedWeights[i] != morphStateComponent.prevComputedWeights[i])
                {
                    MorphBoneStruct[] morphBoneStructs = boneMorphCache[i];
                    MorphBoneStruct[] morphBoneStructs2 = morphStateComponent.morphs[i].MorphBones;
                    float computedWeight = morphStateComponent.computedWeights[i];
                    for (int j = 0; j < morphBoneStructs.Length; j++)
                    {
                        morphBoneStructs[j].Rotation = Quaternion.Slerp(Quaternion.Identity, morphBoneStructs2[j].Rotation, computedWeight);
                        morphBoneStructs[j].Translation = morphBoneStructs2[j].Translation * computedWeight;
                    }
                }
            }
            for (int i = 0; i < boneMorphCache.Count; i++)
            {
                if (boneMorphCache[i] == null) continue;
                MorphBoneStruct[] morphBoneStructs = boneMorphCache[i];
                for (int j = 0; j < morphBoneStructs.Length; j++)
                {
                    MorphBoneStruct morphBoneStruct = morphBoneStructs[j];
                    bones[morphBoneStruct.BoneIndex].rotation *= morphBoneStruct.Rotation;
                    bones[morphBoneStruct.BoneIndex].dynamicPosition += morphBoneStruct.Translation;
                }
            }

            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bone.AppendTranslation)
                {
                    bone.dynamicPosition += Vector3.Transform(bones[bone.AppendParentIndex].dynamicPosition, bones[bone.AppendParentIndex].GetParentRotation(bones)) * bone.AppendRatio;
                }
                if (bone.AppendRotation)
                {
                    bone.rotation *= Quaternion.Slerp(Quaternion.Identity, bones[bone.AppendParentIndex].rotation, bone.AppendRatio);
                }
            }
        }



        void IK(int index, List<BoneEntity> bones)
        {
            int ikTargetIndex = bones[index].IKTargetIndex;
            if (ikTargetIndex == -1) return;
            var entity = bones[index];
            var entitySource = bones[ikTargetIndex];

            entity.GetPositionAndRotation(bones, out var posTarget, out var rot0);


            bool skipOne = false;
            if (entity.boneIKLinks.Length == 2)
            {
                int cLimit = 0;
                int cIndex = 0;
                if (entity.boneIKLinks[0].HasLimit) { cLimit++; cIndex = 0; }
                if (entity.boneIKLinks[1].HasLimit) { cLimit++; cIndex = 1; }
                if (cLimit == 1)
                {
                    if (entitySource.ParentIndex == entity.boneIKLinks[cIndex].LinkedIndex)
                    {
                        BoneEntity ikS1 = bones[entity.boneIKLinks[1 - cIndex].LinkedIndex];
                        BoneEntity ikLimited = bones[entity.boneIKLinks[cIndex].LinkedIndex];
                        Quaternion cacheRot = ikS1.rotation;
                        ikS1.rotation = Quaternion.Identity;
                        ikS1.GetPositionAndRotation(bones, out Vector3 pos1, out Quaternion rot);
                        entitySource.GetPositionAndRotation(bones, out var posSource, out var rot1);
                        void _switchTo(Vector3 _angle)
                        {
                            switch (entity.IkTransformOrders[cIndex])
                            {
                                case IKTransformOrder.OrderZxy:
                                    ikLimited.rotation = Quaternion.Normalize(ZxyToQuaternion(_angle));
                                    break;
                                case IKTransformOrder.OrderXyz:
                                    ikLimited.rotation = Quaternion.Normalize(XyzToQuaternion(_angle));
                                    break;
                                case IKTransformOrder.OrderYzx:
                                    ikLimited.rotation = Quaternion.Normalize(YzxToQuaternion(_angle));
                                    break;
                            }
                            entitySource.GetPositionAndRotation(bones, out posSource, out rot1);
                        }
                        Vector3 LimitMax = entity.boneIKLinks[cIndex].LimitMax;
                        Vector3 LimitMin = entity.boneIKLinks[cIndex].LimitMin;

                        float targetDistance = Vector3.Distance(posTarget, pos1);
                        Vector3 minAng = entity.boneIKLinks[cIndex].LimitMin;
                        _switchTo(entity.boneIKLinks[cIndex].LimitMin);
                        float distMin = MathF.Abs(Vector3.Distance(posSource, pos1) - targetDistance);
                        for (int j = 0; j < 17; j++)
                        {
                            Vector3 eularAngle = Vector3.Lerp(LimitMin, LimitMax, j / 16.0f);
                            _switchTo(eularAngle);
                            float currentDDistance = MathF.Abs(Vector3.Distance(posSource, pos1) - targetDistance);
                            if (currentDDistance < distMin)
                            {
                                distMin = currentDDistance;
                                minAng = eularAngle;
                            }
                        }
                        void _toMin(int times, float sp)
                        {
                            Vector3 svec = (LimitMax - LimitMin) / sp;
                            for (int j = 0; j < times; j++)
                            {
                                Vector3 eularAngle = Vector3.Clamp(minAng + svec, LimitMin, LimitMax);
                                _switchTo(eularAngle);
                                float currentDDistance = MathF.Abs(Vector3.Distance(posSource, pos1) - targetDistance);
                                if (currentDDistance < distMin)
                                {
                                    distMin = currentDDistance;
                                    minAng = eularAngle;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            for (int j = 0; j < times; j++)
                            {
                                Vector3 eularAngle = Vector3.Clamp(minAng - svec, LimitMin, LimitMax);
                                _switchTo(eularAngle);
                                float currentDDistance = MathF.Abs(Vector3.Distance(posSource, pos1) - targetDistance);
                                if (currentDDistance < distMin)
                                {
                                    distMin = currentDDistance;
                                    minAng = eularAngle;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        _toMin(3, 32);
                        _toMin(3, 128);
                        _toMin(3, 512);
                        _toMin(3, 2048);
                        _toMin(3, 8192);
                        _toMin(3, 32768);
                        _toMin(3, 131072);
                        _switchTo(minAng);
                        ikS1.rotation = cacheRot;
                        skipOne = true;
                    }
                }
            }
            for (int i = 0; i < entity.CCDIterateLimit; i++)
            {
                for (int j = 0; j < entity.boneIKLinks.Length; j++)
                {
                    if (skipOne && entity.boneIKLinks[j].HasLimit) { skipOne = false; continue; }

                    BoneEntity itEntity = bones[entity.boneIKLinks[j].LinkedIndex];
                    entitySource.GetPositionAndRotation(bones, out var posSource, out var rot1);
                    Vector3 VSourceTarget = posTarget - posSource;
                    Quaternion rotationSnapshot = itEntity.rotation;
                    float lengthSnapshot = VSourceTarget.Length();
                    if (lengthSnapshot < 1e-3f) return;

                    itEntity.GetPositionAndRotation(bones, out var itPosition, out var itRot);

                    Vector3 targetDirection = Vector3.Normalize(itPosition - posTarget);
                    Vector3 ikDirection = Vector3.Normalize(itPosition - posSource);
                    if (Vector3.Dot(targetDirection, ikDirection) > 0.999999f) continue;


                    Vector3 ikRotateAxis = Vector3.Normalize(Vector3.Transform(Vector3.Cross(targetDirection, ikDirection),
                       Quaternion.Inverse(itRot / itEntity.rotation)));

                    itEntity.rotation = Quaternion.Normalize(itEntity.rotation * Quaternion.CreateFromAxisAngle(Vector3.Normalize(ikRotateAxis),
                         -(float)AngleBetween(targetDirection, ikDirection)));

                    var itl = entity.boneIKLinks[j];
                    if (itl.HasLimit)
                    {
                        switch (entity.IkTransformOrders[j])
                        {
                            case IKTransformOrder.OrderZxy:
                                {
                                    var eularAngle = QuaternionToZxy(itEntity.rotation);
                                    Vector3 cachedE = eularAngle;
                                    eularAngle = Vector3.Clamp(eularAngle, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != eularAngle)
                                        itEntity.rotation = Quaternion.Normalize(ZxyToQuaternion(eularAngle));
                                    break;
                                }
                            case IKTransformOrder.OrderXyz:
                                {
                                    var eularAngle = QuaternionToXyz(itEntity.rotation);
                                    Vector3 cachedE = eularAngle;
                                    eularAngle = Vector3.Clamp(eularAngle, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != eularAngle)
                                        itEntity.rotation = Quaternion.Normalize(XyzToQuaternion(eularAngle));
                                    break;
                                }
                            case IKTransformOrder.OrderYzx:
                                {
                                    var eularAngle = QuaternionToYzx(itEntity.rotation);
                                    Vector3 cachedE = eularAngle;
                                    eularAngle = Vector3.Clamp(eularAngle, itl.LimitMin, itl.LimitMax);
                                    if (cachedE != eularAngle)
                                        itEntity.rotation = Quaternion.Normalize(YzxToQuaternion(eularAngle));
                                    break;
                                }
                            default:
                                throw new NotImplementedException();
                        }
                        entitySource.GetPositionAndRotation(bones, out var tempPos, out var rot2);
                        if ((posTarget - tempPos).Length() > lengthSnapshot + 0.01f)//阻止乱转
                        {
                            itEntity.rotation = rotationSnapshot;
                        }
                    }
                }
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

        double[] m1 = new double[8];
        double[] m2 = new double[8];
        double AngleBetween(Vector3 a, Vector3 b)
        {
            m1[0] = a.X;
            m1[1] = a.Y;
            m1[2] = a.Z;
            m2[0] = b.X;
            m2[1] = b.Y;
            m2[2] = b.Z;
            Vector<double> v1 = new Vector<double>(m1);
            Vector<double> v2 = new Vector<double>(m2);
            return Math.Acos(Vector.Dot(v1, v2));
        }
        #endregion
    }
    public class BoneEntity
    {
        public int index;
        public Vector3 dynamicPosition;
        public Vector3 relativePosition;
        public Vector3 staticPosition;
        public Quaternion rotation;

        Matrix4x4 generatedTransform;
        public Matrix4x4 GeneratedTransform { get => generatedTransform; }

        public int ParentIndex = -1;
        public string Name;
        public string NameEN;

        public int IKTargetIndex = -1;
        public int CCDIterateLimit = 0;
        public float CCDAngleLimit = 0;
        public BoneIKLink[] boneIKLinks;
        public IKTransformOrder[] IkTransformOrders;

        public int AppendParentIndex = -1;
        public float AppendRatio;
        public bool AppendRotation;
        public bool AppendTranslation;
        public BoneFlags Flags;

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

        public void GetPositionAndRotation(List<BoneEntity> list, out Vector3 position, out Quaternion rotation)
        {
            if (ParentIndex != -1)
            {
                var parnet = list[ParentIndex];
                parnet.GetPositionAndRotation(list, out Vector3 pPos, out Quaternion pRot);
                position = pPos + Vector3.Transform(relativePosition + dynamicPosition, pRot);
                rotation = pRot * this.rotation;
            }
            else
            {
                position = relativePosition + dynamicPosition;
                rotation = this.rotation;
            }
        }
        /// <summary>在调用之前确保它的父级已经更新。一般从前向后调用即可。</summary>
        public Matrix4x4 GetTransformMatrixG(List<BoneEntity> list)
        {
            if (ParentIndex != -1)
            {
                generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                   Matrix4x4.CreateFromQuaternion(rotation) *
                   Matrix4x4.CreateTranslation(staticPosition + dynamicPosition) * list[ParentIndex].generatedTransform;
            }
            else
            {
                generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                   Matrix4x4.CreateFromQuaternion(rotation) *
                   Matrix4x4.CreateTranslation(staticPosition + dynamicPosition);
            }
            return generatedTransform;
        }
        public List<BoneEntity> Child = new List<BoneEntity>();

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
                IkTransformOrders = IkTransformOrders,
                generatedTransform = generatedTransform,
                Flags = Flags,
                AppendParentIndex = AppendParentIndex,
                AppendRatio = AppendRatio,
                AppendRotation = AppendRotation,
                AppendTranslation = AppendTranslation,
                Child = new List<BoneEntity>(Child),
            };
        }
        public override string ToString()
        {
            return string.Format("{0}_{1}", Name, NameEN);
        }
    }
    public enum IKTransformOrder
    {
        OrderYzx = 0,
        OrderZxy = 1,
        OrderXyz = 2,
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
            boneComponent.GpuUsable = false;
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
                    boneComponent.bones[boneEntity.ParentIndex].Child.Add(boneEntity);//不知道稳定不
                }
                else
                {
                    boneEntity.relativePosition = _bone.Position;
                }
                if (_bone.Flags.HasFlag(BoneFlags.HasIK))
                {
                    boneEntity.IKTargetIndex = _bone.boneIK.IKTargetIndex;
                    boneEntity.CCDIterateLimit = _bone.boneIK.CCDIterateLimit;
                    boneEntity.CCDAngleLimit = _bone.boneIK.CCDAngleLimit;
                    boneEntity.boneIKLinks = new BoneIKLink[_bone.boneIK.IKLinks.Length];
                    boneEntity.IkTransformOrders = new IKTransformOrder[_bone.boneIK.IKLinks.Length];
                    for (int j = 0; j < boneEntity.boneIKLinks.Length; j++)
                    {
                        boneEntity.boneIKLinks[j] = _bone.boneIK.IKLinks[j].GetCopy();
                        Vector3 tempMin = boneEntity.boneIKLinks[j].LimitMin;
                        Vector3 tempMax = boneEntity.boneIKLinks[j].LimitMax;
                        boneEntity.boneIKLinks[j].LimitMin = Vector3.Min(tempMin, tempMax);
                        boneEntity.boneIKLinks[j].LimitMax = Vector3.Max(tempMin, tempMax);

                        if (boneEntity.boneIKLinks[j].LimitMin.X > -Math.PI * 0.5 && boneEntity.boneIKLinks[j].LimitMax.X < Math.PI * 0.5)
                            boneEntity.IkTransformOrders[j] = IKTransformOrder.OrderZxy;
                        else if (boneEntity.boneIKLinks[j].LimitMin.Y > -Math.PI * 0.5 && boneEntity.boneIKLinks[j].LimitMax.Y < Math.PI * 0.5)
                            boneEntity.IkTransformOrders[j] = IKTransformOrder.OrderXyz;
                        else
                            boneEntity.IkTransformOrders[j] = IKTransformOrder.OrderYzx;

                    }
                }
                if (_bone.AppendBoneIndex >= 0 && _bone.AppendBoneIndex < _bones.Count)
                {
                    boneEntity.AppendParentIndex = _bone.AppendBoneIndex;
                    boneEntity.AppendRatio = _bone.AppendBoneRatio;
                    boneEntity.AppendRotation = _bone.Flags.HasFlag(BoneFlags.AcquireRotate);
                    boneEntity.AppendTranslation = _bone.Flags.HasFlag(BoneFlags.AcquireTranslate);
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


            int morphCount = modelResource.Morphs.Count;
            boneComponent.boneMorphCache = new List<MorphBoneStruct[]>();
            for (int i = 0; i < morphCount; i++)
            {
                if (modelResource.Morphs[i].Type == MorphType.Bone)
                {
                    MorphBoneStruct[] morphBoneStructs = new MorphBoneStruct[modelResource.Morphs[i].MorphBones.Length];
                    MorphBoneStruct[] morphBoneStructs2 = modelResource.Morphs[i].MorphBones;
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
