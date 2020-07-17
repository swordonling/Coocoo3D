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
        public const int c_tempDataSize = 512;
        public const int c_transformMatrixDataSize = 64;
        const int c_materialDataSize = 512;
        const int c_argumentsSizeInBytes = 64;
        const int c_presentDataSize = 256;
        public override GraphicsSignature GraphicsSignature => rootSignatureGraphics;
        byte[] rcDataUploadBuffer = new byte[c_tempDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        RayTracingScene RayTracingScene = new RayTracingScene();


        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
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
                cameraPresentDatas[i].Reload(deviceResources, c_presentDataSize);
            }

            rootSignatureGraphics.Reload(deviceResources, new GraphicSignatureDesc[]
            {
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
                GraphicSignatureDesc.CBV,
            });
        }

        #region graphics assets
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMDSkinning2, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");

            RayTracingScene.ReloadPipelineStatesStep0();
            RayTracingScene.ReloadPipelineStatesStep1(await ReadAllBytes("ms-appx:///Coocoo3DGraphics/Raytracing2.cso"), new string[] { "MyRaygenShader", "ClosestHitShaderColor", "ClosestHitShaderTest", "MissShaderColor", "MissShaderTest"/*, "MyAnyHitShader"*/, });
            RayTracingScene.ReloadPipelineStatesStep2(deviceResources, hitGroupDescs, 32, 8, 5);
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
        string[] missShaderNames = { "MissShaderColor", "MissShaderTest", };
        string[] hitGroupNames = new string[] { "HitGroupColor", "HitGroupTest", };
        HitGroupDesc[] hitGroupDescs = new HitGroupDesc[]
        {
            new HitGroupDesc { HitGroupName = "HitGroupColor"/*,AnyHitName="MyAnyHitShader"*/, ClosestHitName = "ClosestHitShaderColor" },
            new HitGroupDesc { HitGroupName = "HitGroupTest",ClosestHitName="ClosestHitShaderTest" },
        };
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

                IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize);
            }
            for (int i = 0; i < Entities.Count; i++)
            {
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(Entities[i].Rotation) * Matrix4x4.CreateTranslation(Entities[i].Position);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0), true);
                graphicsContext.UpdateResource(entityDataBuffers[i], rcDataUploadBuffer, c_transformMatrixDataSize);
            }


            IntPtr ptr_rc = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
            #region Update material data
            int matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Array.Clear(rcDataUploadBuffer, 0, c_materialDataSize);
                    Marshal.StructureToPtr(Materials[j].innerStruct, ptr_rc, true);
                    WriteLightData(context.scene.Lightings, ptr_rc + MMDMatLit.c_materialDataSize);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, c_materialDataSize);
                    matIndex++;
                }
            }
            #endregion

            RayTracingScene.NextASIndex(matIndex);
            RayTracingScene.NextSTIndex();
        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            IList<MMD3DEntity> Entities = context.scene.Entities;
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
                    BuildEntityBAS(context, entities[i], ref matIndex);
                }
                graphicsContext.BuildTopAccelerationStructures(RayTracingScene);
                RayTracingScene.BuildShaderTableStep1(context.deviceResources, rayGenShaderNames, missShaderNames, c_argumentsSizeInBytes);
                RayTracingScene.BuildShaderTableStep2(context.deviceResources, hitGroupNames, c_argumentsSizeInBytes, matIndex);
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

                graphicsContext.BuildBASAndParam(RayTracingScene, entity.rendererComponent.mesh, indexStartLocation, Materials[i].indexCount,2, tex1, materialBuffers[matIndex]);
                matIndex++;

                indexStartLocation += Materials[i].indexCount;
            }
        }

        private void WriteLightData(IList<Lighting> lightings, IntPtr pBufferData)
        {
            int lightCount = 0;
            for (int j = 0; j < lightings.Count; j++)
            {
                if (lightings[j].LightingType == LightingType.Directional)
                    Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[j].rotateMatrix), pBufferData, true);
                else
                    Marshal.StructureToPtr(lightings[j].Rotation * 180 / MathF.PI, pBufferData, true);
                Marshal.StructureToPtr((uint)lightings[j].LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(lightings[j].Color, pBufferData + 16, true);
                Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[j].vpMatrix), pBufferData + 32, true);
                lightCount++;
                pBufferData += 32;
                if (lightCount >= 8)
                    break;
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
