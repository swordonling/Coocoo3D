using Coocoo3D.Components;
using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using Coocoo3DNativeInteroperable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class ForwardRenderPipeline1 : RenderPipeline
    {
        public const int c_lightingDataSize = 384;
        public const int c_offsetLightingData = 0;
        public const int c_materialDataSize = 256;
        public const int c_offsetMaterialData = c_offsetLightingData + c_lightingDataSize;
        public const int c_presentDataSize = 512;
        public const int c_offsetPresentData = c_offsetMaterialData + c_materialDataSize;
        public void Reload(DeviceResources deviceResources)
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Reload(deviceResources, c_presentDataSize);
            }
            LightCameraDataBuffer.Reload(deviceResources, c_presentDataSize);
            Ready = true;
        }
        #region graphics assets
        public void Unload()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Unload();
            }
            LightCameraDataBuffer.Unload();
        }
        #endregion

        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public ConstantBuffer LightCameraDataBuffer = new ConstantBuffer();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBuffer> materialBuffers = new List<ConstantBuffer>();
        byte[] rcDataUploadBuffer = new byte[c_lightingDataSize + c_materialDataSize + c_presentDataSize];
        public GCHandle gch_rcDataUploadBuffer;

        struct _Counters
        {
            public int material;
            public int vertex;
        }

        public ForwardRenderPipeline1()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i] = new PresentData();
            }
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
        }
        ~ForwardRenderPipeline1()
        {
            gch_rcDataUploadBuffer.Free();
        }

        public override void TimeChange(double time, double deltaTime)
        {
            for (int i = 0; i < cameraPresentDatas.Length; i++)
            {
                cameraPresentDatas[i].PlayTime = (float)time;
                cameraPresentDatas[i].DeltaTime = (float)deltaTime;
            }
        }

        bool HasMainLight;
        PObject currentDrawPObject;
        PObject currentSkinningPObject;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var deviceResources = context.deviceResources;
            var cameras = context.dynamicContext.cameras;
            var graphicsContext = context.graphicsContext;
            ref var settings = ref context.dynamicContext.settings;
            ref var inShaderSettings = ref context.dynamicContext.inShaderSettings;
            var Entities = context.dynamicContext.entities;
            var lightings = context.dynamicContext.lightings;

            int countMaterials = 0;
            currentSkinningPObject = context.RPAssetsManager.PObjectMMDSkinning;
            if (settings.RenderStyle == 1)
                currentDrawPObject = context.RPAssetsManager.PObjectMMD_Toon1;
            else if (inShaderSettings.Quality == 0)
                currentDrawPObject = context.RPAssetsManager.PObjectMMD;
            else
                currentDrawPObject = context.RPAssetsManager.PObjectMMD_DisneyBrdf;

            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireEntityBuffers(deviceResources, Entities.Count);
            DesireMaterialBuffers(deviceResources, countMaterials);
            var camera = context.dynamicContext.cameras[0];
            Matrix4x4 lightCameraMatrix = Matrix4x4.Identity;
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
            HasMainLight = false;
            if (lightings.Count > 0 && lightings[0].LightingType == LightingType.Directional)
            {
                lightCameraMatrix = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(settings.ExtendShadowMapRange, camera.LookAtPoint, camera.Angle, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix, pBufferData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
                HasMainLight = true;
            }
            #region Update Entities Data
            IntPtr p0 = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);

            #region Lighting
            Array.Clear(rcDataUploadBuffer, c_offsetLightingData, c_lightingDataSize);
            int lightCount = 0;
            pBufferData = p0 + 64;
            Marshal.StructureToPtr(lightCameraMatrix, p0, true);
            for (int j = 0; j < lightings.Count; j++)
            {
                LightingData data1 = lightings[j];
                Marshal.StructureToPtr(data1.GetPositionOrDirection(), pBufferData, true);
                Marshal.StructureToPtr((uint)data1.LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(data1.Color, pBufferData + 16, true);

                lightCount++;
                pBufferData += 32;
                if (lightCount >= 4)
                    break;
            }
            #endregion

            for (int i = 0; i < Entities.Count; i++)
            {
                graphicsContext.UpdateResource(entityDataBuffers[i], rcDataUploadBuffer, c_lightingDataSize, c_offsetLightingData);
            }
            #endregion

            #region Update material data
            int matIndex = 0;
            pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetMaterialData);
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Marshal.StructureToPtr(Materials[j].innerStruct, pBufferData, true);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, MMDMatLit.c_materialDataSize, c_offsetMaterialData);
                    matIndex++;
                }
            }
            #endregion

            pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.inShaderSettings = inShaderSettings;
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
            }

        }

        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var Entities = context.dynamicContext.entities;
            var graphicsContext = context.graphicsContext;
            ref var settings = ref context.dynamicContext.settings;
            ref var inShaderSettings = ref context.dynamicContext.inShaderSettings;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            //graphicsContext.SetSRVT((RenderTexture2D)null, 6);//may used in d3d11
            graphicsContext.SetAndClearDSV(context.DSV0);
            graphicsContext.SetSOMesh(context.SkinningMeshBuffer);
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(context, Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i]);
            graphicsContext.SetSOMesh((MeshBuffer)null);
            graphicsContext.SetRootSignatureCompute(context.RPAssetsManager.rootSignatureCompute);
            _Counters counterParticle = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                ParticleCompute(context, Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counterParticle);
            if (HasMainLight && inShaderSettings.EnableShadow)
            {
                graphicsContext.SetMesh(context.SkinningMeshBuffer);

                graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
                _Counters counterShadow = new _Counters();
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityShadow(context, Entities[i].rendererComponent, LightCameraDataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counterShadow);
            }


            int cameraIndex = 0;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.ScreenSizeDSVs[0], Vector4.Zero);
            graphicsContext.SetCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
            graphicsContext.SetSRVT(context.DSV0, 6);
            graphicsContext.SetSRVT(context.EnvCubeMap, 7);
            graphicsContext.SetSRVT(context.IrradianceMap, 8);
            graphicsContext.SetSRVT(context.BRDFLut, 9);
            #region Render Sky box
            graphicsContext.SetPObject(context.RPAssetsManager.PObjectSkyBox, CullMode.back);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexed(context.ndcQuadMesh.m_indexCount, 0, 0);
            #endregion

            graphicsContext.SetMesh(context.SkinningMeshBuffer);
            _Counters counter1 = new _Counters();
            if (context.dynamicContext.settings.ZPrepass)
                for (int i = 0; i < Entities.Count; i++)
                    ZPass(context, Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counter1);
            _Counters counter2 = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                RenderEntity(context, Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counter2);
        }

        private void DesireEntityBuffers(DeviceResources deviceResources, int count)
        {
            while (entityDataBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_lightingDataSize);
                entityDataBuffers.Add(constantBuffer);
            }
        }

        private void DesireMaterialBuffers(DeviceResources deviceResources, int count)
        {
            while (materialBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, MMDMatLit.c_materialDataSize);
                materialBuffers.Add(constantBuffer);
            }
        }

        private void EntitySkinning(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer)
        {
            var graphicsContext = context.graphicsContext;
            var Materials = rendererComponent.Materials;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);
            var POSkinning = rendererComponent.POSkinning;
            if (POSkinning != null && POSkinning.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectStreamOut(POSkinning);
            else
                graphicsContext.SetPObjectStreamOut(currentSkinningPObject);
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
        private void RenderEntityShadow(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var Materials = rendererComponent.Materials;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);

#if _TEST
            graphicsContext.SetMeshIndex(rendererComponent.mesh);
#endif
            List<Texture2D> texs = rendererComponent.textures;
            graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDShadowDepth);

            int countIndexLocal = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.CastSelfShadow))
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

        private void RenderEntity(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;

            var PODraw = PObjectStatusSelect(rendererComponent.PODraw, context.RPAssetsManager.PObjectMMDLoading, currentDrawPObject, context.RPAssetsManager.PObjectMMDError);
            var Materials = rendererComponent.Materials;
            List<Texture2D> texs = rendererComponent.textures;
#if _TEST
            graphicsContext.SetMeshIndex(rendererComponent.mesh);
#endif
            //graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetCBVR(entityDataBuffer, 1);
            //graphicsContext.SetCBVR(cameraPresentData, 2);
            CooGExtension.SetCBVBuffer3(graphicsContext, entityBoneDataBuffer, entityDataBuffer, cameraPresentData, 0);
            int countIndexLocal = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].innerStruct.DiffuseColor.W <= 0)
                {
                    counter.material++;
                    countIndexLocal += Materials[i].indexCount;
                    continue;
                }
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1 && Materials[i].texIndex < Materials.Count)
                    tex1 = texs[Materials[i].texIndex];
                Texture2D tex2 = null;
                if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    tex2 = texs[Materials[i].toonIndex];
                graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex2, textureLoading, textureError, textureError), 5);
                CooGExtension.SetSRVTexture2(graphicsContext, tex1, tex2, 4, textureLoading, textureError);
                CullMode cullMode = CullMode.back;
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.DrawDoubleFace))
                    cullMode = CullMode.none;
                graphicsContext.SetPObject(PODraw, cullMode);
#if _TEST
                graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
#else
                graphicsContext.Draw(Materials[i].indexCount, counter.vertex+countIndexLocal);
#endif
                counter.material++;
                countIndexLocal += Materials[i].indexCount;
            }
            counter.vertex += rendererComponent.meshVertexCount;
        }

        private void ZPass(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var Materials = rendererComponent.Materials;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);
            List<Texture2D> texs = rendererComponent.textures;
#if _TEST
            graphicsContext.SetMeshIndex(rendererComponent.mesh);
#endif
            graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDDepth);
            //graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            int countIndexLocal = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1)
                    tex1 = texs[Materials[i].texIndex];
                graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
#if _TEST
                graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
#else
                graphicsContext.Draw(Materials[i].indexCount, counter.vertex + countIndexLocal);
#endif
                counter.material++;
                countIndexLocal += Materials[i].indexCount;
            }
            counter.vertex += rendererComponent.meshVertexCount;
        }
        private void ParticleCompute(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
        {
            if (rendererComponent.ParticleCompute == null || rendererComponent.ParticleCompute.Status != GraphicsObjectStatus.loaded)
            {
                counter.vertex += rendererComponent.meshVertexCount;
                return;
            }
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetComputeCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetComputeCBVR(entityDataBuffer, 1);
            graphicsContext.SetComputeCBVR(cameraPresentData, 2);
            graphicsContext.SetComputeUAVR(context.SkinningMeshBuffer, counter.vertex, 4);
            graphicsContext.SetComputeUAVR(rendererComponent.meshParticleBuffer, 0, 5);
            graphicsContext.SetPObject(rendererComponent.ParticleCompute);
            graphicsContext.Dispatch((rendererComponent.meshVertexCount + 63) / 64, 1, 1);
            counter.vertex += rendererComponent.meshVertexCount;
        }
    }
}