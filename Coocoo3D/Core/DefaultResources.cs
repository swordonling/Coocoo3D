﻿using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.Core
{
    public class DefaultResources
    {
        public VertexShader VSUIStandard = new VertexShader();
        public PixelShader uiPixelShader = new PixelShader();
        public GeometryShader uiGeometryShader = new GeometryShader();
        public PObject uiPObject = new PObject();

        public RenderTexture2D DepthStencil0 = new RenderTexture2D();

        public RenderTexture2D ScreenSizeRenderTexture0 = new RenderTexture2D();
        public RenderTexture2D ScreenSizeDepthStencil0 = new RenderTexture2D();

        public Texture2D ui0Texture = new Texture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();

        public MMDMesh quadMesh = new MMDMesh();

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(DeviceResources deviceResources, MainCaches mainCaches)
        {
            DepthStencil0.ReloadAsDepthStencil(deviceResources, 4096, 4096);
            mainCaches.AddRenderTextureToUpdateList(DepthStencil0);

            TextureLoading.ReloadPure(1, 1, new System.Numerics.Vector4(0, 1, 1, 1));
            TextureError.ReloadPure(1, 1, new System.Numerics.Vector4(1, 0, 1, 1));
            mainCaches.AddTextureToLoadList(TextureLoading);
            mainCaches.AddTextureToLoadList(TextureError);

            quadMesh.ReloadNDCQuad();
            mainCaches.AddMeshToLoadList(quadMesh);

            await ReloadVertexShader(VSUIStandard, deviceResources, "ms-appx:///Coocoo3DGraphics/VSUIStandard.cso");
            
            await ReloadGeometryShader(uiGeometryShader, deviceResources, "ms-appx:///Coocoo3DGraphics/GSUIStandard.cso");
            
            await ReloadPixelShader(uiPixelShader, deviceResources, "ms-appx:///Coocoo3DGraphics/PSUIStandard.cso");

            await ReloadTexture2D(ui0Texture, deviceResources, mainCaches, "ms-appx:///Assets/Textures/UI_0.png");

            

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
        private async Task ReloadTexture2D(Texture2D texture2D, DeviceResources deviceResources, MainCaches mainCaches, string uri)
        {
            texture2D.ReloadFromImage1(deviceResources, await ReadAllBytes(uri));
            mainCaches.AddTextureToLoadList(texture2D);
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
