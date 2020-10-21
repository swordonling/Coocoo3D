using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class ProcessingList
    {
        static void Move1<T>(IList<T> source, IList<T> target)
        {
            lock (source)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    target.Add(source[i]);
                }
                source.Clear();
            }
        }
        public List<ITexture> TextureLoadList = new List<ITexture>();
        public List<IRenderTexture> RenderTextureUpdateList = new List<IRenderTexture>();
        public List<MMDMesh> MMDMeshLoadList = new List<MMDMesh>();
        public List<StaticBuffer> staticBufferList = new List<StaticBuffer>();
        public List<ReadBackTexture2D> readBackTextureList = new List<ReadBackTexture2D>();
        public List<TwinBuffer> twinBufferList = new List<TwinBuffer>();
        public List<PObject>[] pobjectLists = new List<PObject>[] { new List<PObject>(), new List<PObject>(), new List<PObject>(), };
        public List<ComputePO>[] computePObjectLists = new List<ComputePO>[] { new List<ComputePO>(),};

        public void AddObject(MMDMesh mesh)
        {
            lock (MMDMeshLoadList)
            {
                MMDMeshLoadList.Add(mesh);
            }
        }
        public void AddObject(ITexture texture)
        {
            lock (TextureLoadList)
            {
                TextureLoadList.Add(texture);
            }
        }
        public void AddObject(IRenderTexture texture)
        {
            lock (RenderTextureUpdateList)
            {
                RenderTextureUpdateList.Add(texture);
            }
        }
        public void AddObject(ReadBackTexture2D texture)
        {
            lock (readBackTextureList)
            {
                readBackTextureList.Add(texture);
            }
        }
        public void AddObject(StaticBuffer buffer)
        {
            lock (staticBufferList)
            {
                staticBufferList.Add(buffer);
            }
        }
        public void AddObject(TwinBuffer buffer)
        {
            lock (twinBufferList)
            {
                twinBufferList.Add(buffer);
            }
        }
        /// <summary>无法用语言描述的函数 </summary>
        public void RS(PObject pObject, int slot)
        {
            lock (pobjectLists[slot])
            {
                pobjectLists[slot].Add(pObject);
            }
        }
        public void RS(ComputePO pObject, int slot)
        {
            lock (computePObjectLists[slot])
            {
                computePObjectLists[slot].Add(pObject);
            }
        }

        public void MoveToAnother(ProcessingList another)
        {
            Move1(TextureLoadList, another.TextureLoadList);
            Move1(RenderTextureUpdateList, another.RenderTextureUpdateList);
            Move1(MMDMeshLoadList, another.MMDMeshLoadList);
            Move1(readBackTextureList, another.readBackTextureList);
            Move1(staticBufferList, another.staticBufferList);
            Move1(twinBufferList, another.twinBufferList);
            for (int i = 0; i < pobjectLists.Length; i++)
                Move1(pobjectLists[i], another.pobjectLists[i]);
            for (int i = 0; i < computePObjectLists.Length; i++)
                Move1(computePObjectLists[i], another.computePObjectLists[i]);
        }

        public void Clear()
        {
            TextureLoadList.Clear();
            RenderTextureUpdateList.Clear();
            MMDMeshLoadList.Clear();
            readBackTextureList.Clear();
            staticBufferList.Clear();
            twinBufferList.Clear();
            for (int i = 0; i < pobjectLists.Length; i++)
                pobjectLists[i].Clear();
            for (int i = 0; i < computePObjectLists.Length; i++)
                computePObjectLists[i].Clear();
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < pobjectLists.Length; i++)
                if (pobjectLists[i].Count == 0)
                    return false;
            for (int i = 0; i < computePObjectLists.Length; i++)
                if (computePObjectLists[i].Count == 0)
                    return false;

            return TextureLoadList.Count == 0 &&
                RenderTextureUpdateList.Count == 0 &&
                MMDMeshLoadList.Count == 0 &&
                staticBufferList.Count == 0 &&
                readBackTextureList.Count == 0;
        }

        public void UnsafeAdd(ITexture texture)
        {
            TextureLoadList.Add(texture);
        }

        public void UnsafeAdd(IRenderTexture texture)
        {
            RenderTextureUpdateList.Add(texture);
        }

        public void UnsafeAdd(MMDMesh mesh)
        {
            MMDMeshLoadList.Add(mesh);
        }

        public void UnsafeAdd(ReadBackTexture2D texture)
        {
            readBackTextureList.Add(texture);
        }

        public void _DealStep1(GraphicsContext graphicsContext)
        {
            for (int i = 0; i < TextureLoadList.Count; i++)
                graphicsContext.UploadTexture(TextureLoadList[i]);
            for (int i = 0; i < MMDMeshLoadList.Count; i++)
                graphicsContext.UploadMesh(MMDMeshLoadList[i]);
            for (int i = 0; i < staticBufferList.Count; i++)
                graphicsContext.UploadBuffer(staticBufferList[i]);
        }
        public void _DealStep2(GraphicsContext graphicsContext, DeviceResources deviceResources)
        {
            for (int i = 0; i < TextureLoadList.Count; i++)
                TextureLoadList[i].ReleaseUploadHeapResource();
            for (int i = 0; i < MMDMeshLoadList.Count; i++)
                MMDMeshLoadList[i].ReleaseUploadHeapResource();
            for (int i = 0; i < staticBufferList.Count; i++)
                staticBufferList[i].ReleaseUploadHeapResource();

            for (int i = 0; i < RenderTextureUpdateList.Count; i++)
                graphicsContext.UpdateRenderTexture(RenderTextureUpdateList[i]);
            for (int i = 0; i < readBackTextureList.Count; i++)
                graphicsContext.UpdateReadBackTexture(readBackTextureList[i]);
            for (int i = 0; i < twinBufferList.Count; i++)
                twinBufferList[i].Initilize(deviceResources);
        }
    }
}
