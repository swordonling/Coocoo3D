using Coocoo3D.Utility;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Core
{
    public class ProcessingList
    {
        public List<ITexture> TextureLoadList = new List<ITexture>();
        public List<IRenderTexture> RenderTextureUpdateList = new List<IRenderTexture>();
        public List<MMDMesh> MMDMeshLoadList = new List<MMDMesh>();

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

        public void MoveToAnother(ProcessingList another)
        {
            TextureLoadList.MoveTo_CC(another.TextureLoadList);
            RenderTextureUpdateList.MoveTo_CC(another.RenderTextureUpdateList);
            MMDMeshLoadList.MoveTo_CC(another.MMDMeshLoadList);
        }

        public void Clear()
        {
            TextureLoadList.Clear();
            RenderTextureUpdateList.Clear();
            MMDMeshLoadList.Clear();
        }

        public bool IsEmpty()
        {
            return TextureLoadList.Count == 0 &&
                RenderTextureUpdateList.Count == 0 &&
                MMDMeshLoadList.Count == 0;
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

        public void _DealStep1(GraphicsContext graphicsContext)
        {
            for (int i = 0; i < TextureLoadList.Count; i++)
                graphicsContext.UploadTexture(TextureLoadList[i]);
            for (int i = 0; i < MMDMeshLoadList.Count; i++)
                graphicsContext.UploadMesh(MMDMeshLoadList[i]);
        }
        public void _DealStep2(GraphicsContext graphicsContext)
        {
            for (int i = 0; i < RenderTextureUpdateList.Count; i++)
            {
                graphicsContext.UpdateRenderTexture(RenderTextureUpdateList[i]);
            }
            for (int i = 0; i < TextureLoadList.Count; i++)
                TextureLoadList[i].ReleaseUploadHeapResource();
            for (int i = 0; i < MMDMeshLoadList.Count; i++)
                MMDMeshLoadList[i].ReleaseUploadHeapResource();
        }
    }
}
