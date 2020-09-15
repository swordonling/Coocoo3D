using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using GSD = Coocoo3DGraphics.GraphicSignatureDesc;

namespace Coocoo3D.RenderPipeline
{
    public class RPAssetsManager
    {
        public GraphicsSignature rootSignature = new GraphicsSignature();
        public GraphicsSignature rootSignaturePostProcess = new GraphicsSignature();
        public GraphicsSignature rootSignatureCompute = new GraphicsSignature();
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public VertexShader VSSkyBox = new VertexShader();
        public VertexShader VSPostProcess = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMD_DisneyBrdf = new PixelShader();
        public PixelShader PSMMD_Toon1 = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader PSMMDAlphaClip = new PixelShader();
        public PixelShader PSMMDAlphaClip1 = new PixelShader();
        public PixelShader PSSkyBox = new PixelShader();
        public PixelShader PSPostProcess = new PixelShader();
        public PObject PObjectMMDSkinning = new PObject();
        public PObject PObjectMMD = new PObject();
        public PObject PObjectMMD_DisneyBrdf = new PObject();
        public PObject PObjectMMD_Toon1 = new PObject();
        public PObject PObjectMMDShadowDepth = new PObject();
        public PObject PObjectMMDDepth = new PObject();
        public PObject PObjectMMDLoading = new PObject();
        public PObject PObjectMMDError = new PObject();
        public PObject PObjectSkyBox = new PObject();
        public PObject PObjectPostProcess = new PObject();
        public DxgiFormat CurrentRenderTargetFormat;
        public bool Ready;
        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.ReloadMMD(deviceResources);
            rootSignaturePostProcess.Reload(deviceResources, new GraphicSignatureDesc[] { GSD.CBV, GSD.SRVTable, GSD.SRVTable });
            rootSignatureCompute.ReloadCompute(deviceResources, new GraphicSignatureDesc[] { GSD.CBV,GSD.CBV,GSD.CBV,GSD.SRV, GSD.UAV, GSD.UAV });
        }
        public async Task ReloadAssets()
        {
            await ReloadVertexShader(VSMMDSkinning2, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");
            await ReloadVertexShader(VSSkyBox, "ms-appx:///Coocoo3DGraphics/VSSkyBox.cso");
            await ReloadPixelShader(PSMMD, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMD_DisneyBrdf, "ms-appx:///Coocoo3DGraphics/PSMMD_DisneyBRDF.cso");
            await ReloadPixelShader(PSMMD_Toon1, "ms-appx:///Coocoo3DGraphics/PSMMD_Toon1.cso");
            await ReloadPixelShader(PSMMDLoading, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");
            await ReloadPixelShader(PSMMDAlphaClip, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip.cso");
            await ReloadPixelShader(PSMMDAlphaClip1, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip1.cso");
            await ReloadPixelShader(PSSkyBox, "ms-appx:///Coocoo3DGraphics/PSSkyBox.cso");


            await ReloadVertexShader(VSPostProcess, "ms-appx:///Coocoo3DGraphics/VSPostProcess.cso");
            await ReloadPixelShader(PSPostProcess, "ms-appx:///Coocoo3DGraphics/PSPostProcess.cso");
        }
        public void ChangeRenderTargetFormat(DeviceResources deviceResources, DxgiFormat format, DxgiFormat backBufferFormat)
        {
            CurrentRenderTargetFormat = format;
            PObjectMMDSkinning.ReloadSkinning(deviceResources, rootSignature, VSMMDSkinning2, null);

            PObjectMMD.ReloadDrawing(deviceResources, rootSignature, BlendState.alpha, VSMMDTransform,null, PSMMD, format);
            PObjectMMD_DisneyBrdf.ReloadDrawing(deviceResources, rootSignature, BlendState.alpha, VSMMDTransform, null, PSMMD_DisneyBrdf, format);
            PObjectMMD_Toon1.ReloadDrawing(deviceResources, rootSignature, BlendState.alpha, VSMMDTransform, null, PSMMD_Toon1, format);
            PObjectMMDLoading.ReloadDrawing(deviceResources, rootSignature, BlendState.alpha, VSMMDTransform, null, PSMMDLoading, format);
            PObjectMMDError.ReloadDrawing(deviceResources, rootSignature, BlendState.alpha, VSMMDTransform, null, PSMMDError, format);

            PObjectSkyBox.Reload(deviceResources, rootSignature, PObjectType.postProcess, BlendState.none, VSSkyBox, null, PSSkyBox, format);
            PObjectMMDShadowDepth.ReloadDepthOnly(deviceResources, rootSignature, VSMMDTransform, PSMMDAlphaClip, 10000);
            PObjectMMDDepth.ReloadDepthOnly(deviceResources, rootSignature, VSMMDTransform, PSMMDAlphaClip1, 0);

            PObjectPostProcess.Reload(deviceResources, rootSignaturePostProcess, PObjectType.postProcess, BlendState.none, VSPostProcess, null, PSPostProcess, backBufferFormat);
            Ready = true;
        }
        protected async Task ReloadPixelShader(PixelShader pixelShader, string uri)
        {
            pixelShader.Reload(await ReadAllBytes(uri));
        }
        protected async Task ReloadVertexShader(VertexShader vertexShader, string uri)
        {
            vertexShader.Reload(await ReadAllBytes(uri));
        }
        protected async Task ReloadGeometryShader(GeometryShader geometryShader, string uri)
        {
            geometryShader.Reload(await ReadAllBytes(uri));
        }
        protected async Task<byte[]> ReadAllBytes(string uri)
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
