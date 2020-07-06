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
        public void Reload(DeviceResources deviceResources)
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Reload(deviceResources);
            }
            lightingCameraPresentData.Reload(deviceResources);
            rootSignature.ReloadMMD(deviceResources);
        }
        #region graphics assets
        public async Task ReloadAssets(DeviceResources deviceResources)
        {
            await ReloadVertexShader(VSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMD.cso");
            await ReloadPixelShader(PSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMDLoading, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");

            PObjectMMD.Reload(deviceResources, rootSignature, PObjectType.mmd, VSMMD, null, PSMMD);
            PObjectMMDLoading.Reload(deviceResources, rootSignature, PObjectType.mmd, VSMMD, null, PSMMDLoading);
            PObjectMMDError.Reload(deviceResources, rootSignature, PObjectType.mmd, VSMMD, null, PSMMDError);
            Ready = true;
        }
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public VertexShader VSMMD = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PObject PObjectMMD = new PObject();
        public PObject PObjectMMDLoading = new PObject();
        public PObject PObjectMMDError = new PObject();
        #endregion

        public override GraphicsSignature GraphicsSignature => rootSignature;

        public volatile bool Ready;
        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public PresentData lightingCameraPresentData = new PresentData();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        public List<ConstantBuffer> materialBuffers = new List<ConstantBuffer>();
        Settings settings;
        byte[] rcDataUploadBuffer = new byte[c_transformMatrixDataSize + c_lightingDataSize + c_materialDataSize];
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

        public void TimeChange(float time, float deltaTime)
        {
            for (int i = 0; i < cameraPresentDatas.Length; i++)
            {
                cameraPresentDatas[i].PlayTime = time;
                cameraPresentDatas[i].DeltaTime = deltaTime;
            }
            lightingCameraPresentData.PlayTime = time;
            lightingCameraPresentData.PlayTime = deltaTime;
        }

        int lightingIndex1;


        public void PrepareRenderData(RenderPipelineContext context)
        {
            this.settings = context.settings;
            var deviceResources = context.deviceResources;
            var cameras = context.cameras;
            var scene = context.scene;
            var graphicsContext = context.graphicsContext;

            DesireEntityBuffers(deviceResources, scene.Entities.Count);
            int countMaterials = 0;
            var Entities = scene.Entities;
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            DesireMaterialBuffers(deviceResources, countMaterials);

            #region Update Entities Data
            for (int i = 0; i < Entities.Count; i++)
            {
                MMD3DEntity entity = Entities[i];
                IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetLightingData);
                for (int j = 0; j < c_lightingDataSize; j += 4)
                {
                    Marshal.WriteInt32(pBufferData + j, 0);
                }
                int mainLightIndex = -1;
                int lightCount = 0;
                var lightings = scene.Lightings;
                for (int j = 0; j < lightings.Count; j++)
                {
                    if (lightings[j].LightingType == LightingType.Directional)
                    {
                        Marshal.StructureToPtr(Vector3.Transform(-Vector3.UnitZ, lightings[j].rotateMatrix), pBufferData, true);
                        Marshal.StructureToPtr((uint)lightings[j].LightingType, pBufferData + 12, true);
                        Marshal.StructureToPtr(lightings[j].Color, pBufferData + 16, true);
                        Marshal.StructureToPtr(Matrix4x4.Transpose(lightings[j].vpMatrix), pBufferData + 32, true);
                        mainLightIndex = j;
                        lightCount++;
                        pBufferData += 96;
                        break;
                    }
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
                    IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(rcDataUploadBuffer, c_offsetMaterialData);
                    Marshal.StructureToPtr(Materials[j].innerStruct, ptr, true);
                    graphicsContext.UpdateResource(materialBuffers[matIndex], rcDataUploadBuffer, MMDMatLit.c_materialDataSize, c_offsetMaterialData);
                    matIndex++;
                }
            }
            #endregion

            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].UpdateBuffer(graphicsContext);
            }
            IList<Lighting> Lightings = scene.Lightings;
            lightingIndex1 = -1;
            if (Lightings.Count > 0)
            {
                for (int i = 0; i < Lightings.Count; i++)
                {
                    if (Lightings[i].LightingType == LightingType.Directional)
                    {
                        lightingCameraPresentData.UpdateCameraData(Lightings[i]);
                        lightingCameraPresentData.UpdateBuffer(graphicsContext);
                        lightingIndex1 = i;
                        break;
                    }
                }
            }
        }

        public void BeforeRenderCameras(RenderPipelineContext context)
        {
            var graphicsContext = context.graphicsContext;
            var DSV0 = context.DSV0;

            graphicsContext.SetRootSignature(rootSignature);
            graphicsContext.SetSRV(PObjectType.mmd, null, 2);
            graphicsContext.SetAndClearDSV(DSV0);
            IList<MMD3DEntity> Entities = context.scene.Entities;
            if (lightingIndex1 != -1)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityDepth(graphicsContext, Entities[i], lightingCameraPresentData, entityDataBuffers[i]);
            }
        }

        public void RenderCamera(RenderPipelineContext context, int cameraIndex)
        {
            var graphicsContext = context.graphicsContext;
            var scene = context.scene;

            graphicsContext.SetRootSignature(rootSignature);
            //graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
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
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, MMDMatLit.c_materialDataSize);
                materialBuffers.Add(constantBuffer);
            }
        }

        private void RenderEntityDepth(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, entityDataBuffer, cameraPresentData.DataBuffer, null);
            graphicsContext.SetMesh(entity.rendererComponent.mesh);
            if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                graphicsContext.SetPObjectDepthOnly(PObjectMMD);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                graphicsContext.SetPObjectDepthOnly(entity.rendererComponent.pObject);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.loading)
                graphicsContext.SetPObjectDepthOnly(PObjectMMDLoading);
            else if (entity.rendererComponent.pObject.Status == GraphicsObjectStatus.error)
                graphicsContext.SetPObjectDepthOnly(PObjectMMDError);

            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
        }

        private void RenderEntity(RenderPipelineContext context, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer, ref int matIndex)
        {
            Texture2D textureLoading = context.TextureLoading;
            Texture2D textureError = context.TextureError;
            var graphicsContext = context.graphicsContext;
            MMDRendererComponent rendererComponent = entity.rendererComponent;
            graphicsContext.SetMesh(rendererComponent.mesh);
            var Materials = rendererComponent.Materials;
            int indexStartLocation = 0;
            List<Texture2D> texs = rendererComponent.texs;
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


                    if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    {
                        Texture2D tex2 = texs[Materials[i].toonIndex];
                        if (tex2 != null)
                        {
                            if (tex2.Status == GraphicsObjectStatus.loaded)
                                graphicsContext.SetSRV(PObjectType.mmd, tex2, 1);
                            else if (tex2.Status == GraphicsObjectStatus.loading)
                                graphicsContext.SetSRV(PObjectType.mmd, textureLoading, 1);
                            else
                                graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                        }
                        else
                            graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                    }
                    else
                        graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                }
                else
                {
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                }
                graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, entityDataBuffer, cameraPresentData.DataBuffer, materialBuffers[matIndex]);
                matIndex++;
                CullMode cullMode = CullMode.back;
                BlendState blendState = BlendState.alpha;
                if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawDoubleFace))
                    cullMode = CullMode.none;
                //if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawSelfShadow))
                //    blendState = BlendState.none;

                if (rendererComponent.pObject.Status == GraphicsObjectStatus.unload)
                    graphicsContext.SetPObject(PObjectMMD, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.loaded)
                    graphicsContext.SetPObject(rendererComponent.pObject, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.loading)
                    graphicsContext.SetPObject(PObjectMMDLoading, cullMode, blendState);
                else if (rendererComponent.pObject.Status == GraphicsObjectStatus.error)
                    graphicsContext.SetPObject(PObjectMMDError, cullMode, blendState);
                if (Materials[i].DiffuseColor.W > 0)
                    graphicsContext.DrawIndexed(Materials[i].indexCount, indexStartLocation, 0);
                indexStartLocation += Materials[i].indexCount;
            }
        }
    }
}
