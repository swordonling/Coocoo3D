using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3D.Components;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using Coocoo3D.Core;
using System.ComponentModel;

namespace Coocoo3D.Present
{
    public class MMD3DEntity : ISceneObject, INotifyPropertyChanged
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 PositionNextFrame;
        public Quaternion RotationNextFrame;
        public bool NeedTransform;

        public string Name;
        public string Description;

        public MMDRendererComponent rendererComponent = new MMDRendererComponent();
        public MMDBoneComponent boneComponent = new MMDBoneComponent();
        public MMDMotionComponent motionComponent = new MMDMotionComponent();
        public MMDMorphStateComponent morphStateComponent = new MMDMorphStateComponent();

        public bool RenderReady = false;
        public bool ComponentReady = false;
        public volatile bool needUpdateMotion = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public void PropChange(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        //重新加载不依赖其他实例的资源，仅用于简化代码。
        public void ReloadBase()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Name = "";
            Description = "";
        }

        public void SetMotionTime(float time)
        {
            if (!ComponentReady) return;
            morphStateComponent.SetPose(motionComponent, time);
            morphStateComponent.ComputeWeight();
            boneComponent.SetPose(motionComponent, morphStateComponent, time);
            boneComponent.ComputeMatricesData();
            rendererComponent.SetPose(morphStateComponent);
            needUpdateMotion = true;
        }

        public void UpdateGpuResources(GraphicsContext graphicsContext)
        {
            rendererComponent.UpdateGPUResources(graphicsContext);
            if (!boneComponent.GpuUsable)
            {
                boneComponent.WriteMatriticesData();
                boneComponent.GpuUsable = true;
                needUpdateMotion = true;
            }
            if (needUpdateMotion)
            {
                graphicsContext.UpdateResource(boneComponent.boneMatricesBuffer, boneComponent.boneMatricesData, Components.MMDBoneComponent.c_boneMatrixDataSize, 0);
                needUpdateMotion = false;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
namespace Coocoo3D.FileFormat
{
    public partial class PMXFormat
    {
        const int c_vertexStride = 68;
        const int c_vertexStride2 = 12;
        const int c_vertexFront = 36;
        const int c_indexStride = 4;
        byte[] verticesData;
        GCHandle gch_verticesData;
        byte[] verticesData2;
        GCHandle gch_verticesData2;
        byte[] indexsData;
        GCHandle gch_indexsData;

        public void Reload2()
        {
            if (gch_verticesData.IsAllocated) gch_verticesData.Free();
            if (gch_verticesData2.IsAllocated) gch_verticesData2.Free();
            if (gch_indexsData.IsAllocated) gch_indexsData.Free();
            verticesData = new byte[Vertices.Count * c_vertexStride];
            verticesData2 = new byte[Vertices.Count * c_vertexStride2];
            indexsData = new byte[TriangleIndexs.Count * 4];
            gch_verticesData = GCHandle.Alloc(verticesData);
            gch_verticesData2 = GCHandle.Alloc(verticesData2);
            gch_indexsData = GCHandle.Alloc(indexsData);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(verticesData, 0);
            for (int i = 0; i < Vertices.Count; i++)
            {
                Marshal.StructureToPtr(Vertices[i].innerStruct, ptr, true);
                Marshal.Copy(Vertices[i].boneId, 0, ptr + 24, 4);
                Marshal.Copy(Vertices[i].weight, 0, ptr + 24 + 16, 4);
                ptr += c_vertexStride;
            }
            ptr = Marshal.UnsafeAddrOfPinnedArrayElement(verticesData2, 0);
            for (int i = 0; i < Vertices.Count; i++)
            {
                Marshal.StructureToPtr(Vertices[i].Coordinate, ptr, true);
                ptr += c_vertexStride2;
            }

            ptr = Marshal.UnsafeAddrOfPinnedArrayElement(indexsData, 0);
            for (int i = 0; i < TriangleIndexs.Count; i++)
            {
                Marshal.WriteInt32(ptr + i * c_indexStride, TriangleIndexs[i]);
            }

            //NearTrianglesGenerate();
        }
        ///<summary>Reoad2()后可用</summary>
        public MMDMesh GetMesh()
        {
            MMDMesh meshInstance;
            meshInstance = MMDMesh.Load1(verticesData, verticesData2, indexsData, c_vertexStride, c_vertexStride2, c_indexStride, PrimitiveTopology._TRIANGLELIST);
            return meshInstance;
        }
        ~PMXFormat()
        {
            if (gch_verticesData.IsAllocated) gch_verticesData.Free();
            if (gch_verticesData2.IsAllocated) gch_verticesData2.Free();
            if (gch_indexsData.IsAllocated) gch_indexsData.Free();
        }
        //const int c_nearTrianglesMaxCount = 16;
        //public struct NearTriangleIndex
        //{
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = c_nearTrianglesMaxCount, ArraySubType = UnmanagedType.I4)]
        //    public int[] data;
        //    public int this[int i]
        //    {
        //        get
        //        {
        //            return data[i];
        //        }
        //        set
        //        {
        //            data[i] = value;
        //        }
        //    }
        //    public void Add(int x)
        //    {
        //        for (int i = 0; i < c_nearTrianglesMaxCount; i++)
        //        {
        //            if (data[i] == -1)
        //            {
        //                data[i] = x;
        //                return;
        //            }
        //        }
        //    }
        //    public void NSAdd(int x)
        //    {
        //        for (int i = 0; i < c_nearTrianglesMaxCount; i++)
        //        {
        //            if (data[i] == x)
        //            {
        //                return;
        //            }
        //            if (data[i] == -1)
        //            {
        //                data[i] = x;
        //                return;
        //            }
        //        }
        //    }
        //    public NearTriangleIndex(bool nothing)
        //    {
        //        data = new int[c_nearTrianglesMaxCount];
        //        for (int i = 0; i < c_nearTrianglesMaxCount; i++)
        //            data[i] = -1;
        //    }
        //}

        //public void NearTrianglesGenerate()
        //{
        //    Dictionary<Vector3, NearTriangleIndex> VerticeMerge = new Dictionary<Vector3, NearTriangleIndex>(Vertices.Count);
        //    for (int i = 0; i < Vertices.Count; i++)
        //    {
        //        if (VerticeMerge.ContainsKey(Vertices[i].Coordinate))
        //        {

        //        }
        //        else
        //        {
        //            VerticeMerge[Vertices[i].Coordinate] = new NearTriangleIndex(true);
        //        }
        //    }
        //    int td3 = TriangleIndexs.Count / 3;
        //    //将三角形索引与顶点关联
        //    for (int i = 0; i < td3; i++)
        //    {
        //        int ix3 = i * 3;
        //        VerticeMerge[Vertices[TriangleIndexs[ix3]].Coordinate].Add(i);
        //        VerticeMerge[Vertices[TriangleIndexs[ix3 + 1]].Coordinate].Add(i);
        //        VerticeMerge[Vertices[TriangleIndexs[ix3 + 2]].Coordinate].Add(i);
        //    }

        //    int[] n1 = new int[td3 * c_nearTrianglesMaxCount];
        //    for (int i = 0; i < n1.Length; i++)
        //    {
        //        n1[i] = -1;
        //    }
        //    //将临近的三角形加入引用
        //    for (int i = 0; i < td3; i++)
        //    {
        //        int ix3 = i * 3;
        //        int[] x1 = VerticeMerge[Vertices[TriangleIndexs[ix3]].Coordinate].data;
        //        void _Fun2(int triIndex, int val)
        //        {
        //            int startIndex = triIndex * c_nearTrianglesMaxCount;
        //            for (int j = 0; j < c_nearTrianglesMaxCount; j++)
        //            {
        //                int xV = n1[j + startIndex];
        //                if (xV == val)
        //                {
        //                    return;
        //                }
        //                if (xV == -1)
        //                {
        //                    n1[j + startIndex] = val;
        //                    return;
        //                }
        //            }
        //        }
        //        void _Fun1()
        //        {
        //            for (int j = 0; j < c_nearTrianglesMaxCount; j++)
        //            {
        //                if (x1[j] == -1)
        //                    break;
        //                if (x1[j] != i)
        //                    _Fun2(i, x1[j]);
        //            }
        //        }
        //        _Fun1();
        //        x1 = VerticeMerge[Vertices[TriangleIndexs[ix3 + 1]].Coordinate].data;
        //        _Fun1();
        //        x1 = VerticeMerge[Vertices[TriangleIndexs[ix3 + 2]].Coordinate].data;
        //        _Fun1();
        //    }

        //    NearTriangleBuffer.Reload(n1);
        //}
        //public StaticBuffer NearTriangleBuffer = new StaticBuffer();
    }
    public static partial class PMXFormatExtension
    {
        public static void Reload2(this MMD3DEntity entity, DeviceResources deviceResources, ProcessingList processingList, PMXFormat modelResource)
        {
            entity.ReloadBase();
            entity.Name = string.Format("{0} {1}", modelResource.Name, modelResource.NameEN);
            entity.Description = string.Format("{0}\n{1}", modelResource.Description, modelResource.DescriptionEN);
            entity.motionComponent.ReloadEmpty();

            entity.morphStateComponent.Reload(modelResource);
            entity.boneComponent.Reload(modelResource);
            entity.boneComponent.boneMatricesBuffer.Reload(deviceResources, MMDBoneComponent.c_boneMatrixDataSize);

            entity.rendererComponent.Reload(processingList, modelResource);
            entity.ComponentReady = true;
        }
    }
}
