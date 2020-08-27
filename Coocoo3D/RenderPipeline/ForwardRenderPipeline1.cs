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
        public const int c_entityDataSize = 128;
        public const int c_offsetEntityData = 0;
        public const int c_lightingDataSize = 384;
        public const int c_offsetLightingData = c_offsetEntityData + c_entityDataSize;
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
            rootSignature.ReloadMMD(deviceResources);
        }
        #region graphics assets
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public VertexShader VSSkyBox = new VertexShader();
        public GeometryShader GSMMD = new GeometryShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMD_DisneyBrdf = new PixelShader();
        public PixelShader PSMMD_Toon1 = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader PSMMDAlphaClip = new PixelShader();
        public PixelShader PSSkyBox = new PixelShader();
        public PObject PObjectMMD = new PObject();
        public PObject PObjectMMD_DisneyBrdf = new PObject();
        public PObject PObjectMMD_Toon1 = new PObject();
        public PObject PObjectMMDDepth = new PObject();
        public PObject PObjectMMDLoading = new PObject();
        public PObject PObjectMMDError = new PObject();
        public PObject PObjectSkyBox = new PObject();
        public PObject currentUsedPObject;
        public DxgiFormat CurrentRenderTargetFormat;
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMDSkinning2, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");
            await ReloadVertexShader(VSSkyBox, deviceResources, "ms-appx:///Coocoo3DGraphics/VSSkyBox.cso");
            await ReloadPixelShader(PSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMD_DisneyBrdf, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD_DisneyBRDF.cso");
            await ReloadPixelShader(PSMMD_Toon1, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD_Toon1.cso");
            await ReloadPixelShader(PSMMDLoading, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");
            await ReloadPixelShader(PSMMDAlphaClip, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip.cso");
            await ReloadPixelShader(PSSkyBox, deviceResources, "ms-appx:///Coocoo3DGraphics/PSSkyBox.cso");
            await ReloadGeometryShader(GSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/GSMMD.cso");
        }
        public void Unload()
        {
            CurrentRenderTargetFormat = DxgiFormat.DXGI_FORMAT_UNKNOWN;
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Unload();
            }
            rootSignature.Unload();
            lightingCameraPresentData.Unload();
        }
        public void ChangeRenderTargetFormat(DeviceResources deviceResources, DxgiFormat format)
        {
            CurrentRenderTargetFormat = format;
            PObjectMMD.Reload2(deviceResources, rootSignature, VSMMDSkinning2, GSMMD, PSMMD, VSMMDTransform, format);
            PObjectMMD_DisneyBrdf.Reload2(deviceResources, rootSignature, VSMMDSkinning2, GSMMD, PSMMD_DisneyBrdf, VSMMDTransform, format);
            PObjectMMD_Toon1.Reload2(deviceResources, rootSignature, VSMMDSkinning2, GSMMD, PSMMD_Toon1, VSMMDTransform, format);
            PObjectSkyBox.Reload(deviceResources, rootSignature, PObjectType.postProcess, VSSkyBox, null, PSSkyBox, format);
            PObjectMMDLoading.Reload2(deviceResources, rootSignature, VSMMDSkinning2, GSMMD, PSMMDLoading, VSMMDTransform, format);
            PObjectMMDError.Reload2(deviceResources, rootSignature, VSMMDSkinning2, GSMMD, PSMMDError, VSMMDTransform, format);
            PObjectMMDDepth.ReloadDepthOnly(deviceResources, rootSignature, VSMMDTransform, PSMMDAlphaClip);
            Ready = true;
        }
        #endregion

        public override GraphicsSignature GraphicsSignature => rootSignature;
        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public PresentData lightingCameraPresentData = new PresentData();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBuffer> materialBuffers = new List<ConstantBuffer>();
        byte[] rcDataUploadBuffer = new byte[c_entityDataSize + c_lightingDataSize + c_materialDataSize + c_presentDataSize];
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
            lightingCameraPresentData.PlayTime = (float)deltaTime;
        }

        int mainLightIndex;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var deviceResources = context.deviceResources;
            var cameras = context.cameras;
            var graphicsContext = context.graphicsContext;
            var settings = context.settings;
            var Entities = context.entities;
            int countMaterials = 0;
            if (settings.RenderStyle == 1)
                currentUsedPObject = PObjectMMD_Toon1;
            else if (settings.Quality == 0)
                currentUsedPObject = PObjectMMD;
            else
                currentUsedPObject = PObjectMMD_DisneyBrdf;

            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireEntityBuffers(deviceResources, context.entities.Count);
            DesireMaterialBuffers(deviceResources, countMaterials);

            var lightings = context.lightings;
            mainLightIndex = -1;
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
            for (int i = 0; i < lightings.Count; i++)
            {
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    lightingCameraPresentData.UpdateCameraData(lightings[i], ref context.settings); ;
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
                pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetEntityData);
                Marshal.StructureToPtr(Matrix4x4.Transpose(entity.boneComponent.LocalToWorld), pBufferData, true);
                Marshal.StructureToPtr(entity.rendererComponent.amountAB, pBufferData + 64, true);

                graphicsContext.UpdateResource(entityDataBuffers[i], rcDataUploadBuffer, c_entityDataSize + c_lightingDataSize, c_offsetEntityData);
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
                cameraPresentDatas[i].UpdateCameraData(cameras[i], ref context.settings);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                Marshal.WriteInt32(pBufferData + 256, settings.EnableAO ? 1 : 0);
                Marshal.WriteInt32(pBufferData + 256 + 4, settings.EnableShadow ? 1 : 0);
                Marshal.WriteInt32(pBufferData + 256 + 8, (int)settings.Quality);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
            }
        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            var DSV0 = context.DSV0;

            graphicsContext.SetRootSignature(rootSignature);
            //graphicsContext.SetSRVT((RenderTexture2D)null, 6);//may used in d3d11
            graphicsContext.SetAndClearDSV(DSV0);
            IList<MMD3DEntity> Entities = context.entities;
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(graphicsContext, Entities[i], cameraPresentDatas[0], entityDataBuffers[i]);
            if (mainLightIndex != -1)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityShadow(context, Entities[i], lightingCameraPresentData, entityDataBuffers[i]);
            }
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            var graphicsContext = context.graphicsContext;
            //var scene = context.scene;

            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.outputDSV, Vector4.Zero);
            graphicsContext.SetCBVR(cameraPresentDatas[cameraIndex].DataBuffer, 2);
            graphicsContext.SetSRVT(context.DSV0, 6);
            graphicsContext.SetSRVT(context.EnvCubeMap, 7);
            graphicsContext.SetSRVT(context.IrradianceMap, 8);
            int matIndex = 0;
            for (int i = 0; i < context.entities.Count; i++)
            {
                MMD3DEntity entity = context.entities[i];
                RenderEntity(context, entity, cameraPresentDatas[cameraIndex], entityDataBuffers[i], ref matIndex);
            }
            graphicsContext.SetPObject(PObjectSkyBox, CullMode.back, BlendState.none);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexed(context.ndcQuadMesh.m_indexCount, 0, 0);
        }

        private void DesireEntityBuffers(DeviceResources deviceResources, int count)
        {
            while (entityDataBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_entityDataSize + c_lightingDataSize);
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

        private void EntitySkinning(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            graphicsContext.SetCBVR(entity.boneComponent.boneMatricesBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData.DataBuffer, 2);
            graphicsContext.SetMesh(entity.rendererComponent.mesh);
            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }

            if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                graphicsContext.SetPObjectStreamOut(currentUsedPObject);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectStreamOut(entity.rendererComponent.pObject);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loading)
                graphicsContext.SetPObjectStreamOut(PObjectMMDLoading);
            else
                graphicsContext.SetPObjectStreamOut(PObjectMMDError);

            graphicsContext.SetSOMesh(entity.rendererComponent.mesh);
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
            graphicsContext.SetSOMesh(null);
        }
        private void RenderEntityShadow(RenderPipelineContext context, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entity.boneComponent.boneMatricesBuffer, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData.DataBuffer, 2);

            int indexStartLocation = 0;
            MMDRendererComponent rendererComponent = entity.rendererComponent;
            List<Texture2D> texs = rendererComponent.texs;
            graphicsContext.SetPObjectDepthOnly(PObjectMMDDepth);
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.CastSelfShadow))
                {
                    if (texs != null)
                    {
                        Texture2D tex1 = null;
                        if (Materials[i].texIndex != -1)
                            tex1 = texs[Materials[i].texIndex];
                        if (tex1 != null)
                        {
                            if (tex1.Status == GraphicsObjectStatus.loaded)
                                graphicsContext.SetSRVT(tex1, 4);
                            else if (tex1.Status == GraphicsObjectStatus.loading)
                                graphicsContext.SetSRVT(textureLoading, 4);
                            else
                                graphicsContext.SetSRVT(textureError, 4);
                        }
                        else
                            graphicsContext.SetSRVT(textureError, 4);
                    }
                    graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                }
                indexStartLocation += Materials[i].indexCount;
            }
        }

        private void RenderEntity(RenderPipelineContext context, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            MMDRendererComponent rendererComponent = entity.rendererComponent;
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
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
                if (texs != null)
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                    {
                        tex1 = texs[Materials[i].texIndex];
                        if (tex1.Status == GraphicsObjectStatus.loaded) { }
                        else if (tex1.Status == GraphicsObjectStatus.loading)
                            tex1 = textureLoading;
                        else
                            tex1 = textureError;
                    }
                    else
                        tex1 = textureError;
                    Texture2D tex2 = null;
                    if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    {
                        tex2 = texs[Materials[i].toonIndex];
                        if (tex2 != null)
                        {
                            if (tex2.Status == GraphicsObjectStatus.loaded) { }
                            else if (tex2.Status == GraphicsObjectStatus.loading)
                                tex2 = textureLoading;
                            else
                                tex2 = textureError;
                        }
                        else
                            tex2 = textureError;
                    }
                    else
                        tex2 = textureError;
                    graphicsContext.SetSRVT(tex1, 4);
                    graphicsContext.SetSRVT(tex2, 5);
                }
                else
                {
                    graphicsContext.SetSRVT(textureError, 4);
                    graphicsContext.SetSRVT(textureError, 5);
                }
                graphicsContext.SetCBVR(entity.boneComponent.boneMatricesBuffer, 0);
                graphicsContext.SetCBVR(entityDataBuffer, 1);
                graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                matIndex++;
                CullMode cullMode = CullMode.back;
                BlendState blendState = BlendState.alpha;
                if (Materials[i].DrawFlags.HasFlag(NMMDE_DrawFlag.DrawDoubleFace))
                    cullMode = CullMode.none;
                //if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawSelfShadow))
                //    blendState = BlendState.none;

                if (rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                    graphicsContext.SetPObject(currentUsedPObject, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                    graphicsContext.SetPObject(rendererComponent.pObject, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.loading)
                    graphicsContext.SetPObject(PObjectMMDLoading, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.error)
                    graphicsContext.SetPObject(PObjectMMDError, cullMode, blendState);
                graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
                indexStartLocation += Materials[i].indexCount;
            }
        }
    }
}
