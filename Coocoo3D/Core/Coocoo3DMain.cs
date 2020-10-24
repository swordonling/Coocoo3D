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
            FrameInterval = TimeSpan.FromSeconds(1 / 240.0),
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

        public DateTime LatestRenderTime = DateTime.Now;
        public float Fps = 240;
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
            backgroundColor = new Vector4(0, 0.3f, 0.3f, 0.0f),
            ExtendShadowMapRange = 64,
            ZPrepass = false,
        };
        public InShaderSettings inShaderSettings = new InShaderSettings()
        {
            //backgroundColor = new Vector4(0, 0.3f, 0.3f, 0.0f),
            SkyBoxLightMultiple = 1.0f,
            EnableAO = true,
            EnableShadow = true,
            Quality = 0,
        };
        public PerformaceSettings performaceSettings = new PerformaceSettings()
        {
            MultiThreadRendering = true,
            SaveCpuPower = true,
            HighResolutionShadow = false,
            AutoReloadShaders = true,
            AutoReloadTextures = true,
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
            renderPipelineContext.deviceResources = deviceResources;
            renderPipelineContext.LoadTask = Task.Run(async () =>
            {
                RPAssetsManager.Reload(deviceResources);
                forwardRenderPipeline1.Reload(deviceResources);
                postProcess.Reload(deviceResources);
                renderPipelineContext.SkinningMeshBuffer.Reload(deviceResources, 0);
                if (deviceResources.IsRayTracingSupport())
                    rayTracingRenderPipeline1.Reload(deviceResources);
                await renderPipelineContext.ReloadDefalutResources(wicFactory, ProcessingList, miscProcessContext);

                await RPAssetsManager.ReloadAssets();
                RPAssetsManager.ChangeRenderTargetFormat(deviceResources, ProcessingList, renderPipelineContext.RTFormat, renderPipelineContext.backBufferFormat);

                //await forwardRenderPipeline1.ReloadAssets(deviceResources);

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

            CurrentScene = new Scene();
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(Tick, TimeSpan.FromSeconds(1 / 10.0));
            RenderLoop = ThreadPool.RunAsync((IAsyncAction action) =>
              {
                  while (action.Status == AsyncStatus.Started)
                  {
                      DateTime now = DateTime.Now;
                      if (now - LatestRenderTime < GameDriverContext.FrameInterval) continue;
                      bool actualRender = RenderFrame2();
                      if (performaceSettings.SaveCpuPower)
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
                }
        }
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
        Task RenderTask1;
        private bool RenderFrame2()
        {
            if (!GameDriver.Next(ref GameDriverContext))
            {
                return false;
            }
            lock (deviceResources)
            {
                #region Render Preparing

                renderPipelineContext.dynamicContext1.ClearCollections();
                renderPipelineContext.dynamicContext1.frameRenderIndex = renderPipelineContext.frameRenderIndex;
                renderPipelineContext.frameRenderIndex++;
                CurrentScene.DealProcessList(physics3DScene);
                lock (CurrentScene)
                {
                    renderPipelineContext.dynamicContext1.entities.AddRange(CurrentScene.Entities);
                    for (int i = 0; i < CurrentScene.Lightings.Count; i++)
                    {
                        renderPipelineContext.dynamicContext1.lightings.Add(CurrentScene.Lightings[i].GetLightingData());
                    }
                }

                var entities = renderPipelineContext.dynamicContext1.entities;
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    if (entity.NeedTransform)
                    {
                        entity.NeedTransform = false;
                        entity.Position = entity.PositionNextFrame;
                        entity.Rotation = entity.RotationNextFrame;
                        entity.boneComponent.TransformToNew(physics3DScene, entity.Position, entity.Rotation);

                        GameDriverContext.RequireResetPhysics = true;
                    }
                }
                if (camera.CameraMotionOn) camera.SetCameraMotion((float)GameDriverContext.PlayTime);
                camera.AspectRatio = GameDriverContext.AspectRatio;
                camera.Update();
                renderPipelineContext.dynamicContext1.cameras.Add(camera.GetCameraData());
                renderPipelineContext.dynamicContext1.settings = settings;
                renderPipelineContext.dynamicContext1.inShaderSettings = inShaderSettings;
                renderPipelineContext.dynamicContext1.Preprocess();


                bool needUpdateEntities = NeedUpdateEntities;
                NeedUpdateEntities = false;

                void _ResetPhysics()
                {
                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.ResetPhysics(physics3DScene);
                    }
                    physics3DScene.Simulate(1 / 60.0);
                    physics3DScene.FetchResults();
                }

                double t1 = GameDriverContext.deltaTime;
                void _BoneUpdate()
                {
                    UpdateEntities((float)GameDriverContext.PlayTime);

                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                    }
                    if (t1 >= 0)
                    {
                        physics3DScene.Simulate(t1);
                        physics3DScene.FetchResults();
                    }
                    else
                    {
                        physics3DScene.Simulate(-t1);
                        physics3DScene.FetchResults();
                    }
                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.SetPoseAfterPhysics(physics3DScene);
                    }
                }
                if (GameDriverContext.RequireResetPhysics)
                {
                    GameDriverContext.RequireResetPhysics = false;
                    _ResetPhysics();
                    _BoneUpdate();
                    _ResetPhysics();
                }
                if (GameDriverContext.Playing || needUpdateEntities)
                {
                    _BoneUpdate();
                }

                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].boneComponent.WriteMatriticesData();
                }

                if (RenderTask1 != null && RenderTask1.Status != TaskStatus.RanToCompletion) RenderTask1.Wait();
                #region Render preparing
                var temp1 = renderPipelineContext.dynamicContext1;
                renderPipelineContext.dynamicContext1 = renderPipelineContext.dynamicContext;
                renderPipelineContext.dynamicContext = temp1;


                ProcessingList.MoveToAnother(_processingList);
                int SceneObjectVertexCount = renderPipelineContext.dynamicContext.GetSceneObjectVertexCount();
                if (!_processingList.IsEmpty() || GameDriverContext.RequireInterruptRender || SceneObjectVertexCount > renderPipelineContext.SkinningMeshBufferSize)
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

                        renderPipelineContext.ReloadTextureSizeResources(_processingList);
                    }
                    if (HighResolutionShadowNow != performaceSettings.HighResolutionShadow)
                    {
                        HighResolutionShadowNow = performaceSettings.HighResolutionShadow;
                        if (HighResolutionShadowNow)
                            renderPipelineContext.DSV0.ReloadAsDepthStencil(8192, 8192);
                        else
                            renderPipelineContext.DSV0.ReloadAsDepthStencil(4096, 4096);
                        _processingList.UnsafeAdd(renderPipelineContext.DSV0);
                    }
                    deviceResources.WaitForGpu();
                    _processingList._DealStep2(graphicsContext, deviceResources);
                    RPAssetsManager._DealStep3(deviceResources, _processingList);
                    _processingList.Clear();
                    if (SceneObjectVertexCount > renderPipelineContext.SkinningMeshBufferSize)
                    {
                        renderPipelineContext.SkinningMeshBuffer.Reload(deviceResources, SceneObjectVertexCount);
                        renderPipelineContext.SkinningMeshBufferSize = SceneObjectVertexCount;
                        renderPipelineContext.LightCacheBuffer.Initialize(deviceResources, SceneObjectVertexCount * 16);
                    }
                }
                #endregion

                GraphicsContext.BeginAlloctor(deviceResources);

                miscProcessContext.MoveToAnother(_miscProcessContext);
                _miscProcessContext.graphicsContext = graphicsContext2;
                miscProcess.Process(_miscProcessContext);

                if (swapChainReady && !GameDriverContext.RequireResize)
                {
                    #region context preparing
                    renderPipelineContext.deviceResources = deviceResources;
                    renderPipelineContext.graphicsContext = graphicsContext;
                    renderPipelineContext.RPAssetsManager = RPAssetsManager;
                    #endregion

                    graphicsContext.BeginCommand();
                    graphicsContext.SetDescriptorHeapDefault();



                    var currentRenderPipeline = _currentRenderPipeline;//避免在渲染时切换
                    if (GameDriverContext.Playing || needUpdateEntities)
                        currentRenderPipeline.TimeChange(GameDriverContext.PlayTime, GameDriverContext.deltaTime);
                    else
                        currentRenderPipeline.TimeChange(GameDriverContext.PlayTime, 0);


                    var entities1 = renderPipelineContext.dynamicContext.entities;
                    for (int i = 0; i < entities1.Count; i++)
                    {
                        entities1[i].UpdateGpuResources(graphicsContext);
                    }
                    currentRenderPipeline.PrepareRenderData(renderPipelineContext);
                    postProcess.PrepareRenderData(renderPipelineContext);
                    #endregion

                    void _RenderFunction()
                    {
                        graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._PRESENT, D3D12ResourceStates._RENDER_TARGET);
                        renderPipelineContext.UpdateGPUResource();
                        if (RPAssetsManager.Ready)
                        {
                            if (currentRenderPipeline.Ready)
                                currentRenderPipeline.RenderCamera(renderPipelineContext, 1);
                            if (postProcess.Ready && RPAssetsManager.Ready)
                                postProcess.RenderCamera(renderPipelineContext, 1);
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
                        graphicsContext.Execute();
                        RenderCount++;
                        deviceResources.Present(false);
                    }
                    if (performaceSettings.MultiThreadRendering)
                        RenderTask1 = Task.Run(_RenderFunction);
                    else
                        _RenderFunction();
                }
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

        public async Task WaitForResourcesLoadedAsync()
        {
            if (!renderPipelineContext.LoadTask.IsCompleted)
                await renderPipelineContext.LoadTask;
        }
        #region UI
        public StorageFolder openedStorageFolder;
        public event EventHandler OpenedStorageFolderChanged;
        public void OpenedStorageFolderChange(StorageFolder storageFolder)
        {
            openedStorageFolder = storageFolder;
            OpenedStorageFolderChanged?.Invoke(this, null);
        }
        public Frame frameViewProperties;
        public void ShowDetailPage(Type page, object parameter)
        {
            frameViewProperties.Navigate(page, parameter);
        }
        #endregion
    }

    public struct PerformaceSettings
    {
        public bool MultiThreadRendering;
        public bool SaveCpuPower;
        public bool HighResolutionShadow;
        public bool AutoReloadShaders;
        public bool AutoReloadTextures;
    }

    public struct Settings
    {
        public bool viewSelectedEntityBone;
        public Vector4 backgroundColor;
        public float ExtendShadowMapRange;
        public bool ZPrepass;
        public uint RenderStyle;
    }
}
