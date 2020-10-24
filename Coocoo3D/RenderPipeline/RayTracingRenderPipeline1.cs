
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
        const int c_presentDataSize = 512;
        const int c_lightCameraDataSize = 256;

        struct _Counters
        {
            public int material;
            public int vertex;
        }

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
        public ConstantBuffer LightCameraDataBuffer = new ConstantBuffer();

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
            LightCameraDataBuffer.Reload(deviceResources, c_lightCameraDataSize);
        }

        #region graphics assets
        static readonly string[] c_rayGenShaderNames = { "MyRaygenShader", "MyRaygenShader1" };
        static readonly string[] c_missShaderNames = { "MissShaderSurface", "MissShaderTest", };
        static readonly string[] c_hitGroupNames = new string[] { "HitGroupSurface", "HitGroupTest", };
        HitGroupDesc[] hitGroupDescs = new HitGroupDesc[]
        {
            new HitGroupDesc { HitGroupName = "HitGroupSurface", AnyHitName = "AnyHitShaderSurface", ClosestHitName = "ClosestHitShaderSurface" },
            new HitGroupDesc { HitGroupName = "HitGroupTest", AnyHitName = "AnyHitShaderTest", ClosestHitName = "ClosestHitShaderTest" },
        };
        static readonly string[] c_exportNames = new string[] { "MyRaygenShader", "MyRaygenShader1", "ClosestHitShaderSurface", "ClosestHitShaderTest", "MissShaderSurface", "MissShaderTest", "AnyHitShaderSurface", "AnyHitShaderTest", };

        public async Task ReloadAssets(DeviceResources deviceResources)
        {
            RayTracingScene.ReloadLibrary(await ReadFile("ms-appx:///Coocoo3DGraphics/Raytracing.cso"));
            RayTracingScene.ReloadPipelineStates(deviceResources, c_exportNames, hitGroupDescs, c_rayTracingSceneSettings);
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
        bool HasMainLight;
        int renderMatCount = 0;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var Entities = context.dynamicContext.entities;
            var deviceResources = context.deviceResources;
            int countMaterials = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireMaterialBuffers(deviceResources, countMaterials);
            var graphicsContext = context.graphicsContext;
            var cameras = context.dynamicContext.cameras;
            var camera = context.dynamicContext.cameras[0];
            ref var settings = ref context.dynamicContext.settings;
            ref var inShaderSettings = ref context.dynamicContext.inShaderSettings;
            var lightings = context.dynamicContext.lightings;

            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, 0);
            Matrix4x4 lightCameraMatrix = Matrix4x4.Identity;
            HasMainLight = false;
            if (lightings.Count > 0 && lightings[0].LightingType == LightingType.Directional)
            {
                lightCameraMatrix = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(512, camera.LookAtPoint, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix, pBufferData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffer, rcDataUploadBuffer, c_lightCameraDataSize, 0);
                HasMainLight = true;
            }

            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.inShaderSettings = inShaderSettings;
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                Marshal.StructureToPtr(lightCameraMatrix, pBufferData + 256, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, 0);
            }


            #region Update material data
            _Counters counterMaterial = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Array.Clear(rcDataUploadBuffer, 0, c_materialDataSize);
                    Marshal.StructureToPtr(Materials[j].innerStruct, pBufferData, true);
                    Marshal.StructureToPtr(counterMaterial.vertex, pBufferData + 240, true);
                    WriteLightData(lightings, pBufferData + MMDMatLit.c_materialDataSize);
                    graphicsContext.UpdateResource(materialBuffers[counterMaterial.material], rcDataUploadBuffer, c_materialDataSize, 0);
                    counterMaterial.material++;
                }
                counterMaterial.vertex += Entities[i].rendererComponent.meshVertexCount;
            }
            #endregion
            renderMatCount = counterMaterial.material;
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var graphicsContext = context.graphicsContext;

            RayTracingScene.NextASIndex(renderMatCount);
            RayTracingScene.NextSTIndex();


            var entities = context.dynamicContext.entities;
            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            graphicsContext.SetSOMesh(context.SkinningMeshBuffer);
            for (int i = 0; i < entities.Count; i++)
                EntitySkinning(context, entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i]);
            graphicsContext.SetSOMesh((MeshBuffer)null);

            graphicsContext.SetRootSignatureCompute(context.RPAssetsManager.rootSignatureCompute);
            _Counters counterParticle = new _Counters();
            for (int i = 0; i < entities.Count; i++)
                ParticleCompute(context, entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], null, ref counterParticle);

            if (HasMainLight && context.dynamicContext.inShaderSettings.EnableShadow)
            {
                graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
                graphicsContext.SetAndClearDSV(context.DSV0);
                graphicsContext.SetMesh(context.SkinningMeshBuffer);
                _Counters counterShadow = new _Counters();
                for (int i = 0; i < entities.Count; i++)
                    RenderEntityShadow(context, entities[i].rendererComponent, LightCameraDataBuffer, ref counterShadow);
            }


            _Counters counter1 = new _Counters();
            if (entities.Count > 0)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    BuildEntityBAS1(context, entities[i].rendererComponent, ref counter1);
                }
                graphicsContext.BuildTopAccelerationStructures(RayTracingScene);
                RayTracingScene.BuildShaderTable(context.deviceResources, c_rayGenShaderNames, c_missShaderNames, c_hitGroupNames, counter1.material);
                int cameraIndex = 0;
                graphicsContext.SetRootSignatureRayTracing(RayTracingScene);
                graphicsContext.SetComputeUAVT(context.outputRTV, 0);
                graphicsContext.SetComputeCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
                graphicsContext.SetComputeSRVT(context.EnvCubeMap, 3);
                graphicsContext.SetComputeSRVT(context.IrradianceMap, 4);
                graphicsContext.SetComputeSRVT(context.BRDFLut, 5);
                graphicsContext.SetComputeSRVT(context.DSV0, 6);
                graphicsContext.SetComputeSRVR(context.SkinningMeshBuffer, 0, 7);
                graphicsContext.SetComputeUAVR(context.LightCacheBuffer, context.dynamicContext.frameRenderIndex % 2, 8);
                graphicsContext.SetComputeSRVR(context.LightCacheBuffer, (context.dynamicContext.frameRenderIndex + 1) % 2, 9);

                graphicsContext.DoRayTracing(RayTracingScene, context.dynamicContext.VertexCount, 1, 1);

                graphicsContext.SetComputeUAVR(context.LightCacheBuffer, (context.dynamicContext.frameRenderIndex + 1) % 2, 8);
                graphicsContext.SetComputeSRVR(context.LightCacheBuffer, context.dynamicContext.frameRenderIndex % 2, 9);
                graphicsContext.DoRayTracing(RayTracingScene, context.width, context.height, 0);

                //graphicsContext.SetComputeUAVT(context.outputRTV, 0);
            }
            else
            {
                #region Render Sky box
                int cameraIndex = 0;
                graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
                graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.ScreenSizeDSVs[0], Vector4.Zero);
                graphicsContext.SetCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
                graphicsContext.SetSRVT(context.DSV0, 6);
                graphicsContext.SetSRVT(context.EnvCubeMap, 7);
                graphicsContext.SetSRVT(context.IrradianceMap, 8);
                graphicsContext.SetSRVT(context.BRDFLut, 9);
                graphicsContext.SetPObject(context.RPAssetsManager.PObjectSkyBox, CullMode.back);
                graphicsContext.SetMesh(context.ndcQuadMesh);
                graphicsContext.DrawIndexed(context.ndcQuadMesh.m_indexCount, 0, 0);
                #endregion
            }
        }

        private void EntitySkinning(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer)
        {
            var graphicsContext = context.graphicsContext;
            var Materials = rendererComponent.Materials;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);
            var POSkinning = rendererComponent.POSkinning;
            if (POSkinning != null && POSkinning.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectStreamOut(POSkinning);
            else
                graphicsContext.SetPObjectStreamOut(context.RPAssetsManager.PObjectMMDSkinning);
#if _TEST
            graphicsContext.SetMeshVertex(rendererComponent.mesh);
            int indexCountAll = rendererComponent.meshVertexCount;
#else
            graphicsContext.SetMesh(rendererComponent.mesh);
            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
#endif

#if _TEST
            graphicsContext.Draw(indexCountAll, 0);
#else
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
#endif
        }

        private void BuildEntityBAS1(RenderPipelineContext context, MMDRendererComponent rendererComponent, ref _Counters counter)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;

            var Materials = rendererComponent.Materials;
            List<Texture2D> texs = rendererComponent.textures;

            int countIndexLocal = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1)
                    tex1 = texs[Materials[i].texIndex];
                tex1 = TextureStatusSelect(tex1, textureLoading, textureError, textureError);

                graphicsContext.BuildBASAndParam(RayTracingScene, context.SkinningMeshBuffer, rendererComponent.mesh, 0x1, counter.vertex, countIndexLocal, Materials[i].indexCount, tex1, materialBuffers[counter.material]);
                counter.material++;
                countIndexLocal += Materials[i].indexCount;
            }
            counter.vertex += rendererComponent.meshVertexCount;
        }

        private void WriteLightData(IList<LightingData> lightings, IntPtr pBufferData)
        {
            int lightCount = 0;
            for (int j = 0; j < lightings.Count; j++)
            {
                Marshal.StructureToPtr(lightings[j].GetPositionOrDirection(), pBufferData, true);
                Marshal.StructureToPtr((uint)lightings[j].LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(lightings[j].Color, pBufferData + 16, true);
                lightCount++;
                pBufferData += 32;
                if (lightCount >= 8)
                    break;
            }
        }

        private void ParticleCompute(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
        {
            if (rendererComponent.ParticleCompute == null || rendererComponent.ParticleCompute.Status != GraphicsObjectStatus.loaded)
            {
                counter.vertex += rendererComponent.meshIndexCount;
                return;
            }
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetComputeCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetComputeCBVR(entityDataBuffer, 1);
            graphicsContext.SetComputeCBVR(cameraPresentData, 2);
            graphicsContext.SetComputeUAVR(context.SkinningMeshBuffer, counter.vertex, 4);
            graphicsContext.SetComputeUAVR(rendererComponent.meshParticleBuffer, 0, 5);
            graphicsContext.SetPObject(rendererComponent.ParticleCompute);
            graphicsContext.Dispatch((rendererComponent.mesh.m_indexCount / 3 + 63) / 64, 1, 1);
            counter.vertex += rendererComponent.meshIndexCount;
        }

        private void RenderEntityShadow(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ref _Counters counter)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var Materials = rendererComponent.Materials;
            var graphicsContext = context.graphicsContext;
            //graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);

#if _TEST
            graphicsContext.SetMeshIndex(rendererComponent.mesh);
#endif
            List<Texture2D> texs = rendererComponent.textures;
            graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDShadowDepth);

            int countIndexLocal = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(Coocoo3DNativeInteroperable.NMMDE_DrawFlag.CastSelfShadow))
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                    graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
#if _TEST
                    graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
#else
                    graphicsContext.Draw(Materials[i].indexCount, counter.vertex+countIndexLocal);
#endif
                }
                counter.material++;
                countIndexLocal += Materials[i].indexCount;
            }
            counter.vertex += rendererComponent.meshVertexCount;
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
