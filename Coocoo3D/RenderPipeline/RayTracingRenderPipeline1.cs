using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class RayTracingRenderPipeline1 : RenderPipeline
    {
        public const int c_postProcessDataSize = 256;

        public override GraphicsSignature GraphicsSignature => rootSignature;
        byte[] rcDataUploadBuffer = new byte[c_postProcessDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        RayTracingPipelineStateObject RayTracingPipelineStateObject = new RayTracingPipelineStateObject();

        ConstantBuffer buffer1 = new ConstantBuffer();

        public RayTracingRenderPipeline1()
        {
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
        }
        ~RayTracingRenderPipeline1()
        {
            gch_rcDataUploadBuffer.Free();
        }

        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.ReloadRayTracing(deviceResources);
            buffer1.Reload(deviceResources, c_postProcessDataSize);
        }

        public override void PrepareRenderData(RenderPipelineContext context)
        {

        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {

        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {

        }

        #region graphics assets
        public async Task ReloadAssets(DeviceResources deviceResources)
        {
            RayTracingPipelineStateObject.Reload(deviceResources, rootSignature, await ReadAllBytes("ms-appx:///Coocoo3DGraphics/Raytracing.cso"));
            Ready = true;
        }
        public GraphicsSignature rootSignature = new GraphicsSignature();
        #endregion

    }
}
