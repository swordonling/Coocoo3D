using Coocoo3D.Utility;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Coocoo3D.ResourceWarp
{
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
                return await ReloadTexture(storageItem);
            }
            catch
            {
                Mark(GraphicsObjectStatus.error);
                return false;
            }
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
}
