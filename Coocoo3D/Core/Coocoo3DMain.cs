using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Coocoo3DGraphics;
using Coocoo3D.Present;
using Coocoo3D.Controls;
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
        public WidgetRenderer widgetRenderer = new WidgetRenderer();
        public PresentData[] presentDatas;
        public StorageFolder openedStorageFolder;
        public event EventHandler OpenedStorageFolderChanged;
        public void OpenedStorageFolderChange(StorageFolder storageFolder)
        {
            openedStorageFolder = storageFolder;
            OpenedStorageFolderChanged?.Invoke(this, null);
        }
        #region Time
        ThreadPoolTimer threadPoolTimer;
        bool NeedRender = false;
        bool NeedUpdateEntities = false;
        public DateTime LatestUserOperating = DateTime.Now;

        public float PlayTime;
        public float PlaySpeed = 1.0f;
        public bool Playing;
        public DateTime LatestRenderTime = DateTime.Now;
        public TimeSpan FrameInterval = TimeSpan.FromSeconds(1 / 120.0);
        public CoreDispatcher Dispatcher;
        public event EventHandler FrameUpdated;

        public int RenderCount = 0;//
        private async void Tick(ThreadPoolTimer timer)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                FrameUpdated?.Invoke(this, null);
                ForceAudioAsync();
            });
        }
        #endregion
        public Settings settings = new Settings();
        public float AspectRatio;
        IAsyncAction RenderLoop;
        public Coocoo3DMain()
        {
            graphicsContext.Reload(deviceResources);
            CurrentScene = new Scene(this);
            presentDatas = new PresentData[8];
            for (int i = 0; i < 8; i++)
            {
                presentDatas[i] = new PresentData();
                presentDatas[i].Reload(deviceResources);
            }
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(Tick, TimeSpan.FromSeconds(1 / 15.0));

            defaultResources.LoadTask = Task.Run(async () =>
            {
                await defaultResources.ReloadDefalutResources(deviceResources, mainCaches);
                widgetRenderer.Init(mainCaches, defaultResources, mainCaches.textureCaches);
            });
            RenderLoop = ThreadPool.RunAsync((IAsyncAction action) =>
              {
                  int a = 0;
                  while (action.Status == AsyncStatus.Started)
                  {
                      DateTime now = DateTime.Now;
                      if (now - LatestRenderTime < FrameInterval) continue;
                      if (NeedRender || Playing)
                      {
                          bool actualRender = RenderFrame2();
                      }
                      a = a ^ 1;
                      System.Threading.Thread.Sleep(a);
                  }
              }, WorkItemPriority.Low, WorkItemOptions.TimeSliced);
        }
        #region Render Pipeline
        private void UpdateEntities()
        {
            int threshold = 1;
            if (Entities.Count > threshold)
            {
                Task[] tasks = new Task[Entities.Count - threshold];
                for (int i = threshold; i < Entities.Count; i++)
                {
                    MMD3DEntity entity = Entities[i];
                    tasks[i - threshold] = Task.Run(() => entity.SetMotionTime(PlayTime));
                }
                for (int i = 0; i < threshold; i++)
                    Entities[i].SetMotionTime(PlayTime);
                Task.WaitAll(tasks);
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

        public void RenderFrame(bool updateEntities = false)
        {
            NeedUpdateEntities |= updateEntities;
            bool actualRender = RenderFrame2();
            //if (actualRender)
            FrameUpdated?.Invoke(this, null);
        }

        List<Texture2D> textureProcessing = new List<Texture2D>();
        List<MMDMesh> meshProcessing = new List<MMDMesh>();
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
                    lock (mainCaches.textureLoadList)
                    {
                        if (mainCaches.textureLoadList.Count > 0)
                        {
                            textureProcessing.AddRange(mainCaches.textureLoadList);
                            mainCaches.textureLoadList.Clear();
                        }
                    }
                    lock (mainCaches.mmdMeshLoadList)
                    {
                        if (mainCaches.mmdMeshLoadList.Count > 0)
                        {
                            meshProcessing.AddRange(mainCaches.mmdMeshLoadList);
                            mainCaches.mmdMeshLoadList.Clear();
                        }
                    }
                    graphicsContext.BeginCommand();
                    for (int i = 0; i < textureProcessing.Count; i++)
                    {
                        graphicsContext.UploadTexture(textureProcessing[i]);
                    }
                    for (int i = 0; i < meshProcessing.Count; i++)
                    {
                        graphicsContext.UploadMesh(meshProcessing[i]);
                    }
                    textureProcessing.Clear();
                    meshProcessing.Clear();

                    if (Playing)
                        PlayTime += deltaTime;
                    if (Playing || NeedUpdateEntities)
                    {
                        NeedUpdateEntities = false;
                        UpdateEntities();
                        for (int i = 0; i < presentDatas.Length; i++)
                        {
                            presentDatas[i].PlayTime = PlayTime;
                            presentDatas[i].DeltaTime = deltaTime;
                        }
                    }

                    camera.AspectRatio = AspectRatio;
                    camera.Update();
                    presentDatas[0].UpdateCameraData(camera);
                    presentDatas[0].UpdateBuffer(graphicsContext);
                    for (int i = 0; i < Lightings.Count; i++)
                        Lightings[i].UpdateLightingData(graphicsContext, settings.ExtendShadowMapRange, camera);
                    for (int i = 0; i < Entities.Count; i++)
                        Entities[i].UpdateGpuResources(graphicsContext, Lightings);


                    if (Entities.Count > 0)
                    {
                        graphicsContext.SetSRV(PObjectType.mmd, null, 3);
                        graphicsContext.SetAndClearDSV(defaultResources.DepthStencil0);
                        if (Lightings.Count > 0)
                        {
                            presentDatas[1].UpdateCameraData(Lightings[0]);
                            presentDatas[1].UpdateBuffer(graphicsContext);
                            for (int i = 0; i < Entities.Count; i++)
                                Entities[i].RenderDepth(graphicsContext, defaultResources, presentDatas[1]);
                        }
                        graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
                        graphicsContext.SetSRV_RT(PObjectType.mmd, defaultResources.DepthStencil0, 3);
                    }
                    else
                    {
                        graphicsContext.SetRenderTargetScreenAndClear(settings.backgroundColor);
                    }

                    for (int i = 0; i < Entities.Count; i++)
                        Entities[i].Render(graphicsContext, defaultResources, Lightings, presentDatas[0]);

                    if (defaultResources.Initilized && settings.viewSelectedEntityBone)
                    {
                        graphicsContext.ClearDepthStencil();
                        for (int i = 0; i < SelectedEntities.Count; i++)
                        {
                            if (SelectedEntities[i].ComponentReady)
                                widgetRenderer.RenderBoneVisual(graphicsContext, camera, SelectedEntities[i]);
                        }
                    }
                    graphicsContext.EndCommand();
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


    public class Settings
    {
        public bool viewSelectedEntityBone = true;
        public bool HighResolutionShadow = false;
        public Vector4 backgroundColor = new Vector4(0, 0.3f, 0.3f, 0.0f);
        public float ExtendShadowMapRange = 32;
    }
}
