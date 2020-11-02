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
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.RenderPipeline
{
    public class RenderPipelineDynamicContext
    {
        public Settings settings;
        public InShaderSettings inShaderSettings;
        public List<MMD3DEntity> entities = new List<MMD3DEntity>();
        public List<LightingData> lightings = new List<LightingData>();
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
            cameras.Clear();
        }
    }
    public class RenderPipelineContext
    {
        const int c_entityDataBufferSize = 65536;
        byte[] bigBuffer = new byte[65536];
        GCHandle _bigBufferHandle;
        public RenderTexture2D outputRTV = new RenderTexture2D();
        public RenderTexture2D[] ScreenSizeRenderTextures = new RenderTexture2D[4];
        public RenderTexture2D[] ScreenSizeDSVs = new RenderTexture2D[2];

        public RenderTexture2D ShadowMap0 = new RenderTexture2D();
        public RenderTexture2D ShadowMap1 = new RenderTexture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();
        public TextureCube EnvCubeMap = new TextureCube();
        public RenderTextureCube IrradianceMap = new RenderTextureCube();
        public RenderTextureCube EnvironmentMap = new RenderTextureCube();

        public MMDMesh ndcQuadMesh = new MMDMesh();
        public int ndcQuadMeshIndexCount;
        public MeshBuffer SkinningMeshBuffer = new MeshBuffer();
        public TwinBuffer LightCacheBuffer = new TwinBuffer();
        public int SkinningMeshBufferSize;
        public int frameRenderIndex;

        public RPAssetsManager RPAssetsManager;
        public DeviceResources deviceResources;
        public GraphicsContext graphicsContext;
        public GraphicsContext[] graphicsContexts;

        //public Texture2D ui0Texture = new Texture2D();
        public Texture2D BRDFLut = new Texture2D();
        public Texture2D postProcessBackground = new Texture2D();

        public RenderPipelineDynamicContext dynamicContext = new RenderPipelineDynamicContext();
        public RenderPipelineDynamicContext dynamicContext1 = new RenderPipelineDynamicContext();

        public List<ConstantBuffer> CBs_Bone = new List<ConstantBuffer>();

        public DxgiFormat RTFormat = DxgiFormat.DXGI_FORMAT_R16G16B16A16_UNORM;
        public DxgiFormat backBufferFormat = DxgiFormat.DXGI_FORMAT_B8G8R8A8_UNORM;

        public int width;
        public int height;

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
        }
        ~RenderPipelineContext()
        {
            _bigBufferHandle.Free();
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
            for (int i = 0; i < count; i++)
            {
                var entity = dynamicContext.entities[i];
                data1.vertexCount = entity.rendererComponent.meshVertexCount;
                data1.indexCount = entity.rendererComponent.meshIndexCount;
                IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(bigBuffer, 0);
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(entity.Rotation) * Matrix4x4.CreateTranslation(entity.Position);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), ptr1, true);
                Marshal.StructureToPtr(entity.rendererComponent.amountAB, ptr1 + 64, true);
                Marshal.StructureToPtr(entity.rendererComponent.meshVertexCount, ptr1 + 68, true);
                Marshal.StructureToPtr(entity.rendererComponent.meshIndexCount, ptr1 + 72, true);
                Marshal.StructureToPtr(data1, ptr1 + 80, true);

                graphicsContext.UpdateResource(CBs_Bone[i], bigBuffer, 256, 0);
                graphicsContext.UpdateResourceRegion(CBs_Bone[i], 256, dynamicContext.entities[i].boneComponent.boneMatricesData, 65280, 0);
                data1.vertexStart += entity.rendererComponent.meshVertexCount;
                data1.indexStart += entity.rendererComponent.meshIndexCount;
            }
            #endregion
        }

        public void ReloadTextureSizeResources(ProcessingList processingList)
        {
            int x = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Width), 1);
            int y = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Height), 1);
            outputRTV.ReloadAsRTVUAV(x, y, RTFormat);
            processingList.UnsafeAdd(outputRTV);
            for (int i = 0; i < ScreenSizeRenderTextures.Length; i++)
            {
                ScreenSizeRenderTextures[i].ReloadAsRTVUAV(x, y, RTFormat);
                processingList.UnsafeAdd(ScreenSizeRenderTextures[i]);
            }
            for (int i = 0; i < ScreenSizeDSVs.Length; i++)
            {
                ScreenSizeDSVs[i].ReloadAsDepthStencil(x, y);
                processingList.UnsafeAdd(ScreenSizeDSVs[i]);
            }
            width = x;
            height = y;
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
                ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionHigh, c_shadowMapResolutionHigh);
                ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionHigh, c_shadowMapResolutionHigh);
            }
            else
            {
                ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow);
                ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow);
            }
            processingList.UnsafeAdd(ShadowMap0);
            processingList.UnsafeAdd(ShadowMap1);
        }

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(WICFactory wic, ProcessingList processingList, MiscProcessContext miscProcessContext)
        {
            ShadowMap0.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow);
            ShadowMap1.ReloadAsDepthStencil(c_shadowMapResolutionLow, c_shadowMapResolutionLow);
            processingList.AddObject(ShadowMap0);
            processingList.AddObject(ShadowMap1);

            TextureLoading.ReloadPure(1, 1, new Vector4(0, 1, 1, 1));
            TextureError.ReloadPure(1, 1, new Vector4(1, 0, 1, 1));
            EnvCubeMap.ReloadPure(64, 64, new Vector4[] { new Vector4(0.2f, 0.16f, 0.16f, 1), new Vector4(0.16f, 0.2f, 0.16f, 1), new Vector4(0.2f, 0.2f, 0.2f, 1), new Vector4(0.16f, 0.2f, 0.2f, 1), new Vector4(0.2f, 0.2f, 0.16f, 1), new Vector4(0.16f, 0.16f, 0.2f, 1) });
            IrradianceMap.ReloadAsRTVUAV(32, 32, 1, DxgiFormat.DXGI_FORMAT_R32G32B32A32_FLOAT);
            EnvironmentMap.ReloadAsRTVUAV(1024, 1024, 7, DxgiFormat.DXGI_FORMAT_R16G16B16A16_FLOAT);
            postProcessBackground.ReloadPure(64, 64, new Vector4(1, 1, 1, 0));
            miscProcessContext.Add(new P_Env_Data() { source = EnvCubeMap, IrradianceMap = IrradianceMap, EnvMap = EnvironmentMap, Level = 16 }); ;
            processingList.AddObject(TextureLoading);
            processingList.AddObject(TextureError);
            processingList.AddObject(EnvCubeMap);
            processingList.AddObject(IrradianceMap);
            processingList.AddObject(EnvironmentMap);

            ndcQuadMesh.ReloadNDCQuad();
            ndcQuadMeshIndexCount = ndcQuadMesh.m_indexCount;
            processingList.AddObject(ndcQuadMesh);

            processingList.AddObject(postProcessBackground);

            await ReloadTexture2DNoMip(BRDFLut, wic, processingList, "ms-appx:///Assets/Textures/brdflut.png");
            //await ReloadTexture2D(ui0Texture, wic, processingList, "ms-appx:///Assets/Textures/UI_0.png");

            //uiPObject.Reload(deviceResources, PObjectType.ui3d, VSUIStandard, uiGeometryShader, uiPixelShader);
            Initilized = true;
        }
        private async Task ReloadTexture2D(Texture2D texture2D, WICFactory wic, ProcessingList processingList, string uri)
        {
            texture2D.ReloadFromImage(await FileIO.ReadBufferAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri))));
            processingList.AddObject(texture2D);
        }
        private async Task ReloadTexture2DNoMip(Texture2D texture2D, WICFactory wic, ProcessingList processingList, string uri)
        {
            texture2D.ReloadFromImageNoMip(await FileIO.ReadBufferAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri))), false);
            processingList.AddObject(texture2D);
        }
    }
}
