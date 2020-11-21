﻿using Coocoo3D.Components;
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
    public class DeferredRenderPipeline1 : RenderPipeline
    {
        const int c_materialDataSize = 256;
        const int c_presentDataSize = 512;
        const int c_lightingDataSize = 512;
        const int c_lightCameraCount = 2;
        #region forward
        public const int c_offsetMaterialData = 0;
        public const int c_offsetLightingData = c_offsetMaterialData + c_materialDataSize;
        public const int c_offsetPresentData = c_offsetLightingData + c_lightingDataSize;
        #endregion
        public PresentData[] cameraPresentDatas = new PresentData[c_maxCameraPerRender];

        Random randomGenerator = new Random();

        public void Reload(DeviceResources deviceResources)
        {
            Ready = true;
        }

        public List<ConstantBuffer> lightingBuffers = new List<ConstantBuffer>();

        private void DesireLightingBuffers(DeviceResources deviceResources, int count)
        {
            while (lightingBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_lightingDataSize);
                lightingBuffers.Add(constantBuffer);
            }
        }

        struct _Counters
        {
            public int material;
            public int vertex;
        }

        struct _Struct1
        {
            public Matrix4x4 x1;
            public Matrix4x4 x2;
            public Vector3 positionOrDirection;
            public uint lightType;
            public Vector4 color;
            public float Range;
        }

        bool HasMainLight;
        Matrix4x4 lightCameraMatrix = Matrix4x4.Identity;
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
            for (int i = 0; i < Entities.Count; i++)
            {
                countMaterials += Entities[i].rendererComponent.Materials.Count;
            }
            context.DesireMaterialBuffers(countMaterials);
            DesireLightingBuffers(context.deviceResources, lightings.Count);
            var camera = context.dynamicContext.cameras[0];
            IntPtr pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(context.bigBuffer, 0);
            #region forward lighting
            int lightCount = 0;
            Matrix4x4 lightCameraMatrix0 = Matrix4x4.Identity;
            Matrix4x4 lightCameraMatrix1 = Matrix4x4.Identity;
            HasMainLight = false;
            Array.Clear(context.bigBuffer, c_offsetLightingData, c_lightingDataSize);
            var LightCameraDataBuffers = context.LightCameraDataBuffers;
            if (lightings.Count > 0 && lightings[0].LightingType == LightingType.Directional)
            {
                lightCameraMatrix0 = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(2, camera.LookAtPoint - camera.Pos, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix0, pBufferData + c_offsetPresentData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffers[0], context.bigBuffer, c_presentDataSize, c_offsetPresentData);

                lightCameraMatrix1 = Matrix4x4.Transpose(lightings[0].GetLightingMatrix(settings.ExtendShadowMapRange, camera.LookAtPoint - camera.Pos, camera.Angle, camera.Distance));
                Marshal.StructureToPtr(lightCameraMatrix1, pBufferData + c_offsetPresentData, true);
                graphicsContext.UpdateResource(LightCameraDataBuffers[1], context.bigBuffer, c_presentDataSize, c_offsetPresentData);
                HasMainLight = true;
            }

            IntPtr p0 = Marshal.UnsafeAddrOfPinnedArrayElement(context.bigBuffer, c_offsetLightingData);
            Array.Clear(context.bigBuffer, c_offsetLightingData, c_lightingDataSize);
            pBufferData = p0 + 256;
            Marshal.StructureToPtr(lightCameraMatrix0, p0, true);
            Marshal.StructureToPtr(lightCameraMatrix1, p0 + 64, true);
            for (int i = 0; i < lightings.Count; i++)
            {
                LightingData data1 = lightings[i];
                Marshal.StructureToPtr(data1.GetPositionOrDirection(camera.Pos), pBufferData, true);
                Marshal.StructureToPtr((uint)data1.LightingType, pBufferData + 12, true);
                Marshal.StructureToPtr(data1.Color, pBufferData + 16, true);

                lightCount++;
                pBufferData += 32;
                if (lightCount >= 8)
                    break;
            }
            #endregion

            pBufferData = Marshal.UnsafeAddrOfPinnedArrayElement(context.bigBuffer, 0);
            #region Update material data
            int matIndex = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var Materials = Entities[i].rendererComponent.Materials;
                for (int j = 0; j < Materials.Count; j++)
                {
                    Marshal.StructureToPtr(Materials[j].innerStruct, pBufferData, true);
                    graphicsContext.UpdateResource(context.MaterialBuffers[matIndex], context.bigBuffer, c_materialDataSize + c_lightingDataSize, 0);
                    matIndex++;
                }
            }
            #endregion

            for (int i = 0; i < cameras.Count; i++)
            {
                cameraPresentDatas[i].PlayTime = (float)context.dynamicContext.Time;
                cameraPresentDatas[i].DeltaTime = (float)context.dynamicContext.DeltaTime;

                cameraPresentDatas[i].UpdateCameraData(cameras[i]);
                cameraPresentDatas[i].RandomValue1 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].RandomValue2 = randomGenerator.Next(int.MinValue, int.MaxValue);
                cameraPresentDatas[i].inShaderSettings = inShaderSettings;
                Marshal.StructureToPtr(cameraPresentDatas[i], pBufferData, true);
                graphicsContext.UpdateResource(context.CameraDataBuffers[i], context.bigBuffer, c_presentDataSize, 0);
            }
            for (int i = 0; i < lightings.Count; i++)
            {
                _Struct1 _struct1 = new _Struct1()
                {
                    positionOrDirection = lightings[i].GetPositionOrDirection(camera.Pos),
                    lightType = (uint)lightings[i].LightingType,
                    color = lightings[i].Color,
                    Range = lightings[i].Range
                };
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    _struct1.x1 = Matrix4x4.Transpose(lightings[i].GetLightingMatrix(32, camera.LookAtPoint - camera.Pos, camera.Angle, camera.Distance));
                }
                Marshal.StructureToPtr(_struct1, pBufferData, true);
                graphicsContext.UpdateResource(lightingBuffers[i], context.bigBuffer, c_lightingDataSize, 0);
            }
        }

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
            PObject mmdSkinning = context.RPAssetsManager.PObjectMMDSkinning;
            void EntitySkinning(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer)
            {
                var Materials = rendererComponent.Materials;
                graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                graphicsContext.SetCBVR(cameraPresentData, 2);
                var POSkinning = PObjectStatusSelect(rendererComponent.POSkinning, mmdSkinning, mmdSkinning, mmdSkinning);

                graphicsContext.SetPObjectStreamOut(POSkinning);
                graphicsContext.SetMeshVertex1(rendererComponent.mesh);
                graphicsContext.SetMeshVertex(rendererComponent.meshAppend);
                int indexCountAll = rendererComponent.meshVertexCount;
                graphicsContext.Draw(indexCountAll, 0);
            }
            for (int i = 0; i < Entities.Count; i++)
                EntitySkinning(Entities[i].rendererComponent, context.CameraDataBuffers[0], context.CBs_Bone[i]);
            graphicsContext.SetSOMeshNone();


            int cameraIndex = 0;

            graphicsContext.SetRootSignature(context.RPAssetsManager.rootSignature);
            graphicsContext.SetRTVDSV(context.ScreenSizeRenderTextures, context.ScreenSizeDSVs[0], Vector4.Zero, true, true);
            graphicsContext.SetMesh(context.SkinningMeshBuffer);

            void _RenderEntity(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ref _Counters counter)
            {
                //var PODraw = PObjectStatusSelect(rendererComponent.PODraw, context.RPAssetsManager.PObjectMMDLoading, currentDrawPObject, context.RPAssetsManager.PObjectMMDError);
                var PODraw = context.RPAssetsManager.PObjectDeferredRenderGBuffer; ;
                var Materials = rendererComponent.Materials;
                List<Texture2D> texs = rendererComponent.textures;
                graphicsContext.SetMeshIndex(rendererComponent.mesh);
                graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                graphicsContext.SetCBVR(cameraPresentData, 2);
                //CooGExtension.SetCBVBuffer3(graphicsContext, entityBoneDataBuffer, entityDataBuffer, cameraPresentData, 0);
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
                    graphicsContext.SetCBVR(context.MaterialBuffers[counter.material], 1);
                    //graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 3);
                    //graphicsContext.SetSRVT(TextureStatusSelect(tex2, textureLoading, textureError, textureError), 4);
                    CooGExtension.SetSRVTexture2(graphicsContext, tex1, tex2, 3, textureLoading, textureError);
                    CullMode cullMode = CullMode.back;
                    if (Materials[i].DrawFlags.HasFlag(DrawFlag.DrawDoubleFace))
                        cullMode = CullMode.none;
                    graphicsContext.SetPObject(PODraw, cullMode, context.dynamicContext.settings.Wireframe);
                    graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                    counter.material++;
                    countIndexLocal += Materials[i].indexCount;
                }
                counter.vertex += rendererComponent.meshVertexCount;
            }
            _Counters counter2 = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                _RenderEntity(Entities[i].rendererComponent, context.CameraDataBuffers[cameraIndex], context.CBs_Bone[i], ref counter2);

            graphicsContext.SetRTV(context.outputRTV, Vector4.Zero, true);
            graphicsContext.SetCBVR(context.CameraDataBuffers[cameraIndex], 2);
            graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[0], 3);
            graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[1], 4);
            graphicsContext.SetSRVT(context.EnvironmentMap, 6);
            graphicsContext.SetSRVT(context.IrradianceMap, 7);
            graphicsContext.SetSRVT(context.BRDFLut, 8);

            graphicsContext.SetPObject(context.RPAssetsManager.PObjectDeferredRenderIBL, CullMode.back);
            graphicsContext.SetMesh(context.ndcQuadMesh);
            graphicsContext.DrawIndexed(context.ndcQuadMeshIndexCount, 0, 0);

            var lightings = context.dynamicContext.lightings;
            for (int i = lightings.Count - 1; i >= 0; i--)
            {
                if (lightings[i].LightingType == LightingType.Directional)
                {
                    graphicsContext.SetDSV(context.ShadowMap0, true);
                    graphicsContext.SetPObject(context.RPAssetsManager.PObjectMMDShadowDepth, CullMode.none);
                    graphicsContext.SetMesh(context.SkinningMeshBuffer);

                    void _RenderEntityShadow(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ref _Counters counter)
                    {
                        var Materials = rendererComponent.Materials;
                        graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                        graphicsContext.SetCBVR(cameraPresentData, 2);
                        graphicsContext.SetMeshIndex(rendererComponent.mesh);

                        //List<Texture2D> texs = rendererComponent.textures;
                        //int countIndexLocal = 0;
                        //for (int i = 0; i < Materials.Count; i++)
                        //{
                        //    if (Materials[i].DrawFlags.HasFlag(DrawFlag.CastSelfShadow))
                        //    {
                        //        Texture2D tex1 = null;
                        //        if (Materials[i].texIndex != -1)
                        //            tex1 = texs[Materials[i].texIndex];
                        //        graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                        //        graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                        //        graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                        //    }
                        //    counter.material++;
                        //    countIndexLocal += Materials[i].indexCount;
                        //}
                        graphicsContext.DrawIndexed(rendererComponent.meshIndexCount, 0, counter.vertex);
                        counter.vertex += rendererComponent.meshVertexCount;
                    }

                    _Counters counterShadow0 = new _Counters();
                    for (int j = 0; j < Entities.Count; j++)
                        _RenderEntityShadow(Entities[j].rendererComponent, lightingBuffers[i], context.CBs_Bone[j], ref counterShadow0);


                    graphicsContext.SetPObject(context.RPAssetsManager.PObjectDeferredRenderDirectLight, CullMode.back);
                    graphicsContext.SetRTV(context.outputRTV, Vector4.Zero, false);
                    graphicsContext.SetCBVR(lightingBuffers[i], 1);
                    graphicsContext.SetCBVR(context.CameraDataBuffers[cameraIndex], 2);
                    graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[0], 3);
                    graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[1], 4);
                    graphicsContext.SetSRVT(context.ScreenSizeDSVs[0], 6);
                    graphicsContext.SetSRVT(context.ShadowMap0, 7);
                    graphicsContext.SetMesh(context.ndcQuadMesh);
                    graphicsContext.DrawIndexed(context.ndcQuadMeshIndexCount, 0, 0);
                }
                else if (lightings[i].LightingType == LightingType.Point)
                {
                    graphicsContext.SetPObject(context.RPAssetsManager.PObjectDeferredRenderPointLight, CullMode.back);
                    graphicsContext.SetRTV(context.outputRTV, Vector4.Zero, false);
                    graphicsContext.SetCBVR(lightingBuffers[i], 1);
                    graphicsContext.SetCBVR(context.CameraDataBuffers[cameraIndex], 2);
                    graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[0], 3);
                    graphicsContext.SetSRVT(context.ScreenSizeRenderTextures[1], 4);
                    graphicsContext.SetSRVT(context.ScreenSizeDSVs[0], 6);
                    graphicsContext.SetSRVT(context.ShadowMap0, 7);
                    graphicsContext.SetMesh(context.cubeMesh);
                    graphicsContext.DrawIndexed(context.cubeMeshIndexCount, 0, 0);
                }
            }

            #region forward
            graphicsContext.SetCBVR(context.CameraDataBuffers[cameraIndex], 2);
            graphicsContext.SetMesh(context.SkinningMeshBuffer);

            if (HasMainLight && inShaderSettings.EnableShadow)
            {
                void _RenderEntityShadow(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ref _Counters counter)
                {
                    var Materials = rendererComponent.Materials;
                    graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                    graphicsContext.SetCBVR(cameraPresentData, 2);
                    graphicsContext.SetMeshIndex(rendererComponent.mesh);

                    //List<Texture2D> texs = rendererComponent.textures;
                    //int countIndexLocal = 0;
                    //for (int i = 0; i < Materials.Count; i++)
                    //{
                    //    if (Materials[i].DrawFlags.HasFlag(DrawFlag.CastSelfShadow))
                    //    {
                    //        Texture2D tex1 = null;
                    //        if (Materials[i].texIndex != -1)
                    //            tex1 = texs[Materials[i].texIndex];
                    //        graphicsContext.SetCBVR(materialBuffers[counter.material], 3);
                    //        graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 4);
                    //        graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                    //    }
                    //    counter.material++;
                    //    countIndexLocal += Materials[i].indexCount;
                    //}
                    graphicsContext.DrawIndexed(rendererComponent.meshIndexCount, 0, counter.vertex);
                    counter.vertex += rendererComponent.meshVertexCount;
                }

                graphicsContext.SetMesh(context.SkinningMeshBuffer);
                graphicsContext.SetPObject(context.RPAssetsManager.PObjectMMDShadowDepth, CullMode.none);
                graphicsContext.SetDSV(context.ShadowMap0, true);
                _Counters counterShadow0 = new _Counters();
                var LightCameraDataBuffers = context.LightCameraDataBuffers;
                for (int i = 0; i < Entities.Count; i++)
                    _RenderEntityShadow(Entities[i].rendererComponent, LightCameraDataBuffers[0], context.CBs_Bone[i], ref counterShadow0);
                graphicsContext.SetDSV(context.ShadowMap1, true);
                _Counters counterShadow1 = new _Counters();
                for (int i = 0; i < Entities.Count; i++)
                    _RenderEntityShadow(Entities[i].rendererComponent, LightCameraDataBuffers[1], context.CBs_Bone[i], ref counterShadow1);
            }
            graphicsContext.SetSRVT(context.ShadowMap0, 5);
            graphicsContext.SetSRVT(context.EnvironmentMap, 6);
            graphicsContext.SetSRVT(context.IrradianceMap, 7);
            graphicsContext.SetSRVT(context.BRDFLut, 8);
            graphicsContext.SetSRVT(context.ShadowMap1, 9);

            graphicsContext.SetRTVDSV(context.outputRTV, context.ScreenSizeDSVs[0], Vector4.Zero, false, false);
            void _RenderEntity2(MMDRendererComponent rendererComponent, ConstantBuffer cameraPresentData, ConstantBuffer entityBoneDataBuffer, ref _Counters counter)
            {
                var PODraw = PObjectStatusSelect(rendererComponent.PODraw, context.RPAssetsManager.PObjectMMDLoading, context.RPAssetsManager.PObjectMMDTransparent, context.RPAssetsManager.PObjectMMDError);
                var Materials = rendererComponent.Materials;
                List<Texture2D> texs = rendererComponent.textures;
                graphicsContext.SetMeshIndex(rendererComponent.mesh);
                graphicsContext.SetCBVR(entityBoneDataBuffer, 0);
                graphicsContext.SetCBVR(cameraPresentData, 2);
                //CooGExtension.SetCBVBuffer3(graphicsContext, entityBoneDataBuffer, entityDataBuffer, cameraPresentData, 0);
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
                    graphicsContext.SetCBVR(context.MaterialBuffers[counter.material], 1);
                    //graphicsContext.SetSRVT(TextureStatusSelect(tex1, textureLoading, textureError, textureError), 3);
                    //graphicsContext.SetSRVT(TextureStatusSelect(tex2, textureLoading, textureError, textureError), 4);
                    CooGExtension.SetSRVTexture2(graphicsContext, tex1, tex2, 3, textureLoading, textureError);
                    CullMode cullMode = CullMode.back;
                    if (Materials[i].DrawFlags.HasFlag(DrawFlag.DrawDoubleFace))
                        cullMode = CullMode.none;
                    graphicsContext.SetPObject(PODraw, cullMode, context.dynamicContext.settings.Wireframe);
                    graphicsContext.DrawIndexed(Materials[i].indexCount, countIndexLocal, counter.vertex);
                    counter.material++;
                    countIndexLocal += Materials[i].indexCount;
                }
                counter.vertex += rendererComponent.meshVertexCount;
            }
            _Counters counter3 = new _Counters();
            for (int i = 0; i < Entities.Count; i++)
                _RenderEntity2(Entities[i].rendererComponent, context.CameraDataBuffers[cameraIndex], context.CBs_Bone[i], ref counter3);
            #endregion
        }
    }
}
