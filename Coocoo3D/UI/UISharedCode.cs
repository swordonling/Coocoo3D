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
            entity.Reload2(appBody.deviceResources, appBody.mainCaches, pmx);
            //entity.rendererComponent.texs = await LoadTextureForModel(appBody, storageFolder, pmx);
            entity.rendererComponent.texs = GetTextureListForModel(appBody, storageFolder, pmx);
            scene.AddSceneObject(entity);
            LoadModelTextures(appBody, storageFolder, pmx, entity.rendererComponent.texs);
            appBody.RequireRender();

        }

        public static void Play(Coocoo3DMain appBody)
        {
            appBody.Playing = true;
            appBody.PlaySpeed = 1.0f;
            appBody.LatestRenderTime = DateTime.Now - appBody.FrameInterval;
            appBody.RequireRender();
        }
        public static void Pause(Coocoo3DMain appBody)
        {
            appBody.Playing = false;
            appBody.ForceAudioAsync();
        }
        public static void Stop(Coocoo3DMain appBody)
        {
            appBody.Playing = false;
            appBody.ForceAudioAsync();
            appBody.PlayTime = 0;
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
                    //scene.Entities.Remove(entity);
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

        public static async Task LoadShaderForEntities(Coocoo3DMain appBody, StorageFile storageFile, StorageFolder storageFolder, IList<MMD3DEntity> entities)
        {
            PObject pObject = null;
            lock (appBody.mainCaches.pObjectCaches)
            {
                pObject = appBody.mainCaches.pObjectCaches.GetOrCreate(storageFile.Path);
                if (pObject.LoadTask == null && pObject.Status != GraphicsObjectStatus.loaded)
                {
                    pObject.Status = GraphicsObjectStatus.loading;
                    pObject.LoadTask = Task.Run(async () =>
                    {
                        try
                        {
                            CCShaderFormat ccShader = CCShaderFormat.Load((await storageFile.OpenReadAsync()).AsStreamForRead());

                            IStorageItem storageItem1 = null;
                            IStorageItem storageItem2 = null;
                            IStorageItem storageItem3 = null;
                            IStorageItem storageItem4 = null;
                            if (ccShader.CSPath != null)
                                storageItem1 = await storageFolder.TryGetItemAsync(ccShader.CSPath);
                            if (ccShader.VSPath != null)
                                storageItem2 = await storageFolder.TryGetItemAsync(ccShader.VSPath);
                            if (ccShader.GSPath != null)
                                storageItem3 = await storageFolder.TryGetItemAsync(ccShader.GSPath);
                            if (ccShader.PSPath != null)
                                storageItem4 = await storageFolder.TryGetItemAsync(ccShader.PSPath);


                            VertexShader vertexShader = null;
                            GeometryShader geometryShader = null;
                            PixelShader pixelShader = null;
                            DeviceResources deviceResources = appBody.deviceResources;
                            if (storageItem2 is StorageFile storageFile2)
                            {
                                vertexShader = VertexShader.CompileLoad(deviceResources, await ReadAllBytes(storageFile2));
                            }
                            if (storageItem3 is StorageFile storageFile3)
                            {
                                geometryShader = GeometryShader.CompileLoad(deviceResources, await ReadAllBytes(storageFile3));
                            }
                            if (storageItem4 is StorageFile storageFile4)
                            {
                                pixelShader = PixelShader.CompileLoad(deviceResources, await ReadAllBytes(storageFile4));
                            }

                            if (vertexShader != null && pixelShader != null)
                            {
                                pObject.Reload(deviceResources, appBody.CurrentRenderPipeline.GraphicsSignature, PObjectType.mmd, vertexShader, geometryShader, pixelShader);
                                pObject.Status = GraphicsObjectStatus.loaded;
                            }
                            else
                            {
                                pObject.Status = GraphicsObjectStatus.error;
                            }
                            pObject.LoadTask = null;
                        }
                        catch
                        {
                            pObject.Status = GraphicsObjectStatus.error;
                            pObject.LoadTask = null;
                        }
                    });
                }
            }
            foreach (var entity in entities)
            {
                entity.rendererComponent.pObject = pObject;
            }
            appBody.RequireRender();
            if (pObject.Status != GraphicsObjectStatus.loaded && pObject.LoadTask != null) await (pObject.LoadTask as Task);
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
                                Stream texStream = (await f1.OpenReadAsync()).AsStreamForRead();
                                byte[] texBytes = new byte[texStream.Length];
                                texStream.Read(texBytes, 0, (int)texStream.Length);
                                texStream.Dispose();
                                tex.ReloadFromImage1(appBody.deviceResources, texBytes);
                                appBody.mainCaches.AddTextureToLoadList(tex);
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
