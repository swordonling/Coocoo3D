using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
    public abstract class GameDriver
    {
        public abstract bool Next(ref GameDriverContext context);

        public DateTime LatestRenderTime = DateTime.Now;
    }
}
