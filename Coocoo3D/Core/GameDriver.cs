using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coocoo3DGraphics;

namespace Coocoo3D.Core
{
    public struct GameDriverContext
    {
        public volatile bool NeedRender;
        public bool Playing;
        public double PlayTime;
        public TimeSpan FrameInterval;
        public double deltaTime;
        public float PlaySpeed;
        public bool RequireResetPhysics;
        public DeviceResources DeviceResources;
        public ProcessingList ProcessingList;
        public bool RequireResize;
        public Windows.Foundation.Size NewSize;
        public float AspectRatio;
        public bool RequireInterruptRender;
        public WICFactory WICFactory;
    }
    public abstract class GameDriver
    {
        public abstract bool Next(ref GameDriverContext context);

        public virtual void AfterRender(GraphicsContext graphicsContext, ref GameDriverContext context)
        {

        }

        public DateTime LatestRenderTime = DateTime.Now;
    }
}
