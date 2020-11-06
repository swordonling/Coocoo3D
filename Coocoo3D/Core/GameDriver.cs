using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coocoo3D.RenderPipeline;
using Coocoo3DGraphics;

namespace Coocoo3D.Core
{

    public struct RecordSettings
    {
        public float FPS;
        public float StartTime;
        public float StopTime;
        public int Width;
        public int Height;
    }
    public class GameDriverContext
    {
        public volatile bool NeedRender;
        public volatile bool NeedUpdateEntities;
        public volatile bool EnableDisplay;
        public bool Playing;
        public double PlayTime;
        public double DeltaTime;
        public TimeSpan FrameInterval;
        public float PlaySpeed;
        public volatile bool RequireResetPhysics;
        public bool NeedReloadModel;
        public DeviceResources DeviceResources;
        public ProcessingList ProcessingList;
        public bool RequireResize;
        public bool RequireResizeOuter;
        public Windows.Foundation.Size NewSize;
        public float AspectRatio;
        public bool RequireInterruptRender;
        public WICFactory WICFactory;
        public RecordSettings recordSettings;

        public void ReqireReloadModel()
        {
            NeedReloadModel = true;
            RequireInterruptRender = true;
            NeedRender = true;
        }

        public void RequireRender(bool updateEntities)
        {
            NeedUpdateEntities |= updateEntities;
            NeedRender = true;
        }

        public void RequireRender()
        {
            NeedRender = true;
        }
    }
    public abstract class GameDriver
    {
        public abstract bool Next(GameDriverContext context);

        public virtual void AfterRender(RenderPipelineContext  rpContext, GameDriverContext context)
        {

        }

        public DateTime LatestRenderTime = DateTime.Now;
    }
}
