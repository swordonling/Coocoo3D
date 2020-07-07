using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Coocoo3DGraphics;
using Coocoo3D.Present;
using Coocoo3D.Controls;
using Coocoo3D.Utility;
using Windows.Storage;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Coocoo3D.Core
{
    ///<summary>是整个应用程序的上下文</summary>
    public class Coocoo3DMain
    {
        public DeviceResources deviceResources = new DeviceResources();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public DefaultResources defaultResources = new DefaultResources();
        public MainCaches mainCaches = new MainCaches();

        public WorldViewer worldViewer;
        public MediaElement mediaElement;
        public Scene CurrentScene;

        private List<MMD3DEntity> Entities { get => CurrentScene.Entities; }
        private List<Lighting> Lightings { get => CurrentScene.Lightings; }
        private ObservableCollection<ISceneObject> sceneObjects { get => CurrentScene.sceneObjects; }
        public List<MMD3DEntity> SelectedEntities = new List<MMD3DEntity>();
        public List<Lighting> SelectedLighting = new List<Lighting>();


        public Camera camera = new Camera();
        //public WidgetRenderer widgetRenderer = new WidgetRenderer();
        public StorageFolder openedStorageFolder;
        public event EventHandler OpenedStorageFolderChanged;
        public void OpenedStorageFolderChange(StorageFolder storageFolder)
        {
            openedStorageFolder = storageFolder;
            OpenedStorageFolderChanged?.Invoke(this, null);
        }
        #region Time
        ThreadPoolTimer threadPoolTimer;
        volatile bool NeedRender = false;
        volatile bool NeedUpdateEntities = false;
        public DateTime LatestUserOperating = DateTime.Now;

        public float PlayTime;
        public float PlaySpeed = 1.0f;
        public bool Playing;
        public DateTime LatestRenderTime = DateTime.Now;
        public TimeSpan FrameInterval = TimeSpan.FromSeconds(1 / 120.0);
        public CoreDispatcher Dispatcher;
        public event EventHandler FrameUpdated;

        public int RenderCount = 0;
        private async void Tick(ThreadPoolTimer timer)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                FrameUpdated?.Invoke(this, null);
                ForceAudioAsync();
            });
        }
        #endregion
        public Settings settings = new Settings()
        {
            viewSelectedEntityBone = true,
            HighResolutionShadow = false,
            backgroundColor = new Vector4(0, 0.3f, 0.3f, 0.0f),
            ExtendShadowMapRange = 32
        };
        public float AspectRatio;
        IAsyncAction RenderLoop;
        public Coocoo3DMain()
        {
            _currentRenderPipeline = forwardRenderPipeline1;
            graphicsContext.Reload(deviceResources);

            CurrentScene = new Scene(this);
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(Tick, TimeSpan.FromSeconds(1 / 15.0));

            forwardRenderPipeline1.Reload(deviceResources);
            postProcess.Reload(deviceResources);
            defaultResources.LoadTask = Task.Run(async () =>
            {
                await defaultResources.ReloadDefalutResources(deviceResources, mainCaches);
                await forwardRenderPipeline1.ReloadAssets(deviceResources);
                forwardRenderPipeline1.ChangeRenderTargetFormat(deviceResources, RTFormat);
                await postProcess.ReloadAssets(deviceResources);
                //widgetRenderer.Init(mainCaches, defaultResources, mainCaches.textureCaches);
            });
            RenderLoop = ThreadPool.RunAsync((IAsyncAction action) =>
              {
                  while (action.Status == AsyncStatus.Started)
                  {
                      DateTime now = DateTime.Now;
                      if (now - LatestRenderTime < FrameInterval) continue;
                      if (NeedRender || Playing)
                      {
                          bool actualRender = RenderFrame2();
                      }
                      System.Threading.Thread.Sleep(1);
                  }
              }, WorkItemPriority.Low, WorkItemOptions.TimeSliced);
        }
        #region Render Pipeline
        RenderPipeline.ForwardRenderPipeline1 forwardRenderPipeline1 = new RenderPipeline.ForwardRenderPipeline1();
        public RenderPipeline.PostProcess postProcess = new RenderPipeline.PostProcess();
        public RenderPipeline.RenderPipeline CurrentRenderPipeline { get => _currentRenderPipeline; }
        RenderPipeline.RenderPipeline _currentRenderPipeline;
        public DxgiFormat RTFormat = DxgiFormat.DXGI_FORMAT_R16G16B16A16_UNORM;
        Task[] poolTasks1;
        private void UpdateEntities()
        {
            int threshold = 1;
            if (Entities.Count > threshold)
            {
                if (poolTasks1 != null && poolTasks1.Length == Entities.Count - threshold)
                {
                    for (int i = 0; i < Entities.Count - threshold; i++)
                    {
                        poolTasks1[i] = null;
                    }
                }
                else
                    poolTasks1 = new Task[Entities.Count - threshold];

                for (int i = threshold; i < Entities.Count; i++)
                {
                    MMD3DEntity entity = Entities[i];
                    poolTasks1[i - threshold] = Task.Run(() => entity.SetMotionTime(PlayTime));
                }
                for (int i = 0; i < threshold; i++)
                    Entities[i].SetMotionTime(PlayTime);
                Task.WaitAll(poolTasks1);
            }
            else for (int i = 0; i < Entities.Count; i++)
                    Entities[i].SetMotionTime(PlayTime);
        }
        bool rendering = false;
        public void RequireRender(bool updateEntities = false)
        {
            NeedUpdateEntities |= updateEntities;
            NeedRender = true;
        }

        List<Texture2D> textureProcessing = new List<Texture2D>();
        List<RenderTexture2D> rtProcessing = new List<RenderTexture2D>();
        List<MMDMesh> meshProcessing = new List<MMDMesh>();
        RenderPipeline.RenderPipelineContext renderPipelineContext = new RenderPipeline.RenderPipelineContext();
        private bool RenderFrame2()
        {
            if (DateTime.Now - LatestRenderTime > FrameInterval && !rendering)
            {
                rendering = true;
                NeedRender = false;
                DateTime now = DateTime.Now;
                float deltaTime = MathF.Min((float)(now - LatestRenderTime).TotalSeconds, 0.17f) * PlaySpeed;
                LatestRenderTime = now;
                lock (deviceResources)
                {
                    #region Render Preparing
                    mainCaches.textureLoadList.MoveTo_CC(textureProcessing);
                    mainCaches.RenderTextureUpdateList.MoveTo_CC(rtProcessing);
                    mainCaches.mmdMeshLoadList.MoveTo_CC(meshProcessing);

                    lock (CurrentScene)
                    {
                        for (int i = 0; i < CurrentScene.EntityLoadList.Count; i++)
                        {
                            CurrentScene.Entities.Add(CurrentScene.EntityLoadList[i]);
                        }
                        CurrentScene.EntityLoadList.Clear();
                        for (int i = 0; i < CurrentScene.LightingLoadList.Count; i++)
                        {
                            CurrentScene.Lightings.Add(CurrentScene.LightingLoadList[i]);
                        }
                        CurrentScene.LightingLoadList.Clear();
                    }

                    camera.AspectRatio = AspectRatio;
                    camera.Update();
                    for (int i = 0; i < Lightings.Count; i++)
                        Lightings[i].UpdateLightingData(settings.ExtendShadowMapRange, camera);


                    if (textureProcessing.Count > 0 || rtProcessing.Count > 0 || meshProcessing.Count > 0 || worldViewer.RequireResize)
                    {
                        GraphicsContext.BeginAlloctor(deviceResources);
                        graphicsContext.BeginCommand();
                        for (int i = 0; i < textureProcessing.Count; i++)
                        {
                            graphicsContext.UploadTexture(textureProcessing[i]);
                        }
                        for (int i = 0; i < meshProcessing.Count; i++)
                        {
                            graphicsContext.UploadMesh(meshProcessing[i]);
                        }
                        graphicsContext.EndCommand();
                        graphicsContext.Execute();
                        deviceResources.WaitForGpu();
                        if (worldViewer.RequireResize)
                        {
                            worldViewer.RequireResize = false;
                            deviceResources.SetLogicalSize(worldViewer.NewSize);
                            int x = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Width), 1);
                            int y = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Height), 1);
                            defaultResources.ScreenSizeRenderTextureOutput.ReloadAsRenderTarget(deviceResources, x, y, RTFormat);
                            rtProcessing.Add(defaultResources.ScreenSizeRenderTextureOutput);
                            for(int i=0;i<defaultResources.ScreenSizeRenderTextures.Length;i++)
                            {
                                defaultResources.ScreenSizeRenderTextures[i].ReloadAsRenderTarget(deviceResources, x, y, RTFormat);
                                rtProcessing.Add(defaultResources.ScreenSizeRenderTextures[i]);
                            }
                            defaultResources.ScreenSizeDepthStencilOutput.ReloadAsDepthStencil(deviceResources, x, y);
                            rtProcessing.Add(defaultResources.ScreenSizeDepthStencilOutput);
                        }
                        for (int i = 0; i < rtProcessing.Count; i++)
                            graphicsContext.UpdateRenderTexture(rtProcessing[i]);
                        for (int i = 0; i < textureProcessing.Count; i++)
                            textureProcessing[i].ReleaseUploadHeapResource();
                        for (int i = 0; i < meshProcessing.Count; i++)
                            meshProcessing[i].ReleaseUploadHeapResource();
                        textureProcessing.Clear();
                        meshProcessing.Clear();
                        rtProcessing.Clear();
                    }
                    #region context preparing
                    renderPipelineContext.cameras = new Camera[] { camera };
                    renderPipelineContext.deviceResources = deviceResources;
                    renderPipelineContext.graphicsContext = graphicsContext;
                    renderPipelineContext.outputDSV = defaultResources.ScreenSizeDepthStencilOutput;
                    renderPipelineContext.outputRTV = defaultResources.ScreenSizeRenderTextureOutput;
                    renderPipelineContext.scene = CurrentScene;
                    renderPipelineContext.TextureLoading = defaultResources.TextureLoading;
                    renderPipelineContext.TextureError = defaultResources.TextureError;
                    renderPipelineContext.settings = settings;
                    renderPipelineContext.ndcQuadMesh = defaultResources.quadMesh;
                    renderPipelineContext.DSV0 = defaultResources.DepthStencil0;
                    #endregion
                    #endregion

                    GraphicsContext.BeginAlloctor(deviceResources);
                    graphicsContext.BeginCommand();
                    graphicsContext.BeginEvent();
                    graphicsContext.SetDescriptorHeapDefault();
                    if (Playing)
                        PlayTime += deltaTime;
                    if (Playing || NeedUpdateEntities)
                    {
                        NeedUpdateEntities = false;
                        UpdateEntities();
                        forwardRenderPipeline1.TimeChange(PlayTime, deltaTime);
                    }

                    for (int i = 0; i < Entities.Count; i++)
                        Entities[i].UpdateGpuResources(graphicsContext);

                    graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._PRESENT, D3D12ResourceStates._RENDER_TARGET);
                    if (_currentRenderPipeline.Ready)
                    {
                        _currentRenderPipeline.PrepareRenderData(renderPipelineContext);
                        _currentRenderPipeline.BeforeRenderCameras(renderPipelineContext);
                        _currentRenderPipeline.RenderCamera(renderPipelineContext, 0);
                    }
                    if (postProcess.Ready)
                    {
                        postProcess.PrepareRenderData(renderPipelineContext);
                        postProcess.BeforeRenderCameras(renderPipelineContext);
                        postProcess.RenderCamera(renderPipelineContext, 0);
                    }
                    //if (defaultResources.Initilized && settings.viewSelectedEntityBone)
                    //{
                    //    graphicsContext.ClearDepthStencil();
                    //    for (int i = 0; i < SelectedEntities.Count; i++)
                    //    {
                    //        if (SelectedEntities[i].ComponentReady)
                    //            widgetRenderer.RenderBoneVisual(graphicsContext, camera, SelectedEntities[i]);
                    //    }
                    //}
                    graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._RENDER_TARGET, D3D12ResourceStates._PRESENT);
                    graphicsContext.EndEvent();
                    graphicsContext.EndCommand();
                    graphicsContext.Execute();
                    RenderCount++;
                    deviceResources.Present(false);
                    rendering = false;
                }
                return true;
            }
            else
            {
                NeedRender = true;
                return false;
            }
        }
        #endregion

        int currentRenderPipelineIndex;
        public void SwitchToRenderPipeline(int index)
        {
            if (currentRenderPipelineIndex != index)
            {
                currentRenderPipelineIndex = index;

            }
        }

        public void ForceAudioAsync() => AudioAsync(PlayTime, Playing);
        TimeSpan audioMaxInaccuracy = TimeSpan.FromSeconds(1.0 / 30.0);
        private void AudioAsync(float time, bool playing)
        {
            if (playing && PlaySpeed == 1.0f)
            {
                if (mediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Paused ||
                    mediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Stopped)
                {
                    mediaElement.Play();
                }
                if (mediaElement.IsAudioOnly)
                {
                    if (TimeSpan.FromSeconds(PlayTime) - mediaElement.Position > audioMaxInaccuracy ||
                        mediaElement.Position - TimeSpan.FromSeconds(PlayTime) > audioMaxInaccuracy)
                    {
                        mediaElement.Position = TimeSpan.FromSeconds(PlayTime);
                    }
                }
                else
                {
                    if (TimeSpan.FromSeconds(PlayTime) - mediaElement.Position > audioMaxInaccuracy ||
                           mediaElement.Position - TimeSpan.FromSeconds(PlayTime) > audioMaxInaccuracy)
                    {
                        mediaElement.Position = TimeSpan.FromSeconds(PlayTime);
                    }
                }
            }
            else if (mediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
            {
                mediaElement.Pause();
            }
        }

        public async Task WaitForResourcesLoadedAsync()
        {
            if (!defaultResources.LoadTask.IsCompleted)
                await defaultResources.LoadTask;
        }
        #region UI
        public Frame frameViewProperties;
        public void ShowDetailPage(Type page, object parameter)
        {
            frameViewProperties.Navigate(page, parameter);
        }
        #endregion
    }


    public struct Settings
    {
        public bool viewSelectedEntityBone;
        public bool HighResolutionShadow;
        public Vector4 backgroundColor;
        public float ExtendShadowMapRange;
    }
}
