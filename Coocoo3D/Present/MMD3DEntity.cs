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
using Coocoo3D.RenderPipeline;

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
            lock (motionComponent)
            {
                morphStateComponent.SetPose(motionComponent, time);
                morphStateComponent.ComputeWeight();
                boneComponent.SetPose(motionComponent, morphStateComponent, time);
            }
            boneComponent.ComputeMatricesData();
            rendererComponent.SetPose(morphStateComponent);
            needUpdateMotion = true;
        }

        public void UpdateGpuResources(GraphicsContext graphicsContext)
        {
            rendererComponent.UpdateGPUResources(graphicsContext);
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
                for (int j = 0; j < 4; j++)
                    Marshal.WriteInt32(ptr + 24 + j * 4, Vertices[i].boneId[j]);
                Marshal.Copy(Vertices[i].weight, 0, ptr + 24 + 16, 4);
                ptr += c_vertexStride;
            }
            IntPtr ptr2 = Marshal.UnsafeAddrOfPinnedArrayElement(verticesData2, 0);
            for (int i = 0; i < Vertices.Count; i++)
            {
                Marshal.StructureToPtr(Vertices[i].Coordinate, ptr2 + i * c_vertexStride2, true);
            }

            IntPtr ptr3 = Marshal.UnsafeAddrOfPinnedArrayElement(indexsData, 0);
            for (int i = 0; i < TriangleIndexs.Count; i++)
            {
                Marshal.WriteInt32(ptr3 + i * c_indexStride, TriangleIndexs[i]);
            }
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
    }
    public static partial class PMXFormatExtension
    {
        public static void Reload2(this MMD3DEntity entity, ProcessingList processingList, PMXFormat modelResource)
        {
            entity.ReloadBase();
            entity.Name = string.Format("{0} {1}", modelResource.Name, modelResource.NameEN);
            entity.Description = string.Format("{0}\n{1}", modelResource.Description, modelResource.DescriptionEN);
            entity.motionComponent.ReloadEmpty();

            entity.morphStateComponent.Reload(modelResource);
            entity.boneComponent.Reload(modelResource);

            entity.rendererComponent.Reload(processingList, modelResource);
            entity.ComponentReady = true;
        }
    }
}
