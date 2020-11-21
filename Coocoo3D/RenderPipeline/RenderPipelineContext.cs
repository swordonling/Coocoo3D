using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3D.ResourceWarp;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.RenderPipeline
{
    public class RenderPipelineDynamicContext
    {
        public Settings settings;
        public InShaderSettings inShaderSettings;
        public List<MMD3DEntity> entities = new List<MMD3DEntity>();
        public MMD3DEntity selectedEntity;
        public List<LightingData> lightings = new List<LightingData>();
        public List<LightingData> selectedLightings = new List<LightingData>();
        public List<CameraData> cameras = new List<CameraData>();
        public int VertexCount;
        public int frameRenderIndex;
        public int progressiveRenderIndex;
        public double Time;
        public double DeltaTime;
        public bool EnableDisplay;

        public int GetSceneObjectVertexCount()
        {
            int count = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                count += entities[i].rendererComponent.meshVertexCount;
            }
            VertexCount = count;
            return count;
        }

        public void Preprocess()
        {
            lightings.Sort();
        }

        public void ClearCollections()
        {
            entities.Clear();
            lightings.Clear();
            selectedLightings.Clear();
            cameras.Clear();
        }
    }
    public class RenderPipelineContext
    {
        const int c_entityDataBufferSize = 65536;
        public byte[] bigBuffer = new byte[65536];
        GCHandle _bigBufferHandle;

        public const int c_maxCameraPerRender = 2;
        public const int c_lightCameraCount = 2;
        public const int c_presentDataSize = 512;
        public ConstantBuffer[] CameraDataBuffers = new ConstantBuffer[c_maxCameraPerRender];
        public ConstantBuffer[] LightCameraDataBuffers = new ConstantBuffer[c_lightCameraCount];
        public const int c_materialDataSize = 768;
        public List<ConstantBuffer> MaterialBuffers = new List<ConstantBuffer>();
        public void DesireMaterialBuffers(int count)
        {
            while (MaterialBuffers.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_materialDataSize);
                MaterialBuffers.Add(constantBuffer);
            }
        }

        public RenderTexture2D outputRTV = new RenderTexture2D();
        public RenderTexture2D[] ScreenSizeRenderTextures = new RenderTexture2D[4];
        public RenderTexture2D[] ScreenSizeDSVs = new RenderTexture2D[2];

        public RenderTexture2D ShadowMap0 = new RenderTexture2D();
        public RenderTexture2D ShadowMap1 = new RenderTexture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();
        public TextureCube SkyBox = new TextureCube();
        public RenderTextureCube IrradianceMap = new RenderTextureCube();
        public RenderTextureCube EnvironmentMap = new RenderTextureCube();

        public MMDMesh ndcQuadMesh = new MMDMesh();
        public MMDMesh cubeMesh = new MMDMesh();
        public MMDMesh cubeWireMesh = new MMDMesh();
        public int ndcQuadMeshIndexCount;
        public int cubeMeshIndexCount;
        public int cubeWireMeshIndexCount;
        public MeshBuffer SkinningMeshBuffer = new MeshBuffer();
        public TwinBuffer LightCacheBuffer = new TwinBuffer();
        public int SkinningMeshBufferSize;
        public int frameRenderCount;

        public RPAssetsManager RPAssetsManager = new RPAssetsManager();
        public DeviceResources deviceResources = new DeviceResources();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public GraphicsContext graphicsContext1 = new GraphicsContext();
        public GraphicsContext[] graphicsContexts;

        public Texture2D UI1Texture = new Texture2D();
        public Texture2D BRDFLut = new Texture2D();
        public Texture2D postProcessBackground = new Texture2D();

        public ReadBackTexture2D ReadBackTexture2D = new ReadBackTexture2D();

        public RenderPipelineDynamicContext dynamicContext = new RenderPipelineDynamicContext();
        public RenderPipelineDynamicContext dynamicContext1 = new RenderPipelineDynamicContext();

        public List<ConstantBuffer> CBs_Bone = new List<ConstantBuffer>();

        public DxgiFormat middleFormat = DxgiFormat.DXGI_FORMAT_R16G16B16A16_UNORM;
        public DxgiFormat swapChainFormat = DxgiFormat.DXGI_FORMAT_B8G8R8A8_UNORM;
        public DxgiFormat depthFormat = DxgiFormat.DXGI_FORMAT_D24_UNORM_S8_UINT;

        public int screenWidth;
        public int screenHeight;
        public float dpi = 96.0f;
        public float logicScale = 1;

        public RenderPipelineContext()
        {
            _bigBufferHandle = GCHandle.Alloc(bigBuffer);
            for (int i = 0; i < ScreenSizeRenderTextures.Length; i++)
            {
                ScreenSizeRenderTextures[i] = new RenderTexture2D();
            }
            for (int i = 0; i < ScreenSizeDSVs.Length; i++)
            {
                ScreenSizeDSVs[i] = new RenderTexture2D();
            }
            for (int i = 0; i < CameraDataBuffers.Length; i++)
            {
                CameraDataBuffers[i] = new ConstantBuffer();
            }
            for (int i = 0; i < LightCameraDataBuffers.Length; i++)
            {
                LightCameraDataBuffers[i] = new ConstantBuffer();
            }
        }
        ~RenderPipelineContext()
        {
            _bigBufferHandle.Free();
        }
        public void Reload()
        {
            graphicsContext.Reload(deviceResources);
            graphicsContext1.Reload(deviceResources);
        }

        struct _Data1
        {
            public int vertexStart;
            public int indexStart;
            public int vertexCount;
            public int indexCount;
        }
        public void UpdateGPUResource()
        {
            #region Update bone data
            int count = dynamicContext.entities.Count;
            while (CBs_Bone.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_entityDataBufferSize);
                CBs_Bone.Add(constantBuffer);
            }
            _Data1 data1 = new _Data1();
            Vector3 camPos = dynamicContext.cameras[0].Pos;
            for (int i = 0; i < count; i++)
            {
                var entity = dynamicContext.entities[i];
                var rendererComponent = entity.rendererComponent;
                data1.vertexCount = rendererComponent.meshVertexCount;
                data1.indexCount = rendererComponent.meshIndexCount;
                IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(bigBuffer, 0);
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(entity.Rotation) * Matrix4x4.CreateTranslation(entity.Position - camPos);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), ptr1, true);
                Marshal.StructureToPtr(rendererComponent.amountAB, ptr1 + 64, true);
                Marshal.StructureToPtr(rendererComponent.meshVertexCount, ptr1 + 68, true);
                Marshal.StructureToPtr(rendererComponent.meshIndexCount, ptr1 + 72, true);
                Marshal.StructureToPtr(data1, ptr1 + 80, true);

                graphicsContext.UpdateResource(CBs_Bone[i], bigBuffer, 256, 0);
                graphicsContext.UpdateResourceRegion(CBs_Bone[i], 256, dynamicContext.entities[i].boneComponent.boneMatricesData, 65280, 0);
                data1.vertexStart += rendererComponent.meshVertexCount;
                data1.indexStart += rendererComponent.meshIndexCount;


                if (rendererComponent.meshNeedUpdateA)
                {
                    graphicsContext.UpdateVerticesPos(rendererComponent.meshAppend, rendererComponent.meshPosData1, 0);
                    rendererComponent.meshNeedUpdateA = false;
                }
                if (rendererComponent.meshNeedUpdateB)
                {
                    graphicsContext.UpdateVerticesPos(rendererComponent.meshAppend, rendererComponent.meshPosData2, 1);
                    rendererComponent.meshNeedUpdateB = false;
                }
            }
            #endregion
        }

        public void ReloadTextureSizeResources(ProcessingList processingList)
        {
            int x = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Width), 1);
            int y = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Height), 1);
            outputRTV.ReloadAsRTVUAV(x, y, middleFormat);
            processingList.UnsafeAdd(outputRTV);
            for (int i = 0; i < ScreenSizeRenderTextures.Length; i++)
            {
                ScreenSizeRenderTextures[i].ReloadAsRTVUAV(x, y, middleFormat);
                processingList.UnsafeAdd(ScreenSizeRenderTextures[i]);
            }
            for (int i = 0; i < ScreenSizeDSVs.Length; i++)
            {
                ScreenSizeDSVs[i].ReloadAsDepthStencil(x, y, depthFormat);
                processingList.UnsafeAdd(ScreenSizeDSVs[i]);
            }
            ReadBackTexture2D.Reload(x, y, 4);
            processingList.UnsafeAdd(ReadBackTexture2D);
            screenWidth = x;
            screenHeight = y;
            dpi = deviceResources.GetDpi();
            logicScale = dpi / 96.0f;
        }

        const int c_shadowMapResolutionLow = 2048;
        const int c_shadowMapResolutionHigh = 4096;
        public bool HighResolutionShadowNow;
        public void ChangeShadowMapsQuality(ProcessingList processingList, bool highQuality)
        {
            if (HighResolutionShadowNow == highQuality) return;
            HighResolutionShadowNow = highQuality;
            if (highQuality)
            {
                ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionHigh, c_shadowMapResolutionHigh, depthFormat);
                ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionHigh, c_shadowMapResolutionHigh, depthFormat);
            }
            else
            {
                ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow, depthFormat);
                ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow, depthFormat);
            }
            processingList.UnsafeAdd(ShadowMap0);
            processingList.UnsafeAdd(ShadowMap1);
        }

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(ProcessingList processingList, MiscProcessContext miscProcessContext)
        {
            for (int i = 0; i < CameraDataBuffers.Length; i++)
            {
                CameraDataBuffers[i].Reload(deviceResources, c_presentDataSize);
            }
            for (int i = 0; i < LightCameraDataBuffers.Length; i++)
            {
                LightCameraDataBuffers[i].Reload(deviceResources, c_presentDataSize);
            }

            ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow, depthFormat);
            ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow, depthFormat);
            processingList.AddObject(ShadowMap0);
            processingList.AddObject(ShadowMap1);

            Uploader upTexLoading = new Uploader();
            Uploader upTexError = new Uploader();
            upTexLoading.Texture2DPure(1, 1, new Vector4(0, 1, 1, 1));
            upTexError.Texture2DPure(1, 1, new Vector4(1, 0, 1, 1)); ;
            processingList.AddObject(new Texture2DUploadPack(TextureLoading, upTexLoading));
            processingList.AddObject(new Texture2DUploadPack(TextureError, upTexError));
            Uploader upTexPostprocessBackground = new Uploader();
            upTexPostprocessBackground.Texture2DPure(64, 64, new Vector4(1, 1, 1, 0));
            processingList.AddObject(new Texture2DUploadPack(postProcessBackground, upTexPostprocessBackground));

            Uploader upTexEnvCube = new Uploader();
            upTexEnvCube.TextureCubePure(32, 32, new Vector4[] { new Vector4(0.2f, 0.16f, 0.16f, 1), new Vector4(0.16f, 0.2f, 0.16f, 1), new Vector4(0.2f, 0.2f, 0.2f, 1), new Vector4(0.16f, 0.2f, 0.2f, 1), new Vector4(0.2f, 0.2f, 0.16f, 1), new Vector4(0.16f, 0.16f, 0.2f, 1) });

            IrradianceMap.ReloadAsRTVUAV(32, 32, 1, DxgiFormat.DXGI_FORMAT_R32G32B32A32_FLOAT);
            EnvironmentMap.ReloadAsRTVUAV(1024, 1024, 7, DxgiFormat.DXGI_FORMAT_R16G16B16A16_FLOAT);
            miscProcessContext.Add(new P_Env_Data() { source = SkyBox, IrradianceMap = IrradianceMap, EnvMap = EnvironmentMap, Level = 16 });
            processingList.AddObject(new TextureCubeUploadPack(SkyBox, upTexEnvCube));
            processingList.AddObject(IrradianceMap);
            processingList.AddObject(EnvironmentMap);

            ndcQuadMesh.ReloadNDCQuad();
            ndcQuadMeshIndexCount = ndcQuadMesh.m_indexCount;
            processingList.AddObject(ndcQuadMesh);

            cubeMesh.ReloadCube();
            cubeMeshIndexCount = cubeMesh.m_indexCount;
            processingList.AddObject(cubeMesh);

            cubeWireMesh.ReloadCubeWire();
            cubeWireMeshIndexCount = cubeWireMesh.m_indexCount;
            processingList.AddObject(cubeWireMesh);

            await ReloadTexture2DNoMip(BRDFLut, processingList, "ms-appx:///Assets/Textures/brdflut.png");
            await ReloadTexture2DNoMip(UI1Texture, processingList, "ms-appx:///Assets/Textures/UI_1.png");

            Initilized = true;
        }
        private async Task ReloadTexture2D(Texture2D texture2D, ProcessingList processingList, string uri)
        {
            Uploader uploader = new Uploader();
            uploader.Texture2D(await FileIO.ReadBufferAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri))), true, true);
            processingList.AddObject(new Texture2DUploadPack(texture2D, uploader));
        }
        private async Task ReloadTexture2DNoMip(Texture2D texture2D, ProcessingList processingList, string uri)
        {
            Uploader uploader = new Uploader();
            uploader.Texture2D(await FileIO.ReadBufferAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri))), false, false);
            processingList.AddObject(new Texture2DUploadPack(texture2D, uploader));
        }
    }
}
