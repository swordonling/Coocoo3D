using Coocoo3D.FileFormat;
using Coocoo3D.ResourceWarp;
using Coocoo3D.Utility;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Coocoo3D.RenderPipeline
{
    public class MainCaches
    {
        public Dictionary<string, Texture2DPack> textureCaches = new Dictionary<string, Texture2DPack>();
        public Dictionary<string, RPShaderPack> RPShaderPackCaches = new Dictionary<string, RPShaderPack>();

        public Dictionary<string, ModelPack> ModelPackCaches = new Dictionary<string, ModelPack>();

        public SingleLocker textureTaskLocker;
        public SingleLocker shaderTaskLocker;
        public SingleLocker modelTaskLocker;
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
