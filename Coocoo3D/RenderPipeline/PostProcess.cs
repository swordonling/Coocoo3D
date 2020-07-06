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
        Settings settings;
        byte[] rcDataUploadBuffer = new byte[c_postProcessDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        public volatile bool Ready;

        public PostProcess()
        {
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
        }
        ~PostProcess()
        {
            gch_rcDataUploadBuffer.Free();
        }

        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.ReloadMMD(deviceResources);
        }

        public void PrepareRenderData(RenderPipelineContext context)
        {

        }

        public void BeforeRenderCameras(RenderPipelineContext context)
        {

        }

        public void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
            graphicsContext.SetSRV_RT(PObjectType.mmd, context.outputRTV, 0);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.SetPObject(PObjectPostProcess, CullMode.back, BlendState.none);
            graphicsContext.DrawIndexed(6, 0, 0);
        }

        #region graphics assets
        public async Task ReloadAssets(DeviceResources deviceResources)
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



    }
}
