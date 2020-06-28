using Coocoo3DGraphics;
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
        public VertexShader VSMMD = new VertexShader();
        public VertexShader VSUIStandard = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader uiPixelShader = new PixelShader();
        public GeometryShader particleGeometryShader = new GeometryShader();
        public GeometryShader uiGeometryShader = new GeometryShader();
        public PObject PObjectMMD = new PObject();
        public PObject PObjectMMDLoading = new PObject();
        public PObject PObjectMMDError = new PObject();
        public PObject uiPObject = new PObject();

        public Texture2D DepthStencil0 = new Texture2D();

        public Texture2D ui0Texture = new Texture2D();

        public Texture2D TextureLoading = new Texture2D();
        public Texture2D TextureError = new Texture2D();

        public bool Initilized = false;
        public Task LoadTask;
        public async Task ReloadDefalutResources(DeviceResources deviceResources)
        {
            ;
            DepthStencil0.ReloadAsDepthStencil(deviceResources, 4096, 4096);

            TextureLoading.ReloadPure(deviceResources, 1, 1, new System.Numerics.Vector4(0, 1, 1, 1));
            TextureError.ReloadPure(deviceResources, 1, 1, new System.Numerics.Vector4(1, 0, 1, 1));

            await ReloadVertexShader(VSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/VSMMD.cso");
            await ReloadVertexShader(VSUIStandard, deviceResources, "ms-appx:///Coocoo3DGraphics/VSUIStandard.cso");

            await ReloadGeometryShader(particleGeometryShader, deviceResources, "ms-appx:///Coocoo3DGraphics/GSParticleStandard.cso");
            await ReloadGeometryShader(uiGeometryShader, deviceResources, "ms-appx:///Coocoo3DGraphics/GSUIStandard.cso");

            await ReloadPixelShader(PSMMD, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMDLoading, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, deviceResources, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");
            await ReloadPixelShader(uiPixelShader, deviceResources, "ms-appx:///Coocoo3DGraphics/PSUIStandard.cso");

            await ReloadTexture2D(ui0Texture, deviceResources, "ms-appx:///Assets/Textures/UI_0.png");


            PObjectMMD.Reload(deviceResources, PObjectType.mmd, VSMMD, null, PSMMD);
            PObjectMMDLoading.Reload(deviceResources, PObjectType.mmd, VSMMD, null, PSMMDLoading);
            PObjectMMDError.Reload(deviceResources, PObjectType.mmd, VSMMD, null, PSMMDError);

            uiPObject.Reload(deviceResources, PObjectType.ui, VSUIStandard, uiGeometryShader, uiPixelShader);
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
        private async Task ReloadTexture2D(Texture2D texture2D, DeviceResources deviceResources, string uri)
        {
            texture2D.ReloadFromImage(deviceResources, await ReadAllBytes(uri));
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
