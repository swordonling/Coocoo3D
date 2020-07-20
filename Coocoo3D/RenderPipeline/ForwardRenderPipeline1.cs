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
        public const int c_transformMatrixDataSize = 64;
        public const int c_offsetTransformMatrixData = 0;
        public const int c_lightingDataSize = 384;
        public const int c_offsetLightingData = c_offsetTransformMatrixData + c_transformMatrixDataSize;
        public const int c_materialDataSize = 256;
        public const int c_offsetMaterialData = c_offsetLightingData + c_lightingDataSize;
        public const int c_presentDataSize = 256;
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
        public override async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMDSkinning2, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");
            await ReloadPixelShader(PSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMDLoading, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");
            await ReloadPixelShader(PSMMDAlphaClip, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip.cso");
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
            PObjectMMD2.Reload2(deviceResources, rootSignature, VSMMDSkinning2, null, PSMMD, VSMMDTransform, PSMMDAlphaClip, format);
            PObjectMMDLoading.Reload2(deviceResources, rootSignature, VSMMDSkinning2, null, PSMMDLoading, VSMMDTransform, PSMMDAlphaClip, format);
            PObjectMMDError.Reload2(deviceResources, rootSignature, VSMMDSkinning2, null, PSMMDError, VSMMDTransform, PSMMDAlphaClip, format);
            Ready = true;
        }
        public DxgiFormat CurrentRenderTargetFormat;
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader PSMMDAlphaClip = new PixelShader();
        public PObject PObjectMMD2 = new PObject();
        public PObject PObjectMMDLoading = new PObject();
        public PObject PObjectMMDError = new PObject();
        #endregion

        public override GraphicsSignature GraphicsSignature => rootSignature;
        Random randomGenerator = new Random();

        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public PresentData lightingCameraPresentData = new PresentData();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBufferStatic> materialBuffers = new List<ConstantBufferStatic>();
        byte[] rcDataUploadBuffer = new byte[c_transformMatrixDataSize + c_lightingDataSize + c_materialDataSize + c_presentDataSize];
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

        public override void TimeChange(float time, float deltaTime)
        {
            for (int i = 0; i < cameraPresentDatas.Length; i++)
            {
                cameraPresentDatas[i].PlayTime = time;
                cameraPresentDatas[i].DeltaTime = deltaTime;
            }
            lightingCameraPresentData.PlayTime = time;
            lightingCameraPresentData.PlayTime = deltaTime;
        }

        int mainLightIndex;
        public override void PrepareRenderData(RenderPipelineContext context)
        {
            var deviceResources = context.deviceResources;
            var cameras = context.cameras;
            var scene = context.scene;
            var graphicsContext = context.graphicsContext;

            var Entities = scene.Entities;
            int countMaterials = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireEntityBuffers(deviceResources, scene.Entities.Count);
            DesireMaterialBuffers(deviceResources, countMaterials);

            var lightings = scene.Lightings;
            mainLightIndex = -1;
            for (int i = 0; i < lightings.Count; i++)
            {
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    lightingCameraPresentData.UpdateCameraData(lightings[i]);
                    IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
                    Marshal.StructureToPtr(lightingCameraPresentData.innerStruct, pBufferData, true);
                    graphicsContext.UpdateResource(lightingCameraPresentData.DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
                    mainLightIndex = i;
                    break;
                }
            }
            #region Update Entities Data
            for (int i = 0; i < Entities.Count; i++)
            {
                MMD3DEntity entity = Entities[i];
                #region Lighting
                Array.Clear(rcDataUploadBuffer, c_offsetLightingData, c_lightingDataSize);
                int lightCount = 0;
                IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);
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
                pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetTransformMatrixData);
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(entity.Rotation) * Matrix4x4.CreateTranslation(entity.Position);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), pBufferData, true);

                graphicsContext.UpdateResource(entityDataBuffers[i], rcDataUploadBuffer, c_transformMatrixDataSize + c_lightingDataSize, c_offsetTransformMatrixData);
            }
            #endregion
            #region Update material data
            int matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetMaterialData);
                    Marshal.StructureToPtr(Materials[j].innerStruct, pBufferData, true);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, MMDMatLit.c_materialDataSize, c_offsetMaterialData);
                    matIndex++;
                }
            }
            #endregion

            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].innerStruct.RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].innerStruct.RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetPresentData);
                Marshal.StructureToPtr(cameraPresentDatas[i].innerStruct, pBufferData, true);
                graphicsContext.UpdateResource(cameraPresentDatas[i].DataBuffer, rcDataUploadBuffer, c_presentDataSize, c_offsetPresentData);
            }
        }

        public override void BeforeRenderCameras(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            var DSV0 = context.DSV0;

            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetSRV(PObjectType.mmd, null, 2);
            graphicsContext.SetAndClearDSV(DSV0);
            IList<MMD3DEntity> Entities = context.scene.Entities;
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(graphicsContext, Entities[i], cameraPresentDatas[0], entityDataBuffers[i]);
            if (mainLightIndex != -1)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityDepth(context, Entities[i], lightingCameraPresentData, entityDataBuffers[i]);
            }
        }

        public override void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            var graphicsContext = context.graphicsContext;
            var scene = context.scene;

            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetAndClearRTVDSV(context.outputRTV, context.outputDSV, Vector4.Zero);
            graphicsContext.SetSRV_RT(PObjectType.mmd, context.DSV0, 2);
            int matIndex = 0;
            for (int i = 0; i < scene.Entities.Count; i++)
            {
                MMD3DEntity entity = scene.Entities[i];
                RenderEntity(context, entity, cameraPresentDatas[cameraIndex], entityDataBuffers[i], ref matIndex);
            }
        }

        private void DesireEntityBuffers(DeviceResources deviceResources, int count)
        {
            while (entityDataBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_transformMatrixDataSize + c_lightingDataSize);
                entityDataBuffers.Add(constantBuffer);
            }
        }

        private void DesireMaterialBuffers(DeviceResources deviceResources, int count)
        {
            while (materialBuffers.Count < count)
            {
                ConstantBufferStatic constantBuffer = new ConstantBufferStatic();
                constantBuffer.Reload(deviceResources, MMDMatLit.c_materialDataSize);
                materialBuffers.Add(constantBuffer);
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

            if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                graphicsContext.SetPObjectStreamOut(PObjectMMD2);
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
        private void RenderEntityDepth(RenderPipelineContext context, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            graphicsContext.SetCBVR(entity.boneComponent.boneMatrices, 0);
            graphicsContext.SetCBVR(entityDataBuffer, 1);
            graphicsContext.SetCBVR(cameraPresentData.DataBuffer, 2);

            int indexStartLocation = 0;
            MMDRendererComponent rendererComponent = entity.rendererComponent;
            List<Texture2D> texs = rendererComponent.texs;
            if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                graphicsContext.SetPObjectDepthOnly(PObjectMMD2);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectDepthOnly(entity.rendererComponent.pObject);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loading)
                graphicsContext.SetPObjectDepthOnly(PObjectMMDLoading);
            else
                graphicsContext.SetPObjectDepthOnly(PObjectMMDError);
            graphicsContext.SetMeshSkinned(rendererComponent.mesh);
            for (int i = 0; i < Materials.Count; i++)
            {
                if (texs != null)
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = texs[Materials[i].texIndex];
                    if (tex1 != null)
                    {
                        if (tex1.Status == GraphicsObjectStatus.loaded)
                            graphicsContext.SetSRV(PObjectType.mmd, tex1, 0);
                        else if (tex1.Status == GraphicsObjectStatus.loading)
                            graphicsContext.SetSRV(PObjectType.mmd, textureLoading, 0);
                        else
                            graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                    }
                    else
                        graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                }
                graphicsContext.Draw(Materials[i].indexCount, indexStartLocation);
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
                    graphicsContext.SetSRV(PObjectType.mmd, tex1, 0);
                    graphicsContext.SetSRV(PObjectType.mmd, tex2, 1);
                }
                else
                {
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                }
                graphicsContext.SetCBVR(entity.boneComponent.boneMatrices, 0);
                graphicsContext.SetCBVR(entityDataBuffer, 1);
                graphicsContext.SetCBVR(cameraPresentData.DataBuffer, 2);
                graphicsContext.SetCBVR(materialBuffers[matIndex], 3);
                matIndex++;
                CullMode cullMode = CullMode.back;
                BlendState blendState = BlendState.alpha;
                if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawDoubleFace))
                    cullMode = CullMode.none;
                //if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawSelfShadow))
                //    blendState = BlendState.none;

                if (rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                    graphicsContext.SetPObject(PObjectMMD2, cullMode, blendState);
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
