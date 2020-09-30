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
            lightingCameraPresentData.Reload(deviceResources, c_presentDataSize);
            Ready = true;
        }
        #region graphics assets
        public void Unload()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Unload();
            }
            lightingCameraPresentData.Unload();
        }
        #endregion

        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public PresentData lightingCameraPresentData = new PresentData();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBuffer> materialBuffers = new List<ConstantBuffer>();
        byte[] rcDataUploadBuffer = new byte[c_lightingDataSize + c_materialDataSize + c_presentDataSize];
        public GCHandle gch_rcDataUploadBuffer;

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
            lightingCameraPresentData.PlayTime = (float)time;
            lightingCameraPresentData.DeltaTime = (float)deltaTime;
        }

        int mainLightIndex;
        PObject currentDrawPObject;
        PObject currentSkinningPObject;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var deviceResources = context.deviceResources;
            var cameras = context.renderPipelineDynamicContext.cameras;
            var graphicsContext = context.graphicsContext;
            ref var settings = ref context.renderPipelineDynamicContext.settings;
            ref var inShaderSettings = ref context.renderPipelineDynamicContext.inShaderSettings;
            var Entities = context.renderPipelineDynamicContext.entities;
            var lightings = context.renderPipelineDynamicContext.lightings;

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
            mainLightIndex = -1;
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
            for (int i = 0; i < lightings.Count; i++)
            {
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    lightingCameraPresentData.UpdateCameraData(lightings[i]); ;
                    Marshal.StructureToPtr(lightingCameraPresentData.innerStruct, pBufferData, true);
                    graphicsContext.UpdateResource(lightingCameraPresentData.DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
                    mainLightIndex = i;
                    break;
                }
            }
            #region Update Entities Data
            IntPtr p0 = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);
            pBufferData = p0;
            for (int i = 0; i < Entities.Count; i++)
            {
                MMD3DEntity entity = Entities[i];
                #region Lighting
                Array.Clear(rcDataUploadBuffer, c_offsetLightingData, c_lightingDataSize);
                int lightCount = 0;
                pBufferData = p0;
                if (mainLightIndex != -1)
                {
                    Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[mainLightIndex].rotateMatrix), pBufferData, true);
                    Marshal.StructureToPtr((uint)lightings[mainLightIndex].LightingType, pBufferData + 12, true);
                    Marshal.StructureToPtr(lightings[mainLightIndex].Color, pBufferData + 16, true);
                    Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[mainLightIndex].vpMatrix), pBufferData + 32, true);
                    lightCount++;
                    pBufferData += 96;
                }
                for (int j = 0; j < lightings.Count; j++)
                {
                    if (j != mainLightIndex)
                    {
                        if (lightings[j].LightingType == LightingType.Directional)
                            Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[j].rotateMatrix), pBufferData, true);
                        else
                            Marshal.StructureToPtr(lightings[j].Rotation * 180 / MathF.PI, pBufferData, true);
                        Marshal.StructureToPtr((uint)lightings[j].LightingType, pBufferData + 12, true);
                        Marshal.StructureToPtr(lightings[j].Color, pBufferData + 16, true);
                        Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[j].vpMatrix), pBufferData + 32, true);
                        lightCount++;
                        pBufferData += 96;
                        if (lightCount >= 4)
                            break;
                    }
                }
                #endregion

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
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                Marshal.StructureToPtr(inShaderSettings, pBufferData + 256, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
            }

        }

        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var Entities = context.renderPipelineDynamicContext.entities;
            var graphicsContext = context.graphicsContext;
            ref var settings = ref context.renderPipelineDynamicContext.settings;
            ref var inShaderSettings = ref context.renderPipelineDynamicContext.inShaderSettings;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            //graphicsContext.SetSRVT((RenderTexture2D)null, 6);//may used in d3d11
            graphicsContext.SetAndClearDSV(context.DSV0);
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(context, Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i]);
            graphicsContext.SetSOMesh(null);
            int matIndex = 0;
            if (mainLightIndex != -1 && inShaderSettings.EnableShadow)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityShadow(context, Entities[i].rendererComponent, lightingCameraPresentData.DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref matIndex);
            }
            graphicsContext.SetRootSignatureCompute(context.RPAssetsManager.rootSignatureCompute);
            for (int i = 0; i < Entities.Count; i++)
                ParticleCompute(context, Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i]);


            int cameraIndex = 0;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.ScreenSizeDSVs[0], Vector4.Zero);
            graphicsContext.SetCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
            graphicsContext.SetSRVT(context.DSV0, 6);
            graphicsContext.SetSRVT(context.EnvCubeMap, 7);
            graphicsContext.SetSRVT(context.IrradianceMap, 8);
            //渲染天空盒
            graphicsContext.SetPObject(context.RPAssetsManager.PObjectSkyBox, CullMode.back);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexed(context.ndcQuadMesh.m_indexCount, 0, 0);

            matIndex = 0;
            if (context.renderPipelineDynamicContext.settings.ZPrepass)
                for (int i = 0; i < Entities.Count; i++)
                    ZPass(context, Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref matIndex);
            matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
                RenderEntity(context, Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref matIndex);
            matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
                RenderParticle(context, Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref matIndex);
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
            GraphicsContext graphicsContext = context.graphicsContext;
            var Materials = rendererComponent.Materials;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
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
                graphicsContext.SetPObjectStreamOut(currentSkinningPObject);

            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
        }
        private void RenderEntityShadow(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var Materials = rendererComponent.Materials;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);

            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.textures;
            graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDShadowDepth);
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.CastSelfShadow))
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                    graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                    graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                }
                matIndex++;
                indexStartLocation += Materials[i].indexCount;
            }
        }

        private void ZPass(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var Materials = rendererComponent.Materials;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData, 2);

            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.textures;
            graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDDepth);
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.CastSelfShadow))
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                    graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                    graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                }
                matIndex++;
                indexStartLocation += Materials[i].indexCount;
            }
        }

        private void RenderEntity(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            var PODraw = PObjectStatusSelect(rendererComponent.PODraw, context.RPAssetsManager.PObjectMMDLoading, currentDrawPObject, context.RPAssetsManager.PObjectMMDError);
            var Materials = rendererComponent.Materials;
            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.textures;
            //graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetCBVR(entityDataBuffer, 1);
            //graphicsContext.SetCBVR(cameraPresentData, 2);
            CooGExtension.SetCBVBuffer3(graphicsContext, entityBoneDataBuffer, entityDataBuffer, cameraPresentData, 0);
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].innerStruct.DiffuseColor.W <= 0)
                {
                    matIndex++;
                    indexStartLocation += Materials[i].indexCount;
                    continue;
                }
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1 && Materials[i].texIndex < Materials.Count)
                    tex1 = texs[Materials[i].texIndex];
                Texture2D tex2 = null;
                if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    tex2 = texs[Materials[i].toonIndex];
                graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex2, textureLoading, textureError, textureError), 5);
                CooGExtension.SetSRVTexture2(graphicsContext, tex1, tex2, 4, textureLoading, textureError);
                CullMode cullMode = CullMode.back;
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.DrawDoubleFace))
                    cullMode = CullMode.none;
                graphicsContext.SetPObject(PODraw, cullMode);
                graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                matIndex++;
                indexStartLocation += Materials[i].indexCount;
            }
        }
        private void ParticleCompute(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer)
        {
            if (rendererComponent.ParticleCompute == null || rendererComponent.ParticleCompute.Status != GraphicsObjectStatus.loaded) return;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetComputeCBVR(entityBoneDataBuffer, 0);
            graphicsContext.SetComputeCBVR(entityDataBuffer, 1);
            graphicsContext.SetComputeCBVR(cameraPresentData, 2);
            graphicsContext.SetComputeSRVRSkinnedMesh(rendererComponent.mesh, 3);
            graphicsContext.SetComputeUAVR(rendererComponent.dynamicMesh, 4);
            graphicsContext.SetComputeUAVR(rendererComponent.meshParticleBuffer, 0, 5);
            graphicsContext.SetPObject(rendererComponent.ParticleCompute);
            graphicsContext.Dispatch((rendererComponent.mesh.m_indexCount / 3 + 63) / 64, 1, 1);
        }
        private void RenderParticle(RenderPipelineContext context, MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            var Materials = rendererComponent.Materials;
            if (rendererComponent.POParticleDraw == null || rendererComponent.POParticleDraw.Status != GraphicsObjectStatus.loaded)
            {
                matIndex += Materials.Count;
                return;
            }
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetMesh(rendererComponent.dynamicMesh);
            var PODraw = PObjectStatusSelect(rendererComponent.POParticleDraw, context.RPAssetsManager.PObjectMMDLoading, context.RPAssetsManager.PObjectMMDError, context.RPAssetsManager.PObjectMMDError);
            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.textures;
            //graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
            //graphicsContext.SetCBVR(entityDataBuffer, 1);
            //graphicsContext.SetCBVR(cameraPresentData, 2);
            CooGExtension.SetCBVBuffer3(graphicsContext, entityBoneDataBuffer, entityDataBuffer, cameraPresentData, 0);
            for (int i = 0; i < Materials.Count; i++)
            {
                Texture2D tex1 = null;
                if (Materials[i].texIndex != -1 && Materials[i].texIndex < Materials.Count)
                    tex1 = texs[Materials[i].texIndex];
                Texture2D tex2 = null;
                if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    tex2 = texs[Materials[i].toonIndex];
                graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                //graphicsContext.SetSRVT(TextureStatusSelect(tex2, textureLoading, textureError, textureError), 5);
                CooGExtension.SetSRVTexture2(graphicsContext, tex1, tex2, 4, textureLoading, textureError);
                graphicsContext.SetPObject(PODraw, CullMode.none);
                graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                matIndex++;
                indexStartLocation += Materials[i].indexCount;
            }
        }
    }
}