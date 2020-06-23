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

        public string Name;
        public string Description;

        public MMDRendererComponent rendererComponent = new MMDRendererComponent();
        public MMDBoneComponent boneComponent = new MMDBoneComponent();
        public MMDMotionComponent motionComponent = new MMDMotionComponent();
        public MMDMorphStateComponent morphStateComponent = new MMDMorphStateComponent();

        public bool RenderReady = false;
        public bool ComponentReady = false;

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
        }

        public void UpdateGpuResources(DeviceResources deviceResources, IList<Lighting> lightings)
        {
            Matrix4x4 matWorld = Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
            GraphicsContext graphicsContext = GraphicsContext.Load(deviceResources);
            rendererComponent.UpdateGPUResources(graphicsContext, matWorld, lightings);
            if (!boneComponent.GpuUsable)
            {
                boneComponent.ComputeMatricesData();
                graphicsContext.UpdateResource(boneComponent.boneMatrices, boneComponent.boneMatricesData);
                boneComponent.GpuUsable = true;
            }
        }

        public void RenderDepth(GraphicsContext graphicsContext, DefaultResources defaultResources, PresentData presentData)
        {
            rendererComponent.RenderDepth(graphicsContext, boneComponent, presentData);
        }

        public void Render(GraphicsContext graphicsContext, DefaultResources defaultResources, IList<Lighting> lightings, PresentData presentData)
        {
            rendererComponent.Render(graphicsContext, defaultResources, boneComponent, presentData);
        }

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public class MMDMatLit
    {
        public const int c_materialDataSize = 128;
        public bool GpuUsable = false;

        public Texture2D tex;
        public string Name;
        public string NameEN;
        public int indexCount;
        public int texIndex;
        public int toonIndex;
        public MMDSupport.DrawFlags DrawFlags;
        public Vector4 DiffuseColor { get => innerStruct.DiffuseColor; set => innerStruct.DiffuseColor = value; }
        public Vector4 SpecularColor { get => innerStruct.SpecularColor; set => innerStruct.SpecularColor = value; }
        public Vector3 AmbientColor { get => innerStruct.AmbientColor; set => innerStruct.AmbientColor = value; }
        public float EdgeScale { get => innerStruct.EdgeScale; set => innerStruct.EdgeScale = value; }
        public Vector4 EdgeColor { get => innerStruct.EdgeColor; set => innerStruct.EdgeColor = value; }
        public ConstantBuffer matBuf;

        public InnerStruct innerStruct;
        public struct InnerStruct
        {
            public Vector4 DiffuseColor;
            public Vector4 SpecularColor;
            public Vector3 AmbientColor;
            public float EdgeScale;
            public Vector4 EdgeColor;

            public Vector4 Texture;
            public Vector4 SubTexture;
            public Vector4 ToonTexture;
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
        }
        ///<summary>Reoad2()后可用</summary>
        public MMDMesh GetMesh(DeviceResources deviceResources)
        {
            MMDMesh meshInstance;
            meshInstance = MMDMesh.Load1(deviceResources, verticesData, verticesData2, indexsData, c_vertexStride, c_vertexStride2, c_indexStride, PrimitiveTopology._TRIANGLELIST);
            meshInstance.m_verticeData = verticesData;
            meshInstance.m_verticeData2 = verticesData2;
            meshInstance.m_indexData = indexsData;
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
        public static MMD3DEntity LoadEntity(DeviceResources deviceResources, PMXFormat modelResource, DefaultResources defaultResources, List<Texture2D> textures)
        {
            MMD3DEntity entity = new MMD3DEntity();
            entity.Reload(deviceResources, modelResource, defaultResources, textures);
            return entity;
        }
        public static void Reload(this MMD3DEntity entity, DeviceResources deviceResources, PMXFormat modelResource, DefaultResources defaultResources, List<Texture2D> textures)
        {
            entity.ReloadBase();
            entity.Name = string.Format("{0} {1}", modelResource.Name, modelResource.NameEN);
            entity.Description = string.Format("{0}\n{1}", modelResource.Description, modelResource.DescriptionEN);
            entity.motionComponent.ReloadEmpty();

            entity.morphStateComponent.Reload(modelResource);
            entity.boneComponent.Reload(modelResource);
            entity.boneComponent.boneMatrices.Reload(deviceResources, MMDBoneComponent.c_boneMatrixDataSize);

            entity.rendererComponent.Reload(deviceResources, modelResource);
            entity.ComponentReady = true;

            entity.rendererComponent.pObject = defaultResources.PObjectMMD;
            entity.rendererComponent.texs = new List<Texture2D>(textures);
            entity.RenderReady = true;
        }

        public static void Reload2(this MMD3DEntity entity, DeviceResources deviceResources, PMXFormat modelResource)
        {
            entity.ReloadBase();
            entity.Name = string.Format("{0} {1}", modelResource.Name, modelResource.NameEN);
            entity.Description = string.Format("{0}\n{1}", modelResource.Description, modelResource.DescriptionEN);
            entity.motionComponent.ReloadEmpty();

            entity.morphStateComponent.Reload(modelResource);
            entity.boneComponent.Reload(modelResource);
            entity.boneComponent.boneMatrices.Reload(deviceResources, MMDBoneComponent.c_boneMatrixDataSize);

            entity.rendererComponent.Reload(deviceResources, modelResource);
            entity.ComponentReady = true;
        }
    }
}
