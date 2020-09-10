using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.RenderPipeline
{
    public class RenderPipelineContext
    {
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

        public Settings settings;
        //public Scene scene;

        public List<MMD3DEntity> entities = new List<MMD3DEntity>();
        public List<Lighting> lightings = new List<Lighting>();
        public List<Camera> cameras = new List<Camera>();


        public RenderPipelineContext()
        {
            for (int i = 0; i < ScreenSizeRenderTextures.Length; i++)
            {
                ScreenSizeRenderTextures[i] = new RenderTexture2D();
            }
            for (int i = 0; i < ScreenSizeDSVs.Length; i++)
            {
                ScreenSizeDSVs[i] = new RenderTexture2D();
            }
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

        public void ClearCollections()
        {
            entities.Clear();
            lightings.Clear();
            cameras.Clear();
        }
    }
}
