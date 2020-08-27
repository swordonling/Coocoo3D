using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Coocoo3D.Core
{
    public class RecorderGameDriver : GameDriver
    {
        const int c_frameCount = 3;
        public override bool Next(ref GameDriverContext context)
        {
            context.NeedRender = false;
            DateTime now = DateTime.Now;
            LatestRenderTime = now;

            if (switchEffect)
            {
                switchEffect = false;
                context.Playing = true;
                context.PlaySpeed = 2.0f;
                context.PlayTime = 0.0f;
                ref RecordSettings recordSettings = ref context.recordSettings;
                float logicSizeScale = context.DeviceResources.GetDpi() / 96.0f;
                context.NewSize = new Windows.Foundation.Size(recordSettings.Width / logicSizeScale, recordSettings.Height / logicSizeScale);
                context.AspectRatio = (float)recordSettings.Width / (float)recordSettings.Height;
                context.RequireResize = true;
                ReadBackTexture2D.Reload(recordSettings.Width, recordSettings.Height, 4);
                context.ProcessingList.AddObject(ReadBackTexture2D);
                context.RequireInterruptRender = true;
                context.RequireResetPhysics = true;
                StartTime = recordSettings.StartTime;
                StopTime = recordSettings.StopTime;
                RenderCount = 0;
                RecordCount = 0;
                FrameIntervalF = 1 / context.recordSettings.FPS;
            }
            else
            {
            }
            context.deltaTime = FrameIntervalF;
            context.PlayTime = FrameIntervalF * RenderCount;
            RenderCount++;
            return true;
        }
        class Pack1
        {
            public ReadBackTexture2D ReadBackTexture2D;
            public int index;
            public int renderIndex;
            public WICFactory WICFactory;
            public Task taskX;
            public StorageFolder saveFolder;
            public async Task task1()
            {
                var bytes = ReadBackTexture2D.EncodePNG(WICFactory, index);
                StorageFile file = await saveFolder.CreateFileAsync(string.Format("{0}.png", renderIndex), CreationCollisionOption.ReplaceExisting);
                var stream = await file.OpenStreamForWriteAsync();
                stream.Write(bytes, 0, bytes.Length);
                await stream.FlushAsync();
                stream.Close();
            }
        }
        Pack1[] packs = new Pack1[c_frameCount];
        public override void AfterRender(GraphicsContext graphicsContext, ref GameDriverContext context)
        {
            if (context.PlayTime >= StartTime && (RenderCount - 3) * FrameIntervalF <= StopTime)
            {
                int index1 = RecordCount % c_frameCount;
                graphicsContext.CopyBackBuffer(ReadBackTexture2D, index1);
                if (RecordCount > 2)
                {
                    ReadBackTexture2D.GetDataTolocal(index1);
                    if (packs[index1] == null)
                        packs[index1] = new Pack1() { ReadBackTexture2D = ReadBackTexture2D, index = index1, WICFactory = context.WICFactory, saveFolder = saveFolder };
                    else if (!packs[index1].taskX.IsCompleted)
                    {
                        packs[index1].taskX.Wait();
                    }
                    packs[index1].renderIndex = RecordCount - 3;
                    packs[index1].taskX = Task.Run(packs[index1].task1);
                }
                RecordCount++;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (packs[i] != null && !packs[i].taskX.IsCompleted)
                    {
                        packs[i].taskX.Wait();
                        packs[i] = null;
                    }
                }
            }
        }
        public float StartTime;
        public float StopTime;

        public float FrameIntervalF = 1 / 60.0f;
        public int RecordCount = 0;
        public int RenderCount = 0;
        bool switchEffect;
        public ReadBackTexture2D ReadBackTexture2D = new ReadBackTexture2D();
        public StorageFolder saveFolder;
        public void SwitchEffect()
        {
            switchEffect = true;
        }
    }
}
