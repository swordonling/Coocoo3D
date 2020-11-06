using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class WidgetRenderer : RenderPipeline
    {
        const int c_bufferSize = 256;
        public ConstantBuffer[] constantBuffers = new ConstantBuffer[8];
        public byte[] uploadBuffer = new byte[c_bufferSize];
        GCHandle gch_uploadBuffer;
        public WidgetRenderer()
        {
            gch_uploadBuffer = GCHandle.Alloc(uploadBuffer);
            for (int i = 0; i < constantBuffers.Length; i++)
            {
                constantBuffers[i] = new ConstantBuffer();
            }
        }
        ~WidgetRenderer()
        {
            if (gch_uploadBuffer.IsAllocated) gch_uploadBuffer.Free();
        }
        public void Reload(DeviceResources deviceResources)
        {
            for (int i = 0; i < constantBuffers.Length; i++)
            {
                constantBuffers[i].Reload(deviceResources, c_bufferSize);
            }
        }

        struct _Data
        {
            public Vector2 size;
            public Vector2 offset;
            public Vector2 uvSize;
            public Vector2 uvOffset;
        }
        readonly Vector2 c_buttonSize = new Vector2(64, 64);

        int allocated;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(uploadBuffer, 0);
            Vector2 screenSize = new Vector2(context.screenWidth, context.screenHeight) / context.logicScale;
            Marshal.StructureToPtr(screenSize, pData, true);

            allocated = 0;
            int allocatedSize = 32;
            void write(_Data data1)
            {
                Marshal.StructureToPtr(data1, pData + allocatedSize, true);
                allocatedSize += 32;
                allocated++;
            }

            #region Buttons
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-192, 64),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0, 0),
            });
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-128, 64),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0.25f, 0),
            });
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-64, 64),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0.5f, 0),
            });
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-192, 0),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0, 0.25f),
            });
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-128, 0),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0.25f, 0.25f),
            });
            write(new _Data()
            {
                size = c_buttonSize,
                offset = new Vector2(screenSize.X, 0) + new Vector2(-64, 0),
                uvSize = new Vector2(0.25f, 0.25f),
                uvOffset = new Vector2(0.5f, 0.25f),
            });
            #endregion

            graphicsContext.UpdateResource(constantBuffers[0], uploadBuffer, c_bufferSize, 0);
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignaturePostProcess);
            graphicsContext.SetPObject(context.RPAssetsManager.PObjectWidgetUI1, CullMode.none);
            graphicsContext.SetCBVR(constantBuffers[0], 0);
            graphicsContext.SetSRVT(context.UI1Texture, 1);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexedInstanced(context.ndcQuadMeshIndexCount, 0, 0, allocated, 0);
        }
    }
}
