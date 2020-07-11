using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class PostProcess : RenderPipeline
    {
        public const int c_postProcessDataSize = 256;

        public override GraphicsSignature GraphicsSignature => rootSignature;
        byte[] rcDataUploadBuffer = new byte[c_postProcessDataSize];
        public GCHandle gch_rcDataUploadBuffer;

        public InnerStruct innerStruct;
        ConstantBuffer postProcessDataBuffer = new ConstantBuffer();

        public PostProcess()
        {
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
            innerStruct.GammaCorrection = 2.2f;
        }
        ~PostProcess()
        {
            gch_rcDataUploadBuffer.Free();
        }

        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.Reload(deviceResources, new GraphicSignatureDesc[]
            {
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.SRVTable,
            });
            postProcessDataBuffer.Reload(deviceResources, c_postProcessDataSize);
        }

        public override void PrepareRenderData(RenderPipelineContext context)
        {

        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {
            Marshal.StructureToPtr(innerStruct, Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0), true);
            context.graphicsContext.UpdateResource(postProcessDataBuffer, rcDataUploadBuffer, c_postProcessDataSize);
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetRenderTargetScreenAndClear(context.settings.backgroundColor);
            graphicsContext.SetCBVR(postProcessDataBuffer, 0);
            graphicsContext.SetSRVT(context.outputRTV, 1);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.SetPObject(PObjectPostProcess, CullMode.back, BlendState.alpha);
            graphicsContext.DrawIndexed(context.ndcQuadMesh.m_indexCount, 0, 0);
        }

        #region graphics assets
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSPostProcess, deviceResources, "ms-appx:///Coocoo3DGraphics/VSPostProcess.cso");
            await ReloadPixelShader(PSPostProcess, deviceResources, "ms-appx:///Coocoo3DGraphics/PSPostProcess.cso");
            PObjectPostProcess.Reload(deviceResources, rootSignature, PObjectType.postProcess, VSPostProcess, null, PSPostProcess);
            Ready = true;
        }
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public VertexShader VSPostProcess = new VertexShader();
        public PixelShader PSPostProcess = new PixelShader();
        public PObject PObjectPostProcess = new PObject();
        #endregion

        public struct InnerStruct
        {
            public float GammaCorrection;
        }

    }
}
