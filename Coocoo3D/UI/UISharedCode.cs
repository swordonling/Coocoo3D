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
using System.Threading;

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
            appBody.RequireRender();

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
                    scene.RemoveSceneObject(lighting);
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
            entity.motionComponent.Reload(motionSet);
            appBody.RequireRender(true);
        }

        public static void LoadShaderForEntities1(Coocoo3DMain appBody, StorageFile storageFile, StorageFolder storageFolder, IList<MMD3DEntity> entities)
        {
            RPShaderPack shaderPack;
            lock (appBody.mainCaches.RPShaderPackCaches)
            {
                shaderPack = appBody.mainCaches.RPShaderPackCaches.GetOrCreate(storageFile.Path);
                if (shaderPack.Status != GraphicsObjectStatus.loaded)
                {
                    int testValue = Interlocked.Increment(ref shaderPack.taskLockCounter);
                    if (testValue == 1)
                    {
                        shaderPack.Status = GraphicsObjectStatus.loading;
                        shaderPack.POSkinning.Status = GraphicsObjectStatus.loading;
                        shaderPack.PODraw.Status = GraphicsObjectStatus.loading;
                        shaderPack.POParticleDraw.Status = GraphicsObjectStatus.loading;
                        shaderPack.CSParticle.Status = GraphicsObjectStatus.loading;

                        _ = Task.Run(async () =>
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
                                shaderPack.POParticleDraw.Status = GraphicsObjectStatus.error;
                                shaderPack.CSParticle.Status = GraphicsObjectStatus.error;
                                shaderPack.Status = GraphicsObjectStatus.error;
                                appBody.RequireRender();
                                Interlocked.Decrement(ref shaderPack.taskLockCounter);
                                return;
                            }
                            VertexShader vs0 = shaderPack.VS;
                            GeometryShader gs0 = shaderPack.GS;
                            VertexShader vs1 = shaderPack.VS1;
                            GeometryShader gs1 = shaderPack.GS1;
                            PixelShader ps1 = shaderPack.PS1;

                            VertexShader vs2 = shaderPack.VSParticle;
                            GeometryShader gs2 = shaderPack.GSParticle;
                            PixelShader ps2 = shaderPack.PSParticle;
                            ComputePO cs1 = shaderPack.CSParticle;


                            var RPAssetsManager = appBody.RPAssetsManager;
                            bool haveVS = vs0.CompileReload1(datas, "VS", ShaderMacro.DEFINE_COO_SURFACE);
                            bool haveGS = gs0.CompileReload1(datas, "GS", ShaderMacro.DEFINE_COO_SURFACE);
                            bool haveVS1 = vs1.CompileReload1(datas, "VS1", ShaderMacro.DEFINE_COO_SURFACE);
                            bool haveGS1 = gs1.CompileReload1(datas, "GS1", ShaderMacro.DEFINE_COO_SURFACE);
                            bool havePS1 = ps1.CompileReload1(datas, "PS1", ShaderMacro.DEFINE_COO_SURFACE);

                            bool haveVSParticle = vs2.CompileReload1(datas, "VSParticle", ShaderMacro.DEFINE_COO_SURFACE);
                            bool haveGSParticle = gs2.CompileReload1(datas, "GSParticle", ShaderMacro.DEFINE_COO_SURFACE);
                            bool havePSParticle = ps2.CompileReload1(datas, "PSParticle", ShaderMacro.DEFINE_COO_SURFACE);


                            bool haveCS1 = cs1.CompileReload1(appBody.deviceResources, RPAssetsManager.rootSignatureCompute, datas, "CSParticle", ShaderMacro.DEFINE_COO_PARTICLE);
                            if (haveVS || haveGS)
                            {
                                if (shaderPack.POSkinning.ReloadSkinning(appBody.deviceResources, RPAssetsManager.rootSignature,
                                    haveVS ? vs0 : RPAssetsManager.VSMMDSkinning2,
                                    haveGS ? gs0 : null))
                                    shaderPack.POSkinning.Status = GraphicsObjectStatus.loaded;
                                else
                                    shaderPack.POSkinning.Status = GraphicsObjectStatus.error;
                            }
                            else
                                shaderPack.POSkinning.Status = GraphicsObjectStatus.unload;
                            if (haveVS1 || haveGS1 || havePS1)
                            {
                                if (shaderPack.PODraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha,
                                    haveVS1 ? vs1 : RPAssetsManager.VSMMDTransform,
                                    haveGS1 ? gs1 : null,
                                    havePS1 ? ps1 : RPAssetsManager.PSMMD, appBody.RTFormat))
                                    shaderPack.PODraw.Status = GraphicsObjectStatus.loaded;
                                else
                                    shaderPack.PODraw.Status = GraphicsObjectStatus.error;
                            }
                            else
                                shaderPack.PODraw.Status = GraphicsObjectStatus.unload;
                            if (haveVSParticle || haveGSParticle || havePSParticle)
                            {
                                if (shaderPack.POParticleDraw.ReloadDrawing(appBody.deviceResources, RPAssetsManager.rootSignature, BlendState.alpha,
                                    haveVSParticle ? vs2 : RPAssetsManager.VSMMDTransform,
                                    haveGSParticle ? gs2 : null,
                                    havePSParticle ? ps2 : RPAssetsManager.PSMMD, appBody.RTFormat))
                                    shaderPack.POParticleDraw.Status = GraphicsObjectStatus.loaded;
                                else
                                    shaderPack.POParticleDraw.Status = GraphicsObjectStatus.error;
                            }
                            else
                                shaderPack.POParticleDraw.Status = GraphicsObjectStatus.unload;
                            if (haveCS1)
                                shaderPack.CSParticle.Status = GraphicsObjectStatus.loaded;
                            else
                                shaderPack.CSParticle.Status = GraphicsObjectStatus.unload;

                            shaderPack.Status = GraphicsObjectStatus.loaded;
                            appBody.RequireRender();
                            Interlocked.Decrement(ref shaderPack.taskLockCounter);
                        });

                    }
                    else
                    {
                        Interlocked.Decrement(ref shaderPack.taskLockCounter);
                    }

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
            List<string> relativePaths = new List<string>();
            foreach (var vTex in pmx.Textures)
            {
                string relativePath = vTex.TexturePath.Replace("//", "\\").Replace('/', '\\');
                string texPath = Path.Combine(storageFolder.Path, relativePath);
                paths.Add(texPath);
                relativePaths.Add(relativePath);
            }
            lock (appBody.mainCaches.textureCaches)
            {
                for (int i = 0; i < pmx.Textures.Count; i++)
                {
                    Texture2DPack tex = appBody.mainCaches.textureCaches.GetOrCreate(paths[i]);
                    LoadTexture(appBody, tex, storageFolder, relativePaths[i]);
                    textures.Add(tex.texture2D);
                }
            }
            return textures;
        }
        /// <summary>异步加载纹理</summary>
        public static void LoadTexture(Coocoo3DMain appBody, Texture2DPack texturePack, StorageFolder storageFolder, string relativePath)
        {
            if (texturePack.Status != GraphicsObjectStatus.loaded)
            {
                int testValue = Interlocked.Increment(ref texturePack.taskLockCounter);
                if (testValue == 1)
                    _ = Task.Run(async () =>
                  {
                      texturePack.Status = GraphicsObjectStatus.loading;
                      texturePack.texture2D.Status = GraphicsObjectStatus.loading;
                      if (Path.GetExtension(relativePath).Equals(".tga", StringComparison.OrdinalIgnoreCase))
                      {
                          relativePath = Path.ChangeExtension(relativePath, ".png");
                      }
                      IStorageItem storageItem = await storageFolder.TryGetItemAsync(relativePath);
                      if (storageItem is StorageFile texFile)
                      {
                          try
                          {
                              texturePack.texture2D.ReloadFromImage(appBody.wicFactory, await FileIO.ReadBufferAsync(texFile));
                              appBody.ProcessingList.AddObject(texturePack.texture2D);
                              texturePack.Status = GraphicsObjectStatus.loaded;
                              texturePack.texture2D.Status = GraphicsObjectStatus.loaded;
                          }
                          catch
                          {
                              texturePack.Status = GraphicsObjectStatus.error;
                              texturePack.texture2D.Status = GraphicsObjectStatus.error;
                          }
                      }
                      else
                      {
                          texturePack.Status = GraphicsObjectStatus.error;
                          texturePack.texture2D.Status = GraphicsObjectStatus.error;
                      }
                      appBody.RequireRender();
                      Interlocked.Decrement(ref texturePack.taskLockCounter);
                  });
                else
                {
                    Interlocked.Decrement(ref texturePack.taskLockCounter);
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
