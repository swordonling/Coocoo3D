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
        //public GraphicsSignature rootSignatureNTAO = new GraphicsSignature();
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public VertexShader VSSkyBox = new VertexShader();
        public VertexShader VSPostProcess = new VertexShader();
        //public VertexShader VSNTAO = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMD_DisneyBrdf = new PixelShader();
        public PixelShader PSMMD_Toon1 = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader PSMMDAlphaClip = new PixelShader();
        public PixelShader PSMMDAlphaClip1 = new PixelShader();
        public PixelShader PSSkyBox = new PixelShader();
        public PixelShader PSPostProcess = new PixelShader();
        //public PixelShader PSNTAO = new PixelShader();
        //public PixelShader PSNTAONormal = new PixelShader();
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
        //public PObject PObjectNTAO = new PObject();
        //public PObject PObjectNTAODrawNormal = new PObject();
        public DxgiFormat CurrentRenderTargetFormat;
        public bool Ready;
        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.ReloadMMD(deviceResources);
            rootSignaturePostProcess.Reload(deviceResources,new GraphicSignatureDesc[] { GSD.CBV, GSD.SRVTable, GSD.SRVTable });
            //rootSignatureNTAO.Reload(deviceResources, new GraphicSignatureDesc[] {GSD.CBV, GSD.SRV, GSD.SRV, GSD.SRVTable, GSD.SRVTable, });
        }
        public async Task ReloadAssets()
        {
            await ReloadVertexShader(VSMMDSkinning2, "ms-appx:///Coocoo3DGraphics/VSMMDSkinning2.cso");
            await ReloadVertexShader(VSMMDTransform, "ms-appx:///Coocoo3DGraphics/VSMMDTransform.cso");
            await ReloadVertexShader(VSSkyBox, "ms-appx:///Coocoo3DGraphics/VSSkyBox.cso");
            //await ReloadVertexShader(VSNTAO, "ms-appx:///Coocoo3DGraphics/VS_NTAO.cso");
            await ReloadPixelShader(PSMMD, "ms-appx:///Coocoo3DGraphics/PSMMD.cso");
            await ReloadPixelShader(PSMMD_DisneyBrdf, "ms-appx:///Coocoo3DGraphics/PSMMD_DisneyBRDF.cso");
            await ReloadPixelShader(PSMMD_Toon1, "ms-appx:///Coocoo3DGraphics/PSMMD_Toon1.cso");
            await ReloadPixelShader(PSMMDLoading, "ms-appx:///Coocoo3DGraphics/PSMMDLoading.cso");
            await ReloadPixelShader(PSMMDError, "ms-appx:///Coocoo3DGraphics/PSMMDError.cso");
            await ReloadPixelShader(PSMMDAlphaClip, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip.cso");
            await ReloadPixelShader(PSMMDAlphaClip1, "ms-appx:///Coocoo3DGraphics/PSMMDAlphaClip1.cso");
            await ReloadPixelShader(PSSkyBox, "ms-appx:///Coocoo3DGraphics/PSSkyBox.cso");
            //await ReloadPixelShader(PSNTAO, "ms-appx:///Coocoo3DGraphics/PS_NTAO_1.cso");
            //await ReloadPixelShader(PSNTAONormal, "ms-appx:///Coocoo3DGraphics/PS_NTAO_Normal.cso");


            await ReloadVertexShader(VSPostProcess, "ms-appx:///Coocoo3DGraphics/VSPostProcess.cso");
            await ReloadPixelShader(PSPostProcess, "ms-appx:///Coocoo3DGraphics/PSPostProcess.cso");
        }
        public void ChangeRenderTargetFormat(DeviceResources deviceResources, DxgiFormat format, DxgiFormat backBufferFormat)
        {
            CurrentRenderTargetFormat = format;
            PObjectMMDSkinning.ReloadSkinning(deviceResources, rootSignature, VSMMDSkinning2, null);

            PObjectMMD.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSMMD, format);
            PObjectMMD_DisneyBrdf.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSMMD_DisneyBrdf, format);
            PObjectMMD_Toon1.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSMMD_Toon1, format);
            PObjectMMDLoading.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSMMDLoading, format);
            PObjectMMDError.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSMMDError, format);
            //PObjectNTAODrawNormal.ReloadDrawing(deviceResources, rootSignature, VSMMDTransform, PSNTAONormal, format);

            PObjectSkyBox.Reload(deviceResources, rootSignature, PObjectType.postProcess, VSSkyBox, null, PSSkyBox, format);
            PObjectMMDShadowDepth.ReloadDepthOnly(deviceResources, rootSignature, VSMMDTransform, PSMMDAlphaClip, 10000);
            PObjectMMDDepth.ReloadDepthOnly(deviceResources, rootSignature, VSMMDTransform, PSMMDAlphaClip1, 0);
            //PObjectNTAO.ReloadNTAO(deviceResources, rootSignatureNTAO, VSNTAO, PSNTAO, format);

            PObjectPostProcess.Reload(deviceResources, rootSignaturePostProcess, PObjectType.postProcess, VSPostProcess, null, PSPostProcess, backBufferFormat);
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
