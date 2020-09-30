using Coocoo3D.FileFormat;
using Coocoo3D.RenderPipeline;
using Coocoo3D.Utility;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Coocoo3D.Core
{
    public class RPShaderPack
    {
        public VertexShader VS = new VertexShader();
        public GeometryShader GS = new GeometryShader();
        public VertexShader VS1 = new VertexShader();
        public GeometryShader GS1 = new GeometryShader();
        public PixelShader PS1 = new PixelShader();

        public VertexShader VSParticle = new VertexShader();
        public GeometryShader GSParticle = new GeometryShader();
        public PixelShader PSParticle = new PixelShader();

        public PObject POSkinning = new PObject();
        public PObject PODraw = new PObject();
        public PObject POParticleDraw = new PObject();
        public ComputePO CSParticle = new ComputePO();

        public DateTimeOffset lastModifiedTime;
        public StorageFolder folder;
        public string relativePath;
        public SingleLocker loadLocker;

        public GraphicsObjectStatus Status;

        public void Mark(GraphicsObjectStatus status)
        {
            Status = status;
            POSkinning.Status = status;
            PODraw.Status = status;
            POParticleDraw.Status = status;
            CSParticle.Status = status;
        }

        public async Task<bool> Reload1(StorageFolder folder, string relativePath, RPAssetsManager RPAssetsManager, ProcessingList processingList)
        {
            this.relativePath = relativePath;
            this.folder = folder;
            Mark(GraphicsObjectStatus.loading);
            IStorageItem storageItem = await folder.TryGetItemAsync(relativePath);
            try
            {
                var attr = await storageItem.GetBasicPropertiesAsync();
                lastModifiedTime = attr.DateModified;
            }
            catch
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
            return await Reload(storageItem, RPAssetsManager, processingList);
        }

        public async Task<bool> Reload(IStorageItem storageItem, RPAssetsManager RPAssetsManager, ProcessingList processingList)
        {
            Mark(GraphicsObjectStatus.loading);

            if (!(storageItem is StorageFile file))
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
            Windows.Storage.Streams.IBuffer datas;
            try
            {
                datas = await FileIO.ReadBufferAsync(file);
            }
            catch
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
            VertexShader vs0 = VS;
            GeometryShader gs0 = GS;
            VertexShader vs1 = VS1;
            GeometryShader gs1 = GS1;
            PixelShader ps1 = PS1;

            VertexShader vs2 = VSParticle;
            GeometryShader gs2 = GSParticle;
            PixelShader ps2 = PSParticle;
            ComputePO cs1 = CSParticle;


            bool haveVS = vs0.CompileReload1(datas, "VS", ShaderMacro.DEFINE_COO_SURFACE);
            bool haveGS = gs0.CompileReload1(datas, "GS", ShaderMacro.DEFINE_COO_SURFACE);
            bool haveVS1 = vs1.CompileReload1(datas, "VS1", ShaderMacro.DEFINE_COO_SURFACE);
            bool haveGS1 = gs1.CompileReload1(datas, "GS1", ShaderMacro.DEFINE_COO_SURFACE);
            bool havePS1 = ps1.CompileReload1(datas, "PS1", ShaderMacro.DEFINE_COO_SURFACE);

            bool haveVSParticle = vs2.CompileReload1(datas, "VSParticle", ShaderMacro.DEFINE_COO_SURFACE);
            bool haveGSParticle = gs2.CompileReload1(datas, "GSParticle", ShaderMacro.DEFINE_COO_SURFACE);
            bool havePSParticle = ps2.CompileReload1(datas, "PSParticle", ShaderMacro.DEFINE_COO_SURFACE);


            bool haveCS1 = cs1.CompileReload1(datas, "CSParticle", ShaderMacro.DEFINE_COO_PARTICLE);
            if (haveVS || haveGS)
            {
                //if (shaderPack.POSkinning.ReloadSkinning(appBody.deviceResources, RPAssetsManager.rootSignature,
                //    haveVS ? vs0 : RPAssetsManager.VSMMDSkinning2,
                //    haveGS ? gs0 : null))
                //    shaderPack.POSkinning.Status = GraphicsObjectStatus.loaded;
                //else
                //    shaderPack.POSkinning.Status = GraphicsObjectStatus.error;
                POSkinning.ReloadSkinning(
                    haveVS ? vs0 : RPAssetsManager.VSMMDSkinning2,
                    haveGS ? gs0 : null);
                processingList.RS(POSkinning, 0);
            }
            else
                POSkinning.Status = GraphicsObjectStatus.unload;
            if (haveVS1 || haveGS1 || havePS1)
            {
                //if (shaderPack.PODraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha,
                //    haveVS1 ? vs1 : RPAssetsManager.VSMMDTransform,
                //    haveGS1 ? gs1 : null,
                //    havePS1 ? ps1 : RPAssetsManager.PSMMD, appBody.RTFormat))
                //    shaderPack.PODraw.Status = GraphicsObjectStatus.loaded;
                //else
                //    shaderPack.PODraw.Status = GraphicsObjectStatus.error;
                PODraw.ReloadDrawing(BlendState.alpha,
                    haveVS1 ? vs1 : RPAssetsManager.VSMMDTransform,
                    haveGS1 ? gs1 : null,
                    havePS1 ? ps1 : RPAssetsManager.PSMMD, RPAssetsManager.RTFormat);
                processingList.RS(PODraw, 0);
            }
            else
                PODraw.Status = GraphicsObjectStatus.unload;
            if (haveVSParticle || haveGSParticle || havePSParticle)
            {
                //if (shaderPack.POParticleDraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha,
                //    haveVSParticle ? vs2 : RPAssetsManager.VSMMDTransform,
                //    haveGSParticle ? gs2 : null,
                //    havePSParticle ? ps2 : RPAssetsManager.PSMMD, appBody.RTFormat))
                //    shaderPack.POParticleDraw.Status = GraphicsObjectStatus.loaded;
                //else
                //    shaderPack.POParticleDraw.Status = GraphicsObjectStatus.error;
                POParticleDraw.ReloadDrawing(BlendState.alpha,
                    haveVSParticle ? vs2 : RPAssetsManager.VSMMDTransform,
                    haveGSParticle ? gs2 : null,
                    havePSParticle ? ps2 : RPAssetsManager.PSMMD, RPAssetsManager.RTFormat);
                processingList.RS(POParticleDraw, 0);
            }
            else
                POParticleDraw.Status = GraphicsObjectStatus.unload;
            if (haveCS1)
                processingList.RS(CSParticle, 0);
            else
                CSParticle.Status = GraphicsObjectStatus.unload;

            Status = GraphicsObjectStatus.loaded;
            return true;
        }
    }
    public class Texture2DPack
    {
        public Texture2D texture2D = new Texture2D();

        public DateTimeOffset lastModifiedTime;
        public StorageFolder folder;
        public string relativePath;
        public SingleLocker loadLocker;

        public GraphicsObjectStatus Status;
        public void Mark(GraphicsObjectStatus status)
        {
            Status = status;
            texture2D.Status = status;
        }

        public async Task<bool> ReloadTexture1(StorageFolder folder, string relativePath)
        {
            this.relativePath = relativePath;
            this.folder = folder;
            Mark(GraphicsObjectStatus.loading);
            IStorageItem storageItem = await folder.TryGetItemAsync(relativePath);
            try
            {
                var attr = await storageItem.GetBasicPropertiesAsync();
                lastModifiedTime = attr.DateModified;
            }
            catch
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
            return await ReloadTexture(storageItem);
        }

        public async Task<bool> ReloadTexture(IStorageItem storageItem)
        {
            Mark(GraphicsObjectStatus.loading);
            if (!(storageItem is StorageFile texFile))
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }

            try
            {
                texture2D.ReloadFromImage(await FileIO.ReadBufferAsync(texFile));
                Mark(GraphicsObjectStatus.loaded);
                return true;
            }
            catch
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
        }
    }
    public class MainCaches
    {
        public Dictionary<string, Texture2DPack> textureCaches = new Dictionary<string, Texture2DPack>();
        public Dictionary<string, RPShaderPack> RPShaderPackCaches = new Dictionary<string, RPShaderPack>();

        public Dictionary<string, PMXFormat> pmxCaches = new Dictionary<string, PMXFormat>();

        public SingleLocker textureTaskLocker;
        public SingleLocker shaderTaskLocker;
        public void ReloadTextures(ProcessingList processingList, Action _RequireRender)
        {
            if (textureTaskLocker.GetLocker())
            {
                Task.Run(async () =>
                {
                    List<Texture2DPack> packs = new List<Texture2DPack>();
                    lock (textureCaches)
                        foreach (var texturePack in textureCaches.Values)
                            if (texturePack.Status == GraphicsObjectStatus.loaded || texturePack.Status == GraphicsObjectStatus.error)
                                packs.Add(texturePack);

                    for (int i = 0; i < packs.Count; i++)
                    {
                        var tex = packs[i];
                        if (tex.loadLocker.GetLocker())
                        {
                            try
                            {
                                var file = await tex.folder.GetFileAsync(tex.relativePath);
                                var attr = await file.GetBasicPropertiesAsync();
                                if (attr.DateModified != tex.lastModifiedTime || tex.Status == GraphicsObjectStatus.error)
                                {
                                    tex.lastModifiedTime = attr.DateModified;
                                    tex.Mark(GraphicsObjectStatus.loading);
                                    _RequireRender();
                                    _ = Task.Run(async () =>
                                    {
                                        if (await tex.ReloadTexture(file))
                                            processingList.AddObject(tex.texture2D);
                                        _RequireRender();
                                        tex.loadLocker.FreeLocker();
                                    });
                                }
                                else
                                    tex.loadLocker.FreeLocker();
                            }
                            catch
                            {
                                tex.Mark(GraphicsObjectStatus.error);
                                _RequireRender();
                                tex.loadLocker.FreeLocker();
                            }
                        }
                    }

                    textureTaskLocker.FreeLocker();
                });
                _RequireRender();
            }
        }

        public void ReloadShaders(ProcessingList processingList, RPAssetsManager RPAssetsManager, Action _RequireRender)
        {

            if (shaderTaskLocker.GetLocker())
            {
                Task.Run(async () =>
                {
                    List<RPShaderPack> packs = new List<RPShaderPack>();
                    lock (RPShaderPackCaches)
                        foreach (var shaderPack in RPShaderPackCaches.Values)
                            if (shaderPack.Status == GraphicsObjectStatus.loaded || shaderPack.Status == GraphicsObjectStatus.error)
                                packs.Add(shaderPack);

                    for (int i = 0; i < packs.Count; i++)
                    {
                        var shaderPack = packs[i];
                        if (shaderPack.loadLocker.GetLocker())
                        {
                            try
                            {
                                var file = await shaderPack.folder.GetFileAsync(shaderPack.relativePath);
                                var attr = await file.GetBasicPropertiesAsync();
                                if (attr.DateModified != shaderPack.lastModifiedTime || shaderPack.Status == GraphicsObjectStatus.error)
                                {
                                    shaderPack.lastModifiedTime = attr.DateModified;
                                    shaderPack.Mark(GraphicsObjectStatus.loading);
                                    _RequireRender();
                                    _ = Task.Run(async () =>
                                    {
                                        if (await shaderPack.Reload(file, RPAssetsManager, processingList))
                                        {

                                        }
                                        _RequireRender();
                                        shaderPack.loadLocker.FreeLocker();
                                    });
                                }
                                else
                                    shaderPack.loadLocker.FreeLocker();
                            }
                            catch
                            {
                                shaderPack.Mark(GraphicsObjectStatus.error);
                                _RequireRender();
                                shaderPack.loadLocker.FreeLocker();
                            }
                        }
                    }
                    shaderTaskLocker.FreeLocker();
                });
            }
        }
    }
}
