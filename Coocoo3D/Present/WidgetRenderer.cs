using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Coocoo3DGraphics;
using Coocoo3D.Core;

namespace Coocoo3D.Present
{
    public class WidgetRenderer
    {
        Material boneRenderMaterial;
        MMDMesh particleMesh;
        const int c_particleCount = 1024;
        const int c_perVertexSize = 32;
        byte[] vertexData = new byte[c_particleCount * c_perVertexSize];
        GCHandle gch_vertexData;
        byte[] indexData = new byte[c_particleCount * 1 * 4];
        GCHandle gch_indexData;
        public WidgetRenderer()
        {
            gch_vertexData = GCHandle.Alloc(vertexData);
            gch_indexData = GCHandle.Alloc(indexData);
        }
        ~WidgetRenderer()
        {
            gch_vertexData.Free();
            gch_indexData.Free();
        }

        public void Init(DeviceResources deviceResources, DefaultResources defaultResources, IDictionary<string, Texture2D> caches)
        {
            boneRenderMaterial = Material.Load(defaultResources.uiPObject);
            boneRenderMaterial.SetTexture(defaultResources.ui0Texture, 0);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(indexData, 0);
            for (int i = 0; i < c_particleCount; i++)
            {
                Marshal.WriteInt32(ptr, i);
                ptr += 4;
            }
            particleMesh = MMDMesh.Load(deviceResources, vertexData, indexData, c_perVertexSize, 4, PrimitiveTopology._POINTLIST);
        }

        public void RenderBoneVisual(DeviceResources deviceResources, Camera camera, MMD3DEntity entity)
        {
            GraphicsContext graphicsContext = GraphicsContext.Load(deviceResources);
            graphicsContext.SetMaterial(boneRenderMaterial);

            var bones = entity.boneComponent.bones;
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(vertexData, 0);
            int visibleBoneCount = 0;
            Vector2 viewSize = new Vector2(0.05f, 0.05f);
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Flags.HasFlag(MMDSupport.BoneFlags.Visible))
                {
                    var pos = Vector3.Transform(bones[i].staticPosition, bones[i].GeneratedTransform);
                    visibleBoneCount++;

                    Marshal.StructureToPtr(pos, ptr, true);
                    Marshal.WriteInt32(ptr + 12, 1);
                    Marshal.WriteInt32(ptr + 16, 1);
                    Marshal.WriteInt32(ptr + 20, 0);
                    Marshal.StructureToPtr(viewSize, ptr + 24, true);

                    ptr += c_perVertexSize;
                }
            }
            graphicsContext.UpdateVertices(particleMesh, vertexData);
            graphicsContext.GSSetConstantBuffer(entity.rendererComponent.EntityDataBuffer, 0);
            graphicsContext.SetMesh(particleMesh);
            graphicsContext.SetMaterial(boneRenderMaterial);
            //graphicsContext.DrawIndexed(visibleBoneCount * 1, 0, 0);
            graphicsContext.Draw(visibleBoneCount * 1, 0);
        }
    }
}
