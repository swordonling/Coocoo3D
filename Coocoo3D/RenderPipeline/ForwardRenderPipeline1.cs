using Coocoo3D.Components;
using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class ForwardRenderPipeline1 : RenderPipeline
    {
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
        Settings settings;

        public ForwardRenderPipeline1()
        {
            for (int i = 0; i < c_maxCameraPerRender; i++)
            {
                cameraPresentDatas[i] = new PresentData();
            }
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

        public void PrepareRenderData(GraphicsContext graphicsContext, DefaultResources defaultResources, Settings settings, Scene scene, IReadOnlyList<Camera> cameras)
        {
            this.settings = settings;

            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].UpdateBuffer(graphicsContext);
            }
            IList<Lighting> Lightings = scene.Lightings;
            if (Lightings.Count > 0)
            {
                lightingCameraPresentData.UpdateCameraData(Lightings[0]);
                lightingCameraPresentData.UpdateBuffer(graphicsContext);
            }
        }

        public void RenderBeforeCamera(GraphicsContext graphicsContext, DefaultResources defaultResources, Scene scene)
        {
            graphicsContext.SetRootSignature(defaultResources.signatureMMD);
            graphicsContext.SetSRV(PObjectType.mmd, null, 2);
            graphicsContext.SetAndClearDSV(defaultResources.DepthStencil0);
            IList<MMD3DEntity> Entities = scene.Entities;
            if (scene.Lightings.Count > 0)
            {
                for (int i = 0; i < Entities.Count; i++)
                    RenderEntityDepth(graphicsContext, Entities[i], lightingCameraPresentData);
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
                RenderEntity(graphicsContext, entity, cameraPresentDatas[cameraIndex]);
            }
        }

        public void AfterRender(GraphicsContext graphicsContext, DefaultResources defaultResources, Scene scene)
        {
        }


        private void RenderEntityDepth(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData)
        {
            var Materials = entity.rendererComponent.Materials;
            graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, entity.rendererComponent.EntityDataBuffer, cameraPresentData.DataBuffer, null);
            graphicsContext.SetMesh(entity.rendererComponent.mesh);
            graphicsContext.SetPObjectDepthOnly(entity.rendererComponent.pObject);

            int indexCountAll = 0;
            for (int i = 0; i < Materials.Count; i++)
            {
                indexCountAll += Materials[i].indexCount;
            }
            graphicsContext.DrawIndexed(indexCountAll, 0, 0);
        }

        private void RenderEntity(GraphicsContext graphicsContext, MMD3DEntity entity, PresentData cameraPresentData)
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
                graphicsContext.SetMMDRender1CBResources(entity.boneComponent.boneMatrices, rendererComponent.EntityDataBuffer, cameraPresentData.DataBuffer, Materials[i].matBuf);
                if (Materials[i].DrawFlags.HasFlag(MMDSupport.DrawFlags.DrawDoubleFace))
                    graphicsContext.SetPObject(rendererComponent.pObject, CullMode.none, BlendState.alpha);
                else
                    graphicsContext.SetPObject(rendererComponent.pObject, CullMode.back, BlendState.alpha);
                graphicsContext.DrawIndexed(Materials[i].indexCount, indexStartLocation, 0);
                indexStartLocation += Materials[i].indexCount;
            }
        }
    }
}
