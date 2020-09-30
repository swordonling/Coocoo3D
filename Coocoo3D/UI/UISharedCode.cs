﻿using System;
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
                    if (shaderPack.loadLocker.GetLocker())
                    {
                        string relativePath = storageFile.Name;
                        _ = Task.Run(async () =>
                        {
                            var task1 = shaderPack.Reload1(storageFolder, relativePath, appBody.RPAssetsManager, appBody.ProcessingList);
                            appBody.RequireRender();
                            if (await task1)
                            {

                            }
                            appBody.RequireRender();
                            shaderPack.loadLocker.FreeLocker();
                        });
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
                if (texturePack.loadLocker.GetLocker())
                {
                    _ = Task.Run(async () =>
                  {
                      if (await texturePack.ReloadTexture1(storageFolder, relativePath))
                          appBody.ProcessingList.AddObject(texturePack.texture2D);
                      appBody.RequireRender();
                      texturePack.loadLocker.FreeLocker();
                  });
                }
            }
        }
    }
}
