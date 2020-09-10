using Coocoo3D.FileFormat;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Core
{
    public class RPShaderPack
    {
        public VertexShader VS = new VertexShader();
        public GeometryShader GS = new GeometryShader();
        public PixelShader PS = new PixelShader();
        public VertexShader VS1 = new VertexShader();
        public PObject POSkinning = new PObject();
        public PObject PODraw = new PObject();

        public Task LoadTask;
        public GraphicsObjectStatus Status;
    }
    public class MainCaches
    {
        public Dictionary<string, Texture2D> textureCaches = new Dictionary<string, Texture2D>();
        public Dictionary<string, RPShaderPack> RPShaderPackCaches = new Dictionary<string, RPShaderPack>();

        public Dictionary<string, PMXFormat> pmxCaches = new Dictionary<string, PMXFormat>();
    }
}
