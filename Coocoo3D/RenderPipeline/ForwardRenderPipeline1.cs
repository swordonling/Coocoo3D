using Coocoo3D.Components;
using Coocoo3D.Core;
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
    public class ForwardRenderPipeline1 : RenderPipeline
    {
        public const int c_lightingDataSize = 512;
        public const int c_offsetLightingData = 0;
        public const int c_materialDataSize = 256;
        public const int c_offsetMaterialData = c_offsetLightingData + c_lightingDataSize;
        public const int c_presentDataSize = 512;
        public const int c_offsetPresentData = c_offsetMaterialData + c_materialDataSize;
        public const int c_lightCameraCount = 2;
        public void Reload(DeviceResources deviceResources)
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Reload(deviceResources, c_presentDataSize);
            }
            for (int i = 0; i < c_lightCameraCount; i++)
            {
                LightCameraDataBuffers[i].Reload(deviceResources, c_presentDataSize);
            }
            Ready = true;
        }
        #region graphics assets
        public void Unload()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Unload();
            }
            for (int i = 0; i < c_lightCameraCount; i++)
            {
                LightCameraDataBuffers[i].Unload();
            }
        }
        #endregion

        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public ConstantBuffer[] LightCameraDataBuffers = new ConstantBuffer[c_lightCameraCount];
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
            for (int i = 0; i < c_lightCameraCount; i++)
            {
                LightCameraDataBuffers[i] = new ConstantBuffer();
            }
            gch_rcDataUploadBuffer = GCHandle.Alloc(rcDataUploadBuffer);
        }
        ~ForwardRenderPipeline1()
        {
            gch_rcDataUploadBuffer.Free();
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
            Matrix4x4 lightCameraMatrix0 = Matrix4x4.Identity;
            Matrix4x4 lightCameraMatrix1 = Matrix4x4.Identity;
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
            HasMainLight = false;
            if (lightings.Count > 0 && lightings[0].LightingType == LightingType.Directional)
            {
                lightCameraMatrix0 = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(4, camera.LookAtPoint, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix0, pBufferData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffers[0], rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);

                lightCameraMatrix1 = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(settings.ExtendShadowMapRange, camera.LookAtPoint, camera.Angle, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix1, pBufferData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffers[1], rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
                HasMainLight = true;
            }
            #region Update Entities Data
            IntPtr p0 = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);

            #region Lighting
            Array.Clear(rcDataUploadBuffer, c_offsetLightingData, c_lightingDataSize);
            int lightCount = 0;
            pBufferData = p0 + 256;
            Marshal.StructureToPtr(lightCameraMatrix0, p0, true);
            Marshal.StructureToPtr(lightCameraMatrix1, p0 + 64, true);
            for (int j = 0; j < lightings.Count; j++)
            {
                LightingData data1 = lightings[j];
                Marshal.StructureToPtr(data1.GetPositionOrDirection(), pBufferData, true);
                Marshal.StructureToPtr((uint)data1.LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(data1.Color, pBufferData + 16, true);

                lightCount++;
                pBufferData += 32;
                if (lightCount >= 8)
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
                cameraPresentDatas[i].PlayTime = (float)context.dynamicContext.Time;
                cameraPresentDatas[i].DeltaTime = (float)context.dynamicContext.DeltaTime;

                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.inShaderSettings = inShaderSettings;
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
            }

        }
        //you can fold local function in editor
        public override void RenderCamera(RenderPipelineContext context, int cameraCount)
        {
            var Entities = context.dynamicContext.entities;
            var graphicsContext = context.graphicsContext;
            ref var settings = ref context.dynamicContext.settings;
            ref var inShaderSettings = ref context.dynamicContext.inShaderSettings;
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignatureSkinning);
            graphicsContext.SetSOMesh(context.SkinningMeshBuffer);
            void EntitySkinning(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer)
            {
                var Materials = rendererComponent.Materials;
                graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                graphicsContext.SetCBVR(entityDataBuffer, 1);
                graphicsContext.SetCBVR(cameraPresentData, 2);
                var POSkinning = rendererComponent.POSkinning;
                if (POSkinning != null && POSkinning.Status == GraphicsObjectStatus.loaded)
                    graphicsContext.SetPObjectStreamOut(POSkinning);
                else
                    graphicsContext.SetPObjectStreamOut(currentSkinningPObject);
                graphicsContext.SetMeshVertex(rendererComponent.mesh);
                int indexCountAll = rendererComponent.meshVertexCount;
                graphicsContext.Draw(indexCountAll, 0);
            }
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i]);
            graphicsContext.SetSOMeshNone();


            graphicsContext.SetRootSignatureCompute(context.RPAssetsManager.rootSignatureCompute);
            void ParticleCompute(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
            {
                if (rendererComponent.ParticleCompute == null || rendererComponent.ParticleCompute.Status != GraphicsObjectStatus.loaded)
                {
                    counter.vertex += rendererComponent.meshVertexCount;
                    return;
                }
                graphicsContext.SetComputeCBVR(entityBoneDataBuffer, 0);
                //graphicsContext.SetComputeCBVR(entityDataBuffer, 1);
                graphicsContext.SetComputeCBVR(cameraPresentData, 2);
                graphicsContext.SetComputeUAVR(context.SkinningMeshBuffer, counter.vertex, 4);
                graphicsContext.SetComputeUAVR(rendererComponent.meshParticleBuffer, 0, 5);
                graphicsContext.SetPObject(rendererComponent.ParticleCompute);
                graphicsContext.Dispatch((rendererComponent.meshVertexCount + 63) / 64, 1, 1);
                counter.vertex += rendererComponent.meshVertexCount;
            }
            _Counters counterParticle = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                ParticleCompute(Entities[i].rendererComponent, cameraPresentDatas[0].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counterParticle);
            if (HasMainLight && inShaderSettings.EnableShadow)
            {
                void _RenderEntityShadow(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
                {
                    var Materials = rendererComponent.Materials;
                    graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                    graphicsContext.SetCBVR(entityDataBuffer, 1);
                    graphicsContext.SetCBVR(cameraPresentData, 2);
                    graphicsContext.SetMeshIndex(rendererComponent.mesh);

                    List<Texture2D> texs = rendererComponent.textures;
                    int countIndexLocal = 0;
                    for (int i = 0; i < Materials.Count; i++)
                    {
                        if (Materials[i].DrawFlags.HasFlag(DrawFlag.CastSelfShadow))
                        {
                            Texture2D tex1 = null;
                            if (Materials[i].texIndex != -1)
                                tex1 = texs[Materials[i].texIndex];
                            graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                            graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                            graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                        }
                        counter.material++;
                        countIndexLocal += Materials[i].indexCount;
                    }
                    counter.vertex += rendererComponent.meshVertexCount;
                }

                graphicsContext.SetMesh(context.SkinningMeshBuffer);
                graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
                graphicsContext.SetPObjectDepthOnly(context.RPAssetsManager.PObjectMMDShadowDepth);
                graphicsContext.SetAndClearDSV(context.ShadowMap0);
                _Counters counterShadow0 = new _Counters();
                for (int i = 0; i < Entities.Count; i++)
                    _RenderEntityShadow(Entities[i].rendererComponent, LightCameraDataBuffers[0], context.CBs_Bone[i], entityDataBuffers[i], ref counterShadow0);
                graphicsContext.SetAndClearDSV(context.ShadowMap1);
                _Counters counterShadow1 = new _Counters();
                for (int i = 0; i < Entities.Count; i++)
                    _RenderEntityShadow(Entities[i].rendererComponent, LightCameraDataBuffers[1], context.CBs_Bone[i], entityDataBuffers[i], ref counterShadow1);
            }


            int cameraIndex = 0;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.ScreenSizeDSVs[0], Vector4.Zero);
            graphicsContext.SetCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
            graphicsContext.SetSRVT(context.ShadowMap0, 6);
            graphicsContext.SetSRVT(context.EnvCubeMap, 7);
            graphicsContext.SetSRVT(context.IrradianceMap, 8);
            graphicsContext.SetSRVT(context.BRDFLut, 9);
            graphicsContext.SetSRVT(context.ShadowMap1, 10);
            #region Render Sky box
            graphicsContext.SetPObject(context.RPAssetsManager.PObjectSkyBox, CullMode.back);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexed(context.ndcQuadMeshIndexCount, 0, 0);
            #endregion

            graphicsContext.SetSRVT(context.EnvironmentMap, 7);
            graphicsContext.SetMesh(context.SkinningMeshBuffer);

            void ZPass(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
            {
                var Materials = rendererComponent.Materials;
                graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                graphicsContext.SetCBVR(entityDataBuffer, 1);
                graphicsContext.SetCBVR(cameraPresentData, 2);
                List<Texture2D> texs = rendererComponent.textures;
                graphicsContext.SetMeshIndex(rendererComponent.mesh);
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
                    graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                    counter.material++;
                    countIndexLocal += Materials[i].indexCount;
                }
                counter.vertex += rendererComponent.meshVertexCount;
            }
            _Counters counter1 = new _Counters();
            if (context.dynamicContext.settings.ZPrepass)
                for (int i = 0; i < Entities.Count; i++)
                    ZPass(Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counter1);

            void _RenderEntity(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ConstantBuffer entityDataBuffer, ref _Counters counter)
            {
                var PODraw = PObjectStatusSelect(rendererComponent.PODraw, context.RPAssetsManager.PObjectMMDLoading, currentDrawPObject, context.RPAssetsManager.PObjectMMDError);
                var Materials = rendererComponent.Materials;
                List<Texture2D> texs = rendererComponent.textures;
                graphicsContext.SetMeshIndex(rendererComponent.mesh);
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
                    if (Materials[i].DrawFlags.HasFlag(DrawFlag.DrawDoubleFace))
                        cullMode = CullMode.none;
                    graphicsContext.SetPObject(PODraw, cullMode);
                    graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                    counter.material++;
                    countIndexLocal += Materials[i].indexCount;
                }
                counter.vertex += rendererComponent.meshVertexCount;
            }
            _Counters counter2 = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                _RenderEntity(Entities[i].rendererComponent, cameraPresentDatas[cameraIndex].DataBuffer, context.CBs_Bone[i], entityDataBuffers[i], ref counter2);
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
    }
}