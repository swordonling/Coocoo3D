using Coocoo3D.Components;
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
        const int c_materialDataSize = 256;
        const int c_argumentsSizeInBytes = 64;
        public override GraphicsSignature GraphicsSignature => rootSignatureGraphics;
        byte[] rcDataUploadBuffer = new byte[c_postProcessDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        RayTracingScene RayTracingScene = new RayTracingScene();


        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        ConstantBuffer buffer1 = new ConstantBuffer();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBufferStatic> materialBuffers = new List<ConstantBufferStatic>();

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

            rootSignatureGraphics.Reload(deviceResources, new GraphicSignatureDesc[]
            {
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
            });
            buffer1.Reload(deviceResources, c_postProcessDataSize);
        }

        #region graphics assets
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMDSkinning2, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");

            RayTracingScene.ReloadPipelineStatesStep0();
            RayTracingScene.ReloadPipelineStatesStep1(await ReadAllBytes("ms-appx:///Coocoo3DGraphics/Raytracing2.cso"), new string[] { "MyRaygenShader", "MyClosestHitShader", "MyMissShader", });
            RayTracingScene.ReloadPipelineStatesStep2(deviceResources, new string[] { "MyHitGroup" }, new string[] { "MyClosestHitShader" });
            RayTracingScene.ReloadPipelineStatesStep3(16, 8, 1);
            RayTracingScene.ReloadPipelineStatesStep4(deviceResources);
            RayTracingScene.ReloadAllocScratchAndInstance(deviceResources, 1024 * 1024 * 64, 1024);
            PObjectMMD2.ReloadSkinningOnly(deviceResources, rootSignatureGraphics, VSMMDSkinning2, null);
            Ready = true;
        }
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public PObject PObjectMMD2 = new PObject();
        public GraphicsSignature rootSignatureGraphics = new GraphicsSignature();
        #endregion
        string[] rayGenShaderNames = { "MyRaygenShader" };
        string[] missShaderNames = { "MyMissShader" };
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var Entities = context.scene.Entities;
            var deviceResources = context.deviceResources;
            int countMaterials = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireMaterialBuffers(deviceResources, countMaterials);
            DesireEntityBuffers(deviceResources, Entities.Count);
            var graphicsContext = context.graphicsContext;
            var cameras = context.cameras;
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].UpdateBuffer(graphicsContext);
            }


            IntPtr ptr_rc = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
            #region Update material data
            int matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Marshal.StructureToPtr(Materials[j].innerStruct, ptr_rc, true);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, c_materialDataSize);
                    matIndex++;
                }
            }
            #endregion

            RayTracingScene.NextASIndex(2048);
            RayTracingScene.NextSTIndex();
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
            graphicsContext.SetSOMesh(null);

            var entities = context.scene.Entities;

            if (entities.Count > 0)
            {
                int matIndex = 0;
                for (int i = 0; i < context.scene.Entities.Count; i++)
                {
                    //graphicsContext.BuildBottomAccelerationStructures(RayTracingScene, entities[i].rendererComponent.mesh, 0, entities[i].rendererComponent.mesh.m_indexCount);
                    BuildEntityBAS(context, entities[i], ref matIndex);
                }
                graphicsContext.BuildTopAccelerationStructures(RayTracingScene);
                RayTracingScene.BuildShaderTableStep1(context.deviceResources, rayGenShaderNames, missShaderNames, c_argumentsSizeInBytes);
                RayTracingScene.BuildShaderTableStep2(context.deviceResources, "MyHitGroup", c_argumentsSizeInBytes, matIndex);
            }
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            context.graphicsContext.SetRootSignatureRayTracing(RayTracingScene);
            context.graphicsContext.SetUAVCT(context.outputRTV, 0);
            var entities = context.scene.Entities;

            context.graphicsContext.SetCBVCR(cameraPresentDatas[cameraIndex].DataBuffer, 2);

            if (entities.Count > 0)
            {
                context.graphicsContext.DoRayTracing(RayTracingScene, 1);
            }
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
        }


        private void BuildEntityBAS(RenderPipelineContext context, MMD3DEntity entity, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            MMDRendererComponent rendererComponent = entity.rendererComponent;

            var Materials = rendererComponent.Materials;
            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.texs;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].innerStruct.DiffuseColor.W < 0)
                {
                    indexStartLocation += Materials[i].indexCount;
                    continue;
                }
                Texture2D tex1 = null;
                if (texs != null)
                {

                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    if (tex1 != null)
                    {
                        if (tex1.Status == GraphicsObjectStatus.loaded)
                        {

                        }
                        else if (tex1.Status == GraphicsObjectStatus.loading)
                            tex1 = textureLoading;
                        else
                            tex1 = textureError;
                    }
                    else
                        tex1 = textureError;
                }
                else
                {
                    tex1 = textureError;
                }

                graphicsContext.BuildBASAndParam(RayTracingScene, entity.rendererComponent.mesh, indexStartLocation, Materials[i].indexCount, tex1, materialBuffers[matIndex]);
                matIndex++;

                indexStartLocation += Materials[i].indexCount;
            }
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
        private void DesireMaterialBuffers(DeviceResources deviceResources, int count)
        {
            while (materialBuffers.Count < count)
            {
                ConstantBufferStatic constantBuffer = new ConstantBufferStatic();
                constantBuffer.Reload(deviceResources, c_materialDataSize);
                materialBuffers.Add(constantBuffer);
            }
        }
    }
}
