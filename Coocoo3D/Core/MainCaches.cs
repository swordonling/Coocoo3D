using Coocoo3D.FileFormat;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Coocoo3D.Core
{
    public class RPShaderPack
    {
        public VertexShader VS = new VertexShader();
        public GeometryShader GS = new GeometryShader();
        public VertexShader VS1 = new VertexShader();
        public GeometryShader GS1 = new GeometryShader();
        public PixelShader PS1 = new PixelShader();

        public VertexShader VSParticle = new VertexShader();
        public GeometryShader GSParticle = new GeometryShader();
        public PixelShader PSParticle = new PixelShader();

        public PObject POSkinning = new PObject();
        public PObject PODraw = new PObject();
        public PObject POParticleDraw = new PObject();
        public ComputePO CSParticle = new ComputePO();

        public StorageFile file;
        public int taskLockCounter;
        public GraphicsObjectStatus Status;
    }
    public class Texture2DPack
    {
        public Texture2D texture2D = new Texture2D();

        public StorageFile file;
        public int taskLockCounter;

        public GraphicsObjectStatus Status;
    }
    public class MainCaches
    {
        public Dictionary<string, Texture2DPack> textureCaches = new Dictionary<string, Texture2DPack>();
        public Dictionary<string, RPShaderPack> RPShaderPackCaches = new Dictionary<string, RPShaderPack>();

        public Dictionary<string, PMXFormat> pmxCaches = new Dictionary<string, PMXFormat>();

        public void ResetTextureCache()
        {
            lock (textureCaches)
            {
                foreach (var texturePack in textureCaches.Values)
                {
                    texturePack.Status = GraphicsObjectStatus.unload;
                }
            }
        }

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
