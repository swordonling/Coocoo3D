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
        public const int c_entityDataDataSize = 128;
        const int c_materialDataSize = 512;
        const int c_argumentsSizeInBytes = 64;
        const int c_presentDataSize = 512;

        static readonly RayTracingSceneSettings c_rayTracingSceneSettings = new RayTracingSceneSettings()
        {
            payloadSize = 32,
            attributeSize = 8,
            maxRecursionDepth = 5,
            rayTypeCount = 2,
        };

        byte[] rcDataUploadBuffer = new byte[c_tempDataSize];
        public GCHandle gch_rcDataUploadBuffer;
        RayTracingScene RayTracingScene = new RayTracingScene();
        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
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
        }

        #region graphics assets
        static readonly string[] c_rayGenShaderNames = { "MyRaygenShader" };
        static readonly string[] c_missShaderNames = { "MissShaderSurface", "MissShaderTest", };
        static readonly string[] c_hitGroupNames = new string[] { "HitGroupSurface", "HitGroupTest", };
        HitGroupDesc[] hitGroupDescs = new HitGroupDesc[]
        {
            new HitGroupDesc { HitGroupName = "HitGroupSurface", AnyHitName = "AnyHitShaderSurface", ClosestHitName = "ClosestHitShaderSurface" },
            new HitGroupDesc { HitGroupName = "HitGroupTest", AnyHitName = "AnyHitShaderTest", ClosestHitName = "ClosestHitShaderTest" },
        };

        public async Task ReloadAssets(DeviceResources deviceResources)
        {
            RayTracingScene.ReloadLibrary(await ReadFile("ms-appx:///Coocoo3DGraphics/Raytracing.cso"), new string[] { "MyRaygenShader", "ClosestHitShaderSurface", "ClosestHitShaderTest", "MissShaderSurface", "MissShaderTest", "AnyHitShaderSurface", "AnyHitShaderTest", });
            RayTracingScene.ReloadPipelineStates(deviceResources, hitGroupDescs, c_rayTracingSceneSettings);
            RayTracingScene.ReloadAllocScratchAndInstance(deviceResources, 1024 * 1024 * 64, 1024);
            Ready = true;
        }
        #endregion


        public override void TimeChange(double time, double deltaTime)
        {
            for (int i = 0; i < cameraPresentDatas.Length; i++)
            {
                cameraPresentDatas[i].PlayTime = (float)time;
                cameraPresentDatas[i].DeltaTime = (float)deltaTime;
            }
        }
        int renderMatCount = 0;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var Entities = context.renderPipelineDynamicContext.entities;
            var deviceResources = context.deviceResources;
            int countMaterials = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireMaterialBuffers(deviceResources, countMaterials);
            var graphicsContext = context.graphicsContext;
            var cameras = context.renderPipelineDynamicContext.cameras;
            ref var settings = ref context.renderPipelineDynamicContext.settings;
            ref var inShaderSettings = ref context.renderPipelineDynamicContext.inShaderSettings;
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                Marshal.StructureToPtr(inShaderSettings, pBufferData + 256, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, 0);
            }


            #region Update material data
            IntPtr ptr_rc = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
            int matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Array.Clear(rcDataUploadBuffer, 0, c_materialDataSize);
                    Marshal.StructureToPtr(Materials[j].innerStruct, ptr_rc, true);
                    WriteLightData(context.renderPipelineDynamicContext.lightings, ptr_rc + MMDMatLit.c_materialDataSize);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, c_materialDataSize, 0);
                    matIndex++;
                }
            }
            #endregion
            renderMatCount = matIndex;
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var graphicsContext = context.graphicsContext;

            RayTracingScene.NextASIndex(renderMatCount);
            RayTracingScene.NextSTIndex();


            var entities = context.renderPipelineDynamicContext.entities;
            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            for (int i = 0; i < entities.Count; i++)
                EntitySkinning(context, entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i]);
            graphicsContext.SetSOMesh(null);

            int matIndex = 0;
            if (entities.Count > 0)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    BuildEntityBAS(context, entities[i].rendererComponent, ref matIndex);
                }
                graphicsContext.BuildTopAccelerationStructures(RayTracingScene);
                RayTracingScene.BuildShaderTableStep1(context.deviceResources, c_rayGenShaderNames, c_missShaderNames, c_argumentsSizeInBytes);
                RayTracingScene.BuildShaderTableStep2(context.deviceResources, c_hitGroupNames, c_argumentsSizeInBytes, matIndex);
            }

            int cameraIndex = 0;
            graphicsContext.SetRootSignatureRayTracing(RayTracingScene);
            graphicsContext.SetComputeUAVT(context.outputRTV, 0);
            graphicsContext.SetComputeSRVT(context.EnvCubeMap, 3);
            graphicsContext.SetComputeSRVT(context.IrradianceMap, 4);

            graphicsContext.SetComputeCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);

            if (entities.Count > 0)
            {
                graphicsContext.DoRayTracing(RayTracingScene, 1);
            }
        }

        private void EntitySkinning(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer)
        {
            var graphicsContext = context.graphicsContext;
            var Materials = rendererComponent.Materials;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(cameraPresentData, 2);
            graphicsContext.SetMesh(rendererComponent.mesh);
            graphicsContext.SetSOMesh(rendererComponent.mesh);
            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
            var POSkinning = rendererComponent.POSkinning;
            if (POSkinning != null && POSkinning.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectStreamOut(POSkinning);
            else
                graphicsContext.SetPObjectStreamOut(context.RPAssetsManager.PObjectMMDSkinning);

            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
        }


        private void BuildEntityBAS(RenderPipelineContext context, MMDRendererComponent rendererComponent, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;

            var Materials = rendererComponent.Materials;
            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.textures;
            for (int i = 0; i < Materials.Count; i++)
            {
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1)
                    tex1 = texs[Materials[i].texIndex];
                tex1 = TextureStatusSelect(tex1, textureLoading, textureError, textureError);

                graphicsContext.BuildBASAndParam(RayTracingScene, rendererComponent.mesh, 0x1, indexStartLocation, Materials[i].indexCount, tex1, materialBuffers[matIndex]);
                matIndex++;

                indexStartLocation += Materials[i].indexCount;
            }
        }

        private void WriteLightData(IList<LightingData> lightings, IntPtr pBufferData)
        {
            int lightCount = 0;
            for (int j = 0; j < lightings.Count; j++)
            {
                Marshal.StructureToPtr(lightings[j].RotationOrPosition, pBufferData, true);
                Marshal.StructureToPtr((uint)lightings[j].LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(lightings[j].Color, pBufferData + 16, true);
                lightCount++;
                pBufferData += 32;
                if (lightCount >= 8)
                    break;
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
