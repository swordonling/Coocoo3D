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
using System.Collections.ObjectModel;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Coocoo3DPhysics;
using System.Globalization;
using Coocoo3D.FileFormat;
using Coocoo3D.Components;
using Coocoo3D.RenderPipeline;

namespace Coocoo3D.Core
{
    ///<summary>是整个应用程序的上下文</summary>
    public class Coocoo3DMain
    {
        public DeviceResources deviceResources = new DeviceResources();
        public WICFactory wicFactory = new WICFactory();
        //public DefaultResources defaultResources = new DefaultResources();
        public MainCaches mainCaches = new MainCaches();

        public Scene CurrentScene;

        private List<MMD3DEntity> Entities { get => CurrentScene.Entities; }
        private ObservableCollection<ISceneObject> sceneObjects { get => CurrentScene.sceneObjects; }
        public List<MMD3DEntity> SelectedEntities = new List<MMD3DEntity>();
        public List<Lighting> SelectedLighting = new List<Lighting>();


        public Camera camera = new Camera();
        //public WidgetRenderer widgetRenderer = new WidgetRenderer();
        public GameDriver GameDriver;
        public GeneralGameDriver _GeneralGameDriver = new GeneralGameDriver();
        public RecorderGameDriver _RecorderGameDriver = new RecorderGameDriver();
        public GameDriverContext GameDriverContext = new GameDriverContext()
        {
            FrameInterval = TimeSpan.FromSeconds(1 / 120.0),
            recordSettings = new RecordSettings()
            {
                FPS = 60,
                Width = 1920,
                Height = 1080,
                StartTime = 0,
                StopTime = 9999,
            },
        };
        #region Time
        ThreadPoolTimer threadPoolTimer;
        volatile bool NeedUpdateEntities = false;
        public bool SaveCpuPower = true;

        public DateTime LatestRenderTime = DateTime.Now;
        public float Fps = 120;
        public CoreDispatcher Dispatcher;
        public event EventHandler FrameUpdated;

        public volatile int RenderCount = 0;
        private async void Tick(ThreadPoolTimer timer)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                FrameUpdated?.Invoke(this, null);
            });
        }
        #endregion
        public Settings settings = new Settings()
        {
            viewSelectedEntityBone = true,
            HighResolutionShadow = false,
            backgroundColor = new Vector4(0, 0.3f, 0.3f, 0.0f),
            ExtendShadowMapRange = 48,
            SkyBoxLightMultiple = 1.0f,
            ZPrepass = false,
            EnableAO = true,
            EnableShadow = true,
        };
        public bool HighResolutionShadowNow = false;
        public Physics3D physics3D = new Physics3D();
        public Physics3DScene physics3DScene = new Physics3DScene();
        IAsyncAction RenderLoop;
        public Coocoo3DMain()
        {
            GameDriver = _GeneralGameDriver;
            GameDriverContext.DeviceResources = deviceResources;
            GameDriverContext.ProcessingList = ProcessingList;
            GameDriverContext.WICFactory = wicFactory;
            _currentRenderPipeline = forwardRenderPipeline1;
            graphicsContext.Reload(deviceResources);
            graphicsContext2.Reload(deviceResources);
            renderPipelineContext.LoadTask = Task.Run(async () =>
            {
                RPAssetsManager.Reload(deviceResources);
                forwardRenderPipeline1.Reload(deviceResources);
                postProcess.Reload(deviceResources);
                if (deviceResources.IsRayTracingSupport())
                    rayTracingRenderPipeline1.Reload(deviceResources);
                await renderPipelineContext.ReloadDefalutResources(wicFactory, ProcessingList, miscProcessContext);

                await RPAssetsManager.ReloadAssets();
                RPAssetsManager.ChangeRenderTargetFormat(deviceResources, RTFormat, backBufferFormat);

                //await forwardRenderPipeline1.ReloadAssets(deviceResources);
                //forwardRenderPipeline1.ChangeRenderTargetFormat(deviceResources, RTFormat);

                await miscProcess.ReloadAssets(deviceResources);
                //widgetRenderer.Init(mainCaches, defaultResources, mainCaches.textureCaches);
                if (deviceResources.IsRayTracingSupport())
                    await rayTracingRenderPipeline1.ReloadAssets(deviceResources);
                RequireRender();
            });
            //PhysXAPI.SetAPIUsed(physics3D);
            BulletAPI.SetAPIUsed(physics3D);
            physics3D.Init();
            physics3DScene.Reload(physics3D);
            physics3DScene.SetGravitation(new Vector3(0, -98.01f, 0));

            CurrentScene = new Scene(this);
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(Tick, TimeSpan.FromSeconds(1 / 10.0));
            RenderLoop = ThreadPool.RunAsync((IAsyncAction action) =>
              {
                  while (action.Status == AsyncStatus.Started)
                  {
                      DateTime now = DateTime.Now;
                      if (now - LatestRenderTime < GameDriverContext.FrameInterval) continue;
                      //if (NeedRender || Playing)
                      //{
                      bool actualRender = RenderFrame2();
                      //}
                      if (SaveCpuPower)
                          System.Threading.Thread.Sleep(1);
                  }
              }, WorkItemPriority.Low, WorkItemOptions.TimeSliced);
        }
        #region Rendering
        public RPAssetsManager RPAssetsManager = new RPAssetsManager();
        ForwardRenderPipeline1 forwardRenderPipeline1 = new ForwardRenderPipeline1();
        RayTracingRenderPipeline1 rayTracingRenderPipeline1 = new RayTracingRenderPipeline1();
        MiscProcess miscProcess = new MiscProcess();
        public MiscProcessContext miscProcessContext = new MiscProcessContext();
        MiscProcessContext _miscProcessContext = new MiscProcessContext();
        public PostProcess postProcess = new PostProcess();
        public RenderPipeline.RenderPipeline CurrentRenderPipeline { get => _currentRenderPipeline; }
        RenderPipeline.RenderPipeline _currentRenderPipeline;
        public DxgiFormat RTFormat = DxgiFormat.DXGI_FORMAT_R16G16B16A16_UNORM;
        public DxgiFormat backBufferFormat = DxgiFormat.DXGI_FORMAT_B8G8R8A8_UNORM;
        public bool UseNewFun;
        Task[] poolTasks1;
        private void UpdateEntities(float playTime)
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
                    poolTasks1[i - threshold] = Task.Run(() => entity.SetMotionTime(playTime));
                }
                for (int i = 0; i < threshold; i++)
                    Entities[i].SetMotionTime(playTime);
                Task.WaitAll(poolTasks1);
            }
            else for (int i = 0; i < Entities.Count; i++)
                {
                    Entities[i].SetMotionTime(playTime);
                    //var entity = Entities[i];
                    //if (!entity.ComponentReady) continue;

                    //entity.morphStateComponent.SetPose(entity.motionComponent, playTime);
                    //entity.morphStateComponent.ComputeWeight();


                    //stopwatch1.Restart();
                    //entity.boneComponent.SetPose(entity.motionComponent, entity.morphStateComponent, playTime);

                    //stopwatch1.Stop();
                    //StopwatchTimes[0] = stopwatch1.ElapsedTicks;
                    //stopwatch1.Restart();
                    //entity.boneComponent.ComputeMatricesData();
                    //entity.rendererComponent.SetPose(entity.morphStateComponent);
                    //entity.needUpdateMotion = true;

                    //stopwatch1.Stop();
                    //StopwatchTimes[1] = stopwatch1.ElapsedTicks;
                }
        }
        bool rendering = false;
        public void RequireRender(bool updateEntities = false)
        {
            NeedUpdateEntities |= updateEntities;
            GameDriverContext.NeedRender = true;
        }

        public ProcessingList ProcessingList = new ProcessingList();
        ProcessingList _processingList = new ProcessingList();
        public RenderPipeline.RenderPipelineContext renderPipelineContext = new RenderPipeline.RenderPipelineContext();

        public bool swapChainReady;
        //public long[] StopwatchTimes = new long[8];
        //System.Diagnostics.Stopwatch stopwatch1 = new System.Diagnostics.Stopwatch();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public GraphicsContext graphicsContext2 = new GraphicsContext();
        private bool RenderFrame2()
        {
            if (!GameDriver.Next(ref GameDriverContext) || rendering)
            {
                return false;
            }
            lock (deviceResources)
            {
                #region Render Preparing

                renderPipelineContext.ClearCollections();
                lock (CurrentScene)
                {
                    for (int i = 0; i < CurrentScene.EntityLoadList.Count; i++)
                    {
                        CurrentScene.Entities.Add(CurrentScene.EntityLoadList[i]);
                        CurrentScene.EntityLoadList[i].boneComponent.AddPhysics(physics3DScene);
                    }
                    for (int i = 0; i < CurrentScene.LightingLoadList.Count; i++)
                    {
                        CurrentScene.Lightings.Add(CurrentScene.LightingLoadList[i]);
                    }
                    for (int i = 0; i < CurrentScene.EntityRemoveList.Count; i++)
                    {
                        CurrentScene.EntityRemoveList[i].boneComponent.RemovePhysics(physics3DScene);
                    }
                    CurrentScene.EntityLoadList.Clear();
                    CurrentScene.LightingLoadList.Clear();
                    CurrentScene.EntityRemoveList.Clear();
                    renderPipelineContext.entities.AddRange(CurrentScene.Entities);
                    renderPipelineContext.lightings.AddRange(CurrentScene.Lightings);
                }
                var entities = renderPipelineContext.entities;
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    if (entity.NeedTransform)
                    {
                        entity.NeedTransform = false;
                        entity.Position = entity.PositionNextFrame;
                        entity.Rotation = entity.RotationNextFrame;
                        entity.boneComponent.TransformToNew(physics3DScene, entity.Position, entity.Rotation);
                    }
                }
                if (camera.CameraMotionOn) camera.SetCameraMotion((float)GameDriverContext.PlayTime);
                camera.AspectRatio = GameDriverContext.AspectRatio;
                camera.Update();
                for (int i = 0; i < CurrentScene.Lightings.Count; i++)
                    CurrentScene.Lightings[i].UpdateLightingData(settings.ExtendShadowMapRange, camera);
                bool needUpdateEntities = NeedUpdateEntities;
                NeedUpdateEntities = false;
                if (GameDriverContext.RequireResetPhysics)
                {
                    GameDriverContext.RequireResetPhysics = false;
                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.ResetPhysics(physics3DScene);
                    }
                }
                double t1 = GameDriverContext.deltaTime;
                if (GameDriverContext.Playing || needUpdateEntities)
                {
                    if (t1 >= 0)
                    {
                        //while (t1 > 1e-7f)
                        //{
                        //    const float c_stepTime = 1 / 60.0f;
                        //    double t2 = t1 > c_stepTime ? c_stepTime : t1;
                        //    if (Playing)
                        //        PlayTime += t2;
                        //    UpdateEntities((float)PlayTime);
                        //    for (int i = 0; i < entities.Count; i++)
                        //    {
                        //        entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                        //    }
                        //    physics3DScene.Simulate(t2);
                        //    t1 -= c_stepTime;
                        //    if (t1 < 0.0f)
                        //        t1 = 0.0f;
                        //    physics3DScene.FetchResults();
                        //}
                        //if (Playing)
                        //    PlayTime += t1;
                        UpdateEntities((float)GameDriverContext.PlayTime);
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                        }
                        physics3DScene.Simulate(t1);
                        physics3DScene.FetchResults();
                    }
                    else
                    {
                        //if (Playing)
                        //    PlayTime += t1;
                        UpdateEntities((float)GameDriverContext.PlayTime);
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                        }
                        physics3DScene.Simulate(-t1);
                        physics3DScene.FetchResults();
                    }
                }
                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].boneComponent.SetPoseAfterPhysics(physics3DScene);
                    entities[i].boneComponent.WriteMatriticesData();
                }

                ////test code-------------------

                ////stopwatch1.Restart();
                //if (Playing)
                //    PlayTime += t1;
                //if (Playing || needUpdateEntities)
                //    UpdateEntities((float)PlayTime);
                ////stopwatch1.Stop();
                ////StopwatchTimes[0] = stopwatch1.ElapsedTicks;
                ////stopwatch1.Restart();
                //for (int i = 0; i < entities.Count; i++)
                //    entities[i].boneComponent.WriteMatriticesData();
                ////stopwatch1.Stop();
                ////StopwatchTimes[1] = stopwatch1.ElapsedTicks;
                ////endtest code-------------------

                ProcessingList.MoveToAnother(_processingList);
                if (!_processingList.IsEmpty() || GameDriverContext.RequireInterruptRender)
                {
                    GameDriverContext.RequireInterruptRender = false;
                    GraphicsContext.BeginAlloctor(deviceResources);
                    graphicsContext.BeginCommand();
                    _processingList._DealStep1(graphicsContext);
                    graphicsContext.EndCommand();
                    graphicsContext.Execute();
                    if (GameDriverContext.RequireResize)
                    {
                        GameDriverContext.RequireResize = false;
                        deviceResources.SetLogicalSize(GameDriverContext.NewSize);
                        int x = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Width), 1);
                        int y = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Height), 1);
                        renderPipelineContext.outputRTV.ReloadAsRTVUAV(x, y, RTFormat);
                        _processingList.UnsafeAdd(renderPipelineContext.outputRTV);
                        for (int i = 0; i < renderPipelineContext.ScreenSizeRenderTextures.Length; i++)
                        {
                            renderPipelineContext.ScreenSizeRenderTextures[i].ReloadAsRTVUAV(x, y, RTFormat);
                            _processingList.UnsafeAdd(renderPipelineContext.ScreenSizeRenderTextures[i]);
                        }
                        for (int i = 0; i < renderPipelineContext.ScreenSizeDSVs.Length; i++)
                        {
                            renderPipelineContext.ScreenSizeDSVs[i].ReloadAsDepthStencil(x, y);
                            _processingList.UnsafeAdd(renderPipelineContext.ScreenSizeDSVs[i]);
                        }
                    }
                    if (HighResolutionShadowNow != settings.HighResolutionShadow)
                    {
                        HighResolutionShadowNow = settings.HighResolutionShadow;
                        if (HighResolutionShadowNow)
                            renderPipelineContext.DSV0.ReloadAsDepthStencil(8192, 8192);
                        else
                            renderPipelineContext.DSV0.ReloadAsDepthStencil(4096, 4096);
                        _processingList.UnsafeAdd(renderPipelineContext.DSV0);
                    }
                    deviceResources.WaitForGpu();
                    _processingList._DealStep2(graphicsContext);
                    _processingList.Clear();
                }
                #endregion

                GraphicsContext.BeginAlloctor(deviceResources);

                {
                    miscProcessContext.MoveToAnother(_miscProcessContext);
                    _miscProcessContext.graphicsContext = graphicsContext2;
                    graphicsContext2.BeginCommand();
                    graphicsContext2.SetDescriptorHeapDefault();
                    miscProcess.Process(_miscProcessContext);
                    graphicsContext2.EndCommand();
                    graphicsContext2.Execute();
                }

                if (swapChainReady && !GameDriverContext.RequireResize)
                {
                    #region context preparing
                    renderPipelineContext.cameras.Add(camera);
                    renderPipelineContext.deviceResources = deviceResources;
                    renderPipelineContext.graphicsContext = graphicsContext;
                    renderPipelineContext.settings = settings;
                    renderPipelineContext.RPAssetsManager = RPAssetsManager;
                    #endregion

                    graphicsContext.BeginCommand();
                    graphicsContext.SetDescriptorHeapDefault();


                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].UpdateGpuResources(graphicsContext);
                    }

                    var currentRenderPipeline = _currentRenderPipeline;//避免在渲染时切换
                    if (GameDriverContext.Playing || needUpdateEntities)
                    {
                        currentRenderPipeline.TimeChange(GameDriverContext.PlayTime, GameDriverContext.deltaTime);
                    }
                    graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._PRESENT, D3D12ResourceStates._RENDER_TARGET);
                    if (currentRenderPipeline.Ready && RPAssetsManager.Ready)
                    {
                        currentRenderPipeline.PrepareRenderData(renderPipelineContext);
                        currentRenderPipeline.RenderCamera(renderPipelineContext, 0);

                        //stopwatch1.Restart();
                        //currentRenderPipeline.PrepareRenderData(renderPipelineContext);
                        //stopwatch1.Stop();
                        //StopwatchTimes[3] = stopwatch1.ElapsedTicks;
                        //stopwatch1.Restart();
                        //currentRenderPipeline.RenderCamera(renderPipelineContext, 0);
                        //stopwatch1.Stop();
                        //StopwatchTimes[4] = stopwatch1.ElapsedTicks;
                    }
                    if (postProcess.Ready && RPAssetsManager.Ready)
                    {
                        postProcess.PrepareRenderData(renderPipelineContext);
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
                    GameDriver.AfterRender(graphicsContext, ref GameDriverContext);
                    graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._RENDER_TARGET, D3D12ResourceStates._PRESENT);
                    graphicsContext.EndCommand();
                    //stopwatch1.Restart();
                    graphicsContext.Execute();
                    //stopwatch1.Stop();
                    //StopwatchTimes[2] = stopwatch1.ElapsedTicks;
                    RenderCount++;
                    deviceResources.Present(false);
                }
                rendering = false;
            }
            return true;
        }
        #endregion
        public bool Recording = false;

        int currentRenderPipelineIndex;
        public void SwitchToRenderPipeline(int index)
        {
            if (currentRenderPipelineIndex != index)
            {
                currentRenderPipelineIndex = index;
                if (currentRenderPipelineIndex == 0)
                {
                    _currentRenderPipeline = forwardRenderPipeline1;
                }
                if (currentRenderPipelineIndex == 1)
                {
                    _currentRenderPipeline = rayTracingRenderPipeline1;
                }
            }
        }

        public StorageFolder openedStorageFolder;
        public event EventHandler OpenedStorageFolderChanged;
        public void OpenedStorageFolderChange(StorageFolder storageFolder)
        {
            openedStorageFolder = storageFolder;
            OpenedStorageFolderChanged?.Invoke(this, null);
        }

        public async Task WaitForResourcesLoadedAsync()
        {
            if (!renderPipelineContext.LoadTask.IsCompleted)
                await renderPipelineContext.LoadTask;
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
        public float SkyBoxLightMultiple;
        public bool ZPrepass;
        public bool EnableAO;
        public bool EnableShadow;
        public uint Quality;
        public uint RenderStyle;
    }
}
