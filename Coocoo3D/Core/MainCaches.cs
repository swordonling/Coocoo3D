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

        public VertexShader VSParticle = new VertexShader();
        public GeometryShader GSParticle = new GeometryShader();
        public PixelShader PSParticle = new PixelShader();

        public PObject POSkinning = new PObject();
        public PObject PODraw = new PObject();
        public PObject POParticleDraw = new PObject();
        public ComputePO CSParticle = new ComputePO();


        public Task LoadTask;
        public GraphicsObjectStatus Status;
    }
    public class MainCaches
    {
        public Dictionary<string, Texture2D> textureCaches = new Dictionary<string, Texture2D>();
        public Dictionary<string, RPShaderPack> RPShaderPackCaches = new Dictionary<string, RPShaderPack>();

        public Dictionary<string, PMXFormat> pmxCaches = new Dictionary<string, PMXFormat>();

        public void ResetShaderCache()
        {
            lock (RPShaderPackCaches)
            {
                foreach (var shaderPack in RPShaderPackCaches.Values)
                {
                    shaderPack.Status = GraphicsObjectStatus.unload;
                }
            }
        }
    }
}
