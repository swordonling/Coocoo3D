using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Coocoo3D.FileFormat;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System.Numerics;
using Windows.Storage;
using Coocoo3D.Utility;
using Coocoo3D.Core;
using Windows.Storage.Streams;

namespace Coocoo3D.UI
{
    public static class UISharedCode
    {
        public static async Task LoadEntityIntoScene(Coocoo3DMain appBody, Scene scene, StorageFile pmxFile, StorageFolder storageFolder)
        {
            string pmxPath = pmxFile.Path;
            PMXFormat pmx = null;
            lock (appBody.mainCaches.pmxCaches)
            {
                pmx = appBody.mainCaches.pmxCaches.GetOrCreate(pmxPath);
                if (pmx.LoadTask == null && !pmx.Ready)
                {
                    pmx.LoadTask = Task.Run(async () =>
                    {
                        BinaryReader reader = new BinaryReader((await pmxFile.OpenReadAsync()).AsStreamForRead());
                        pmx.Reload(reader);
                        pmx.Reload2();
                        reader.Dispose();
                        pmx.Ready = true;
                        pmx.LoadTask = null;
                    });
                }
            }
            if (!pmx.Ready && pmx.LoadTask != null) await pmx.LoadTask;
            MMD3DEntity entity = new MMD3DEntity();
            entity.Reload2(appBody.ProcessingList, pmx);
            entity.rendererComponent.textures = GetTextureListForModel(appBody, storageFolder, pmx);
            scene.AddSceneObject(entity);
            _ = LoadModelTextures(appBody, storageFolder, pmx, entity.rendererComponent.textures);
            appBody.RequireRender();

        }

        public static void Play(Coocoo3DMain appBody)
        {
            appBody.GameDriverContext.Playing = true;
            appBody.GameDriverContext.PlaySpeed = 1.0f;
            appBody.LatestRenderTime = DateTime.Now - appBody.GameDriverContext.FrameInterval;
            appBody.RequireRender();
        }
        public static void Pause(Coocoo3DMain appBody)
        {
            appBody.GameDriverContext.Playing = false;
        }
        public static void Stop(Coocoo3DMain appBody)
        {
            if (appBody.Recording)
            {
                appBody.GameDriver = appBody._GeneralGameDriver;
                appBody.Recording = false;
            }
            appBody.GameDriverContext.Playing = false;
            appBody.GameDriverContext.PlayTime = 0;
            appBody.RequireRender(true);
        }
        public static void NewLighting(Coocoo3DMain appBody)
        {
            Lighting lighting = new Lighting();
            lighting.Name = "光源";
            lighting.Color = new Vector4(1, 1, 1, 1);
            lighting.Rotation = new Vector3(1.570796326794f, 0, 0);
            appBody.CurrentScene.AddSceneObject(lighting);
            appBody.RequireRender();
        }
        public static void RemoveSceneObject(Coocoo3DMain appBody, Scene scene, ISceneObject sceneObject)
        {
            if (scene.sceneObjects.Remove(sceneObject))
            {
                if (sceneObject is MMD3DEntity entity)
                {
                    scene.RemoveSceneObject(entity);
                }
                else if (sceneObject is Lighting lighting)
                {
                    lock (appBody.CurrentScene)
                    {
                        scene.Lightings.Remove(lighting);
                    }
                }
            }
            appBody.RequireRender();
        }
        public static async Task OpenResourceFolder(Coocoo3DMain appBody)
        {
            FolderPicker folderPicker = new FolderPicker()
            {
                FileTypeFilter =
                {
                    "*"
                },
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.Thumbnail,
                SettingsIdentifier = "ResourceFolder",
            };
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            appBody.OpenedStorageFolderChange(folder);
        }

        public static async Task LoadVMD(Coocoo3DMain appBody, StorageFile storageFile, MMD3DEntity entity)
        {
            BinaryReader reader = new BinaryReader((await storageFile.OpenReadAsync()).AsStreamForRead());
            VMDFormat motionSet = VMDFormat.Load(reader);
            lock (appBody.deviceResources)
            {
                entity.motionComponent.Reload(motionSet);
            }
            appBody.RequireRender(true);
        }

        public static void LoadShaderForEntities1(Coocoo3DMain appBody, StorageFile storageFile, StorageFolder storageFolder, IList<MMD3DEntity> entities)
        {
            RPShaderPack shaderPack = new RPShaderPack();
            lock (appBody.mainCaches.RPShaderPackCaches)
            {
                shaderPack = appBody.mainCaches.RPShaderPackCaches.GetOrCreate(storageFile.Path);
                if (shaderPack.LoadTask == null && shaderPack.Status != GraphicsObjectStatus.loaded)
                {
                    shaderPack.Status = GraphicsObjectStatus.loading;
                    shaderPack.POSkinning.Status = GraphicsObjectStatus.loading;
                    shaderPack.PODraw.Status = GraphicsObjectStatus.loading;
                    shaderPack.POParticleDraw.Status = GraphicsObjectStatus.loading;
                    shaderPack.CSParticle.Status = GraphicsObjectStatus.loading;

                    shaderPack.LoadTask = Task.Run(async () =>
                    {
                        byte[] datas = null;
                        try
                        {
                            datas = await ReadAllBytes(storageFile);
                        }
                        catch
                        {
                            shaderPack.POSkinning.Status = GraphicsObjectStatus.error;
                            shaderPack.PODraw.Status = GraphicsObjectStatus.error;
                            shaderPack.Status = GraphicsObjectStatus.error;
                            shaderPack.LoadTask = null;
                            appBody.RequireRender();
                            return;
                        }
                        VertexShader vs0 = shaderPack.VS;
                        GeometryShader gs0 = shaderPack.GS;
                        PixelShader ps0 = shaderPack.PS;
                        VertexShader vs1 = shaderPack.VS1;

                        VertexShader vs2 = shaderPack.VSParticle;
                        GeometryShader gs2 = shaderPack.GSParticle;
                        PixelShader ps2 = shaderPack.PSParticle;
                        ComputePO cs1 = shaderPack.CSParticle;


                        var RPAssetsManager = appBody.RPAssetsManager;
                        bool haveVs = vs0.CompileReload1(datas, "VS", ShaderMacro.DEFINE_COO_SURFACE);
                        bool haveGs = gs0.CompileReload1(datas, "GS", ShaderMacro.DEFINE_COO_SURFACE);
                        bool havePs = ps0.CompileReload1(datas, "PS", ShaderMacro.DEFINE_COO_SURFACE);
                        bool haveVS1 = vs1.CompileReload1(datas, "VS1", ShaderMacro.DEFINE_COO_SURFACE);
                        bool haveVSParticle = vs2.CompileReload1(datas, "VSParticle", ShaderMacro.DEFINE_COO_SURFACE);
                        bool haveGSParticle = gs2.CompileReload1(datas, "GSParticle", ShaderMacro.DEFINE_COO_SURFACE);
                        bool havePSParticle = ps2.CompileReload1(datas, "PSParticle", ShaderMacro.DEFINE_COO_SURFACE);
                        bool haveCS1 = cs1.CompileReload1(appBody.deviceResources, RPAssetsManager.rootSignatureCompute, datas, "CSParticle", ShaderMacro.DEFINE_COO_PARTICLE);
                        if (haveVs || haveGs)
                        {
                            if (shaderPack.POSkinning.ReloadSkinning(appBody.deviceResources, RPAssetsManager.rootSignature, haveVs ? vs0 : RPAssetsManager.VSMMDSkinning2, haveGs ? gs0 : null))
                                shaderPack.POSkinning.Status = GraphicsObjectStatus.loaded;
                            else
                                shaderPack.POSkinning.Status = GraphicsObjectStatus.error;
                        }
                        else
                        {
                            shaderPack.POSkinning.Status = GraphicsObjectStatus.unload;
                        }
                        if (havePs || haveVS1)
                        {
                            if (shaderPack.PODraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha, haveVS1 ? vs1 : RPAssetsManager.VSMMDTransform, null, havePs ? ps0 : RPAssetsManager.PSMMD, appBody.RTFormat))
                                shaderPack.PODraw.Status = GraphicsObjectStatus.loaded;
                            else
                                shaderPack.PODraw.Status = GraphicsObjectStatus.error;
                        }
                        else
                        {
                            shaderPack.PODraw.Status = GraphicsObjectStatus.unload;
                        }
                        if (haveVSParticle || havePSParticle)
                        {
                            if (shaderPack.POParticleDraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha, haveVSParticle ? vs2 : RPAssetsManager.VSMMDTransform, null, havePSParticle ? ps2 : RPAssetsManager.PSMMD, appBody.RTFormat))
                                shaderPack.POParticleDraw.Status = GraphicsObjectStatus.loaded;
                            else
                                shaderPack.POParticleDraw.Status = GraphicsObjectStatus.error;
                        }
                        if (haveCS1)
                            shaderPack.CSParticle.Status = GraphicsObjectStatus.loaded;
                        shaderPack.Status = GraphicsObjectStatus.loaded;
                        shaderPack.LoadTask = null;
                        appBody.RequireRender();
                    });
                }
            }
            foreach (var entity in entities)
            {
                entity.rendererComponent.PODraw = shaderPack.PODraw;
                entity.rendererComponent.POSkinning = shaderPack.POSkinning;
                entity.rendererComponent.POParticleDraw = shaderPack.POParticleDraw;
                entity.rendererComponent.ParticleCompute = shaderPack.CSParticle;
            }
            appBody.RequireRender();
        }

        public static List<Texture2D> GetTextureListForModel(Coocoo3DMain appBody, StorageFolder storageFolder, PMXFormat pmx)
        {
            List<Texture2D> textures = new List<Texture2D>();
            List<string> paths = new List<string>();
            foreach (var vTex in pmx.Textures)
            {
                string relativePath = vTex.TexturePath.Replace("//", "\\").Replace('/', '\\');
                string texPath = Path.Combine(storageFolder.Path, relativePath);
                paths.Add(texPath);
            }
            lock (appBody.mainCaches.textureCaches)
            {
                for (int i = 0; i < pmx.Textures.Count; i++)
                {
                    Texture2D tex = appBody.mainCaches.textureCaches.GetOrCreate(paths[i]);
                    if (tex.Status != GraphicsObjectStatus.loaded) tex.Status = GraphicsObjectStatus.loading;
                    tex.Path = paths[i];
                    textures.Add(tex);
                }
            }
            return textures;
        }

        public static async Task LoadModelTextures(Coocoo3DMain appBody, StorageFolder storageFolder, PMXFormat pmx, List<Texture2D> textures)
        {
            for (int i = 0; i < textures.Count; i++)
            {
                Texture2D tex = textures[i];
                string relativePath = pmx.Textures[i].TexturePath.Replace("//", "\\").Replace('/', '\\');
                IStorageItem storageItem = await storageFolder.TryGetItemAsync(relativePath);

                if (storageItem is StorageFile texFile)
                {
                    if (tex.Status != GraphicsObjectStatus.loaded && tex.LoadTask == null)
                    {
                        tex.LoadTask = new Task(async () =>
                        {
                            async Task _LoadImage(StorageFile f1)
                            {
                                tex.ReloadFromImage(appBody.wicFactory, await FileIO.ReadBufferAsync(f1));
                                appBody.ProcessingList.AddObject(tex);
                                tex.Status = GraphicsObjectStatus.loaded;
                                appBody.RequireRender();
                            }
                            try
                            {
                                if (texFile.FileType.Equals(".tga", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    IStorageItem item1 = await storageFolder.TryGetItemAsync(relativePath.Replace(".tga", ".png"));
                                    if (item1 != null && item1 is StorageFile file)
                                    {
                                        await _LoadImage(file);
                                    }
                                    else
                                    {
                                        tex.Status = GraphicsObjectStatus.error;
                                        appBody.RequireRender();
                                    }
                                }
                                else
                                {
                                    await _LoadImage(texFile);
                                }
                            }
                            catch
                            {
                                tex.Status = GraphicsObjectStatus.error;
                                appBody.RequireRender();
                            }
                            tex.LoadTask = null;
                        });
                        tex.Status = GraphicsObjectStatus.loading;
                        (tex.LoadTask as Task).Start();
                    }
                }
                else
                {
                    tex.Status = GraphicsObjectStatus.error;
                    appBody.RequireRender();
                }
            }
        }

        private static async Task<byte[]> ReadAllBytes(StorageFile storageFile)
        {
            var stream = await storageFile.OpenReadAsync();
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
