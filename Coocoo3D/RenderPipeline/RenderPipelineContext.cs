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

        public RenderTexture2D DSV0 = new RenderTexture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();
        public TextureCube EnvCubeMap = new TextureCube();
        public RenderTextureCube IrradianceMap = new RenderTextureCube();

        public MMDMesh ndcQuadMesh = new MMDMesh();

        public RPAssetsManager RPAssetsManager;
        public DeviceResources deviceResources;
        public GraphicsContext graphicsContext;
        public GraphicsContext[] graphicsContexts;

        public Texture2D ui0Texture = new Texture2D();
        public Texture2D postProcessBackground = new Texture2D();

        public RenderPipelineDynamicContext renderPipelineDynamicContext = new RenderPipelineDynamicContext();
        public RenderPipelineDynamicContext renderPipelineDynamicContext1 = new RenderPipelineDynamicContext();

        public List<ConstantBuffer> CBs_Bone = new List<ConstantBuffer>();

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

        public void UpdateGPUResource()
        {
            #region Update bone data
            int count = renderPipelineDynamicContext.entities.Count;
            while (CBs_Bone.Count < count)
            {
                ConstantBuffer constantBuffer = new ConstantBuffer();
                constantBuffer.Reload(deviceResources, c_entityDataBufferSize);
                CBs_Bone.Add(constantBuffer);
            }
            for (int i = 0; i < count; i++)
            {
                var entity = renderPipelineDynamicContext.entities[i];
                IntPtr ptr1 = Marshal.UnsafeAddrOfPinnedArrayElement(bigBuffer, 0);
                Matrix4x4 world = Matrix4x4.CreateFromQuaternion(entity.Rotation) * Matrix4x4.CreateTranslation(entity.Position);
                Marshal.StructureToPtr(Matrix4x4.Transpose(world), ptr1, true);
                Marshal.StructureToPtr(entity.rendererComponent.amountAB, ptr1 + 64, true);
                Marshal.StructureToPtr(entity.rendererComponent.mesh.m_vertexCount, ptr1 + 68, true);
                Marshal.StructureToPtr(entity.rendererComponent.mesh.m_indexCount, ptr1 + 72, true);

                graphicsContext.UpdateResource(CBs_Bone[i], bigBuffer, 256, 0);
                graphicsContext.UpdateResourceRegion(CBs_Bone[i], 256, renderPipelineDynamicContext.entities[i].boneComponent.boneMatricesData, 65280, 0);
            }
            #endregion
        }

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(WICFactory wic, ProcessingList processingList, MiscProcessContext miscProcessContext)
        {
            DSV0.ReloadAsDepthStencil(4096, 4096);
            processingList.AddObject(DSV0);

            TextureLoading.ReloadPure(1, 1, new Vector4(0, 1, 1, 1));
            TextureError.ReloadPure(1, 1, new Vector4(1, 0, 1, 1));
            EnvCubeMap.ReloadPure(64, 64, new Vector4[] { new Vector4(0.5f, 0.4f, 0.4f, 1), new Vector4(0.4f, 0.5f, 0.4f, 1), new Vector4(0.5f, 0.4f, 0.5f, 1), new Vector4(0.08f, 0.1f, 0.1f, 1), new Vector4(0.5f, 0.5f, 0.4f, 1), new Vector4(0.4f, 0.4f, 0.5f, 1) });
            IrradianceMap.ReloadRTVUAV(32, 32, DxgiFormat.DXGI_FORMAT_R32G32B32A32_FLOAT);
            postProcessBackground.ReloadPure(64, 64, new Vector4(1, 1, 1, 0));
            miscProcessContext.Add(new MiscProcessPair<TextureCube, RenderTextureCube>(EnvCubeMap, IrradianceMap, MiscProcessType.GenerateIrradianceMap));
            processingList.AddObject(TextureLoading);
            processingList.AddObject(TextureError);
            processingList.AddObject(EnvCubeMap);
            processingList.AddObject(IrradianceMap);

            ndcQuadMesh.ReloadNDCQuad();
            processingList.AddObject(ndcQuadMesh);

            processingList.AddObject(postProcessBackground);
            await ReloadTexture2D(ui0Texture, wic, processingList, "ms-appx:///Assets/Textures/UI_0.png");

            //uiPObject.Reload(deviceResources, PObjectType.ui3d, VSUIStandard, uiGeometryShader, uiPixelShader);
            Initilized = true;
        }
        private async Task ReloadTexture2D(Texture2D texture2D, WICFactory wic, ProcessingList processingList, string uri)
        {
            texture2D.ReloadFromImage1(wic, await ReadAllBytes(uri));
            processingList.AddObject(texture2D);
        }
        private async Task<byte[]> ReadAllBytes(string uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            var stream = await file.OpenReadAsync();
            DataReader dataReader = new DataReader(stream);
            await dataReader.LoadAsync((uint)stream.Size);
            byte[] data = new byte[stream.Size];
            dataReader.ReadBytes(data);
            stream.Dispose();
            dataReader.Dispose();
            return data;
        }
    }
}
