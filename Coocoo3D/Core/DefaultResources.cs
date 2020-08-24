using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Numerics;

namespace Coocoo3D.Core
{
    public class DefaultResources
    {
        public RenderTexture2D DepthStencil0 = new RenderTexture2D();

        public RenderTexture2D[] ScreenSizeRenderTextures = new RenderTexture2D[4];
        public RenderTexture2D ScreenSizeRenderTextureOutput = new RenderTexture2D();
        public RenderTexture2D ScreenSizeDepthStencilOutput = new RenderTexture2D();

        public Texture2D ui0Texture = new Texture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();
        public TextureCube EnvironmentCube = new TextureCube();
        public RenderTextureCube IrradianceMap = new RenderTextureCube();

        public MMDMesh quadMesh = new MMDMesh();
        public DefaultResources()
        {
            for (int i = 0; i < ScreenSizeRenderTextures.Length; i++)
            {
                ScreenSizeRenderTextures[i] = new RenderTexture2D();
            }
        }

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(WICFactory wic, ProcessingList mainCaches, RenderPipeline.MiscProcessContext miscProcessContext)
        {
            DepthStencil0.ReloadAsDepthStencil(4096, 4096);
            mainCaches.AddObject(DepthStencil0);

            TextureLoading.ReloadPure(1, 1, new Vector4(0, 1, 1, 1));
            TextureError.ReloadPure(1, 1, new Vector4(1, 0, 1, 1));
            EnvironmentCube.ReloadPure(64, 64, new Vector4[] { new Vector4(0.5f, 0.4f, 0.4f, 1), new Vector4(0.4f, 0.5f, 0.4f, 1), new Vector4(0.5f, 0.4f, 0.5f, 1), new Vector4(0.08f, 0.1f, 0.1f, 1) , new Vector4(0.5f, 0.5f, 0.4f, 1), new Vector4(0.4f, 0.4f, 0.5f, 1)});
            IrradianceMap.ReloadRTVUAV(32, 32, DxgiFormat.DXGI_FORMAT_R32G32B32A32_FLOAT);
            miscProcessContext.Add(new RenderPipeline.MiscProcessPair<TextureCube, RenderTextureCube>(EnvironmentCube, IrradianceMap, RenderPipeline.MiscProcessType.GenerateIrradianceMap));
            mainCaches.AddObject(TextureLoading);
            mainCaches.AddObject(TextureError);
            mainCaches.AddObject(EnvironmentCube);
            mainCaches.AddObject(IrradianceMap);

            quadMesh.ReloadNDCQuad();
            mainCaches.AddObject(quadMesh);

            await ReloadTexture2D(ui0Texture, wic, mainCaches, "ms-appx:///Assets/Textures/UI_0.png");

            //uiPObject.Reload(deviceResources, PObjectType.ui3d, VSUIStandard, uiGeometryShader, uiPixelShader);
            Initilized = true;
        }

        private async Task ReloadPixelShader(PixelShader pixelShader, DeviceResources deviceResources, string uri)
        {
            pixelShader.Reload(deviceResources, await ReadAllBytes(uri));
        }
        private async Task ReloadVertexShader(VertexShader vertexShader, DeviceResources deviceResources, string uri)
        {
            vertexShader.Reload(deviceResources, await ReadAllBytes(uri));
        }

        private async Task ReloadGeometryShader(GeometryShader geometryShader, DeviceResources deviceResources, string uri)
        {
            geometryShader.Reload(deviceResources, await ReadAllBytes(uri));
        }
        private async Task ReloadTexture2D(Texture2D texture2D, WICFactory wic, ProcessingList mainCaches, string uri)
        {
            texture2D.ReloadFromImage1(wic, await ReadAllBytes(uri));
            mainCaches.AddObject(texture2D);
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
