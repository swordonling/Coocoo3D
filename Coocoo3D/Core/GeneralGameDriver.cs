using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Core
{
    public class GeneralGameDriver : GameDriver
    {
        public override bool Next(GameDriverContext context)
        {
            if (!(context.NeedRender || context.Playing))
            {
                return false;
            }
            if (DateTime.Now - LatestRenderTime < context.FrameInterval)
            {
                context.NeedRender = true;
                return false;
            }
            context.EnableDisplay = true;
            context.NeedRender = false;
            context.RequireResize = context.RequireResizeOuter;
            context.RequireResizeOuter = false;
            if (context.RequireResize)
                context.RequireInterruptRender = true;

            DateTime now = DateTime.Now;
            context.DeltaTime = Math.Clamp((now - LatestRenderTime).TotalSeconds * context.PlaySpeed, -0.17f, 0.17f);
            LatestRenderTime = now;
            if (context.Playing)
                context.PlayTime += context.DeltaTime;
            return true;
        }
    }
}
