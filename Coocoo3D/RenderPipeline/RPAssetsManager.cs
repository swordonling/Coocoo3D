﻿using Coocoo3D.Present;
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
        public GraphicsSignature rootSignatureSkinning = new GraphicsSignature();
        public GraphicsSignature rootSignaturePostProcess = new GraphicsSignature();
        public GraphicsSignature rootSignatureCompute = new GraphicsSignature();
        public VertexShader VSMMDSkinning2 = new VertexShader();
        public VertexShader VSMMDTransform = new VertexShader();
        public VertexShader VSSkyBox = new VertexShader();
        public VertexShader VSPostProcess = new VertexShader();
        public VertexShader VSWidgetUI1 = new VertexShader();
        public PixelShader PSMMD = new PixelShader();
        public PixelShader PSMMD_DisneyBrdf = new PixelShader();
        public PixelShader PSMMD_Toon1 = new PixelShader();
        public PixelShader PSMMDLoading = new PixelShader();
        public PixelShader PSMMDError = new PixelShader();
        public PixelShader PSMMDAlphaClip = new PixelShader();
        public PixelShader PSMMDAlphaClip1 = new PixelShader();
        public PixelShader PSSkyBox = new PixelShader();
        public PixelShader PSPostProcess = new PixelShader();
        public PixelShader PSWidgetUI1 = new PixelShader();
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
        public PObject PObjectWidgetUI1 = new PObject();
        public DxgiFormat RTFormat;
        public bool Ready;
        public void Reload(DeviceResources deviceResources)
        {
            rootSignature.ReloadMMD(deviceResources);
            rootSignatureSkinning.ReloadSkinning(deviceResources);
            rootSignaturePostProcess.Reload(deviceResources, new GraphicSignatureDesc[] { GSD.CBV, GSD.SRVTable, GSD.SRVTable });
            rootSignatureCompute.ReloadCompute(deviceResources, new GraphicSignatureDesc[] { GSD.CBV, GSD.CBV, GSD.CBV, GSD.SRV, GSD.UAV, GSD.UAV });
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

            await ReloadVertexShader(VSWidgetUI1, "ms-appx:///Coocoo3DGraphics/VSWidgetUI1.cso");
            await ReloadPixelShader(PSWidgetUI1, "ms-appx:///Coocoo3DGraphics/PSWidgetUI1.cso");
        }
        public void ChangeRenderTargetFormat(DeviceResources deviceResources, ProcessingList uploadProcess, DxgiFormat format, DxgiFormat backBufferFormat)
        {
            RTFormat = format;
            PObjectMMDSkinning.ReloadSkinning(VSMMDSkinning2, null);
            uploadProcess.UL(PObjectMMDSkinning, 1);

            PObjectMMD.ReloadDrawing(BlendState.alpha, VSMMDTransform, null, PSMMD, format);
            PObjectMMD_DisneyBrdf.ReloadDrawing(BlendState.alpha, VSMMDTransform, null, PSMMD_DisneyBrdf, format);
            PObjectMMD_Toon1.ReloadDrawing(BlendState.alpha, VSMMDTransform, null, PSMMD_Toon1, format);
            PObjectMMDLoading.ReloadDrawing(BlendState.alpha, VSMMDTransform, null, PSMMDLoading, format);
            PObjectMMDError.ReloadDrawing(BlendState.alpha, VSMMDTransform, null, PSMMDError, format);
            uploadProcess.UL(PObjectMMD, 0);
            uploadProcess.UL(PObjectMMD_DisneyBrdf, 0);
            uploadProcess.UL(PObjectMMD_Toon1, 0);
            uploadProcess.UL(PObjectMMDLoading, 0);
            uploadProcess.UL(PObjectMMDError, 0);

            PObjectMMDShadowDepth.ReloadDepthOnly(VSMMDTransform, PSMMDAlphaClip, 2500);
            PObjectMMDDepth.ReloadDepthOnly(VSMMDTransform, PSMMDAlphaClip1, 0);
            uploadProcess.UL(PObjectMMDShadowDepth, 0);
            uploadProcess.UL(PObjectMMDDepth, 0);


            PObjectSkyBox.Reload(deviceResources, rootSignature, eInputLayout.postProcess, BlendState.none, VSSkyBox, null, PSSkyBox, format);
            PObjectPostProcess.Reload(deviceResources, rootSignaturePostProcess, eInputLayout.postProcess, BlendState.none, VSPostProcess, null, PSPostProcess, backBufferFormat);
            PObjectWidgetUI1.Reload(deviceResources, rootSignaturePostProcess, eInputLayout.postProcess, BlendState.alpha, VSWidgetUI1, null, PSWidgetUI1, backBufferFormat);
            Ready = true;
        }
        protected async Task ReloadPixelShader(PixelShader pixelShader, string uri)
        {
            pixelShader.Reload(await ReadFile(uri));
        }
        protected async Task ReloadVertexShader(VertexShader vertexShader, string uri)
        {
            vertexShader.Reload(await ReadFile(uri));
        }
        protected async Task ReloadGeometryShader(GeometryShader geometryShader, string uri)
        {
            geometryShader.Reload(await ReadFile(uri));
        }
        protected async Task ReloadComputeShader(ComputePO computeShader, string uri)
        {
            computeShader.Reload(await ReadFile(uri));
        }
        protected async Task<IBuffer> ReadFile(string uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            return await FileIO.ReadBufferAsync(file);
        }

        #region UploadProceess
        public void _DealStep3(DeviceResources deviceResources, ProcessingList uploadProcess)
        {
            foreach (var a in uploadProcess.pobjectLists[0])
            {
                a.Upload(deviceResources, rootSignature);
            }
            foreach (var a in uploadProcess.pobjectLists[1])
            {
                a.Upload(deviceResources, rootSignatureSkinning);
            }
            foreach (var a in uploadProcess.pobjectLists[2])
            {
                a.Upload(deviceResources, rootSignaturePostProcess);
            }
            foreach (var a in uploadProcess.computePObjectLists[0])
            {
                a.Upload(deviceResources, rootSignatureCompute);
            }
        }
        #endregion
    }
}
