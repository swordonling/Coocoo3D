using Coocoo3D.Present;
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
    public class RayTracingRenderPipeline1 : RenderPipeline
    {
        public const int c_postProcessDataSize = 256;
        public const int c_transformMatrixDataSize = 64;

        public override GraphicsSignature GraphicsSignature => rootSignature;
        byte[] rcDataUploadBuffer = new byte[c_postProcessDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        RayTracingPipelineStateObject RayTracingPipelineStateObject = new RayTracingPipelineStateObject();
        RayTracingAccelerationStructure RayTracingAccelerationStructure = new RayTracingAccelerationStructure();


        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        ConstantBuffer buffer1 = new ConstantBuffer();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();

        public RayTracingRenderPipeline1()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i] = new PresentData();
            }
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
        }
        ~RayTracingRenderPipeline1()
        {
            gch_rcDataUploadBuffer.Free();
        }

        public void Reload(DeviceResources deviceResources)
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Reload(deviceResources);
            }

            rootSignature.ReloadRayTracing(deviceResources);
            rootSignatureGraphics.Reload(deviceResources, new GraphicSignatureDesc[]
            {
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
            });
            buffer1.Reload(deviceResources, c_postProcessDataSize);
            RayTracingAccelerationStructure.Reload(deviceResources, 1024 * 1024 * 64, 10);
        }

        #region graphics assets
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMDSkinning2, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");

            RayTracingPipelineStateObject.Reload(deviceResources, rootSignature, await ReadAllBytes("ms-appx:///Coocoo3DGraphics/Raytracing.cso"));
            PObjectMMD2.ReloadSkinningOnly(deviceResources, rootSignatureGraphics, VSMMDSkinning2, null);
            Ready = true;
        }
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public PObject PObjectMMD2 = new PObject();
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public GraphicsSignature rootSignatureGraphics = new GraphicsSignature();
        #endregion

        public override void PrepareRenderData(RenderPipelineContext context)
        {
            DesireEntityBuffers(context.deviceResources, context.scene.Entities.Count);
            var graphicsContext = context.graphicsContext;
            var cameras = context.cameras;
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].UpdateBuffer(graphicsContext);
            }
            RayTracingPipelineStateObject.ReloadTablesForModels(context.deviceResources, context.scene.Entities.Count);
        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            IList<MMD3DEntity> Entities = context.scene.Entities;
            for (int i = 0; i < Entities.Count; i++)
            {
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(Entities[i].Rotation) * Matrix4x4.CreateTranslation(Entities[i].Position);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0), true);
                graphicsContext.UpdateResource(entityDataBuffers[i], rcDataUploadBuffer, c_transformMatrixDataSize);
            }
            graphicsContext.SetRootSignature(rootSignatureGraphics);
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(graphicsContext, Entities[i], cameraPresentDatas[0], entityDataBuffers[i]);

            var entities = context.scene.Entities;
            if (entities.Count > 0)
            {
                for (int i = 0; i < context.scene.Entities.Count; i++)
                {
                    RayTracingAccelerationStructure.AddMeshToThisFrameRayTracingList(entities[i].rendererComponent.mesh);
                }
                context.graphicsContext.BuildAccelerationStructures(RayTracingAccelerationStructure);
            }
            graphicsContext.SetRootSignatureRayTracing(rootSignature);
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            context.graphicsContext.SetUAVCT(context.outputRTV, 0);
            var entities = context.scene.Entities;
            if (entities.Count > 0)
            {
                context.graphicsContext.SetSRVCT(RayTracingAccelerationStructure, 1);
            }
            else
            {
                context.graphicsContext.SetSRVCT(null, 1);
            }
            context.graphicsContext.SetCBVCR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
            context.graphicsContext.TestRayTracing(RayTracingPipelineStateObject);
        }

        private void EntitySkinning(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            graphicsContext.SetCBVR(entity.boneComponent.boneMatrices, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData.DataBuffer, 2);
            graphicsContext.SetMesh(entity.rendererComponent.mesh);
            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
            if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectStreamOut(entity.rendererComponent.pObject);
            else
                graphicsContext.SetPObjectStreamOut(PObjectMMD2);

            graphicsContext.SetSOMesh(entity.rendererComponent.mesh);
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
            graphicsContext.SetSOMesh(null);
        }

        private void DesireEntityBuffers(DeviceResources deviceResources, int count)
        {
            while (entityDataBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_transformMatrixDataSize);
                entityDataBuffers.Add(constantBuffer);
            }
        }
    }
}
