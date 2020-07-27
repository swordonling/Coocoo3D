using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Coocoo3D.RenderPipeline
{
    public abstract class RenderPipeline
    {
        public const int c_maxCameraPerRender = 2;

        public abstract GraphicsSignature GraphicsSignature { get; }

        public abstract void PrepareRenderData(RenderPipelineContext context);

        public abstract void BeforeRenderCameras(RenderPipelineContext context);

        public abstract void RenderCamera(RenderPipelineContext context, int cameraIndex);

        public abstract Task ReloadAssets(DeviceResources deviceResources);


        public volatile bool Ready;

        public virtual void TimeChange(double time, double deltaTime)
        {

        }

        protected async Task ReloadPixelShader(PixelShader pixelShader, DeviceResources deviceResources, string uri)
        {
            pixelShader.Reload(deviceResources, await ReadAllBytes(uri));
        }
        protected async Task ReloadVertexShader(VertexShader vertexShader, DeviceResources deviceResources, string uri)
        {
            vertexShader.Reload(deviceResources, await ReadAllBytes(uri));
        }
        protected async Task ReloadGeometryShader(GeometryShader geometryShader, DeviceResources deviceResources, string uri)
        {
            geometryShader.Reload(deviceResources, await ReadAllBytes(uri));
        }
        protected async Task<byte[]> ReadAllBytes(string uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            var stream = await file.OpenReadAsync();
            DataReader dataReader = new DataReader(stream);
            await dataReader.LoadAsync((uint)stream.Size);
            byte[] data = new byte[stream.Size];
            dataReader.ReadBytes(data);
            stream.Dispose();
            dataReader.Dispose();
            return data;
        }
    }
}
