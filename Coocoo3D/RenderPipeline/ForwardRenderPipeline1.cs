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
        public void Reload(DeviceResources deviceResources, DefaultResources defaultResources)
        {
            textureError = defaultResources.TextureError;
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i].Reload(deviceResources);
            }
            lightingCameraPresentData.Reload(deviceResources);
        }
        public bool Ready;
        Texture2D textureError;
        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];
        public PresentData lightingCameraPresentData = new PresentData();
        public List<ConstantBuffer> entityDataBuffers = new List<ConstantBuffer>();
        Settings settings;
        byte[] rcDataUploadBuffer = new byte[c_transformMatrixDataSize + c_lightingDataSize + MMDMatLit.c_materialDataSize];
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
        public void PrepareRenderData(DeviceResources deviceResources, GraphicsContext graphicsContext, DefaultResources defaultResources, Settings settings, Scene scene, IReadOnlyList<Camera> cameras)
        {
            this.settings = settings;
            DesireBuffers(deviceResources, scene.Entities.Count);

            #region Update Entities Data
            for (int i = 0; i < scene.Entities.Count; i++)
            {
                MMD3DEntity entity = scene.Entities[i];
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

        public void RenderBeforeCamera(GraphicsContext graphicsContext, DefaultResources defaultResources, Scene scene)
        {
            graphicsContext.SetRootSignature(defaultResources.signatureMMD);
            graphicsContext.SetSRV(PObjectType.mmd, null, 2);
            graphicsContext.SetAndClearDSV(defaultResources.DepthStencil0);
            IList<MMD3DEntity> Entities = scene.Entities;
            if (lightingIndex1 != -1)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityDepth(graphicsContext, Entities[i], lightingCameraPresentData, entityDataBuffers[i]);
            }
            graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
        }

        public void RenderCamera(GraphicsContext graphicsContext, DefaultResources defaultResources, Scene scene, int cameraIndex)
        {
            graphicsContext.SetRootSignature(defaultResources.signatureMMD);
            graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
            graphicsContext.SetSRV_RT(PObjectType.mmd, defaultResources.DepthStencil0, 2);
            for (int i = 0; i < scene.Entities.Count; i++)
            {
                MMD3DEntity entity = scene.Entities[i];
                RenderEntity(graphicsContext, entity, cameraPresentDatas[cameraIndex], entityDataBuffers[i]);
            }
        }

        public void AfterRender(GraphicsContext graphicsContext, DefaultResources defaultResources, Scene scene)
        {
        }

        private void DesireBuffers(DeviceResources deviceResources, int count)
        {
            while (entityDataBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_transformMatrixDataSize + c_lightingDataSize);
                entityDataBuffers.Add(constantBuffer);
            }
        }

        private void RenderEntityDepth(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            var Materials = entity.rendererComponent.Materials;
            graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, entityDataBuffer, cameraPresentData.DataBuffer, null);
            graphicsContext.SetMesh(entity.rendererComponent.mesh);
            graphicsContext.SetPObjectDepthOnly(entity.rendererComponent.pObject);

            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
        }

        private void RenderEntity(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData, ConstantBuffer entityDataBuffer)
        {
            MMDRendererComponent rendererComponent = entity.rendererComponent;
            graphicsContext.SetMesh(rendererComponent.mesh);
            var Materials = rendererComponent.Materials;
            int indexStartLocation = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (rendererComponent.texs != null)
                {
                    Texture2D tex1 = null;
                    if (Materials[i].texIndex != -1)
                        tex1 = rendererComponent.texs[Materials[i].texIndex];
                    if (tex1 != null)
                        graphicsContext.SetSRV(PObjectType.mmd, tex1, 0);
                    else
                        graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                    if (Materials[i].toonIndex > -1 && Materials[i].toonIndex < Materials.Count)
                    {
                        Texture2D tex2 = rendererComponent.texs[Materials[i].toonIndex];
                        if (tex2 != null)
                            graphicsContext.SetSRV(PObjectType.mmd, tex2, 1);
                        else
                            graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                    }
                    else
                        graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                }
                else
                {
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 1);
                    graphicsContext.SetSRV(PObjectType.mmd, textureError, 0);
                }
                graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, entityDataBuffer, cameraPresentData.DataBuffer, Materials[i].matBuf);
                CullMode cullMode = CullMode.back;
                BlendState blendState = BlendState.alpha;
                if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawDoubleFace))
                    cullMode = CullMode.none;
                if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawSelfShadow))
                    blendState = BlendState.none;

                graphicsContext.SetPObject(rendererComponent.pObject, cullMode, blendState);

                graphicsContext.DrawIndexed(Materials[i].indexCount, indexStartLocation, 0);
                indexStartLocation += Materials[i].indexCount;
            }
        }
    }
}
