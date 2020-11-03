﻿using System;
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

        public DateTime LatestRenderTime = DateTime.Now;
        public float Fps = 240;
        public CoreDispatcher Dispatcher;
        public event EventHandler FrameUpdated;

        public volatile int RenderCount = 0;
        public volatile int VirtualRenderCount = 0;
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
            AutoReloadModels = true,
            VSync = false,
        };

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
            RPContext.deviceResources = deviceResources;
            RPContext.RPAssetsManager = RPAssetsManager;
            RPContext.LoadTask = Task.Run(async () =>
            {
                RPAssetsManager.Reload(deviceResources);
                forwardRenderPipeline1.Reload(deviceResources);
                postProcess.Reload(deviceResources);
                RPContext.SkinningMeshBuffer.Reload(deviceResources, 0);
                if (deviceResources.IsRayTracingSupport())
                    rayTracingRenderPipeline1.Reload(deviceResources);
                await RPContext.ReloadDefalutResources(wicFactory, ProcessingList, miscProcessContext);

                await RPAssetsManager.ReloadAssets();
                RPAssetsManager.ChangeRenderTargetFormat(deviceResources, ProcessingList, RPContext.RTFormat, RPContext.backBufferFormat);

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
                      if (performaceSettings.SaveCpuPower && (!performaceSettings.VSync || !actualRender))//开启VSync下不需要sleep，以免帧生成不均匀
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
        public void RequireRender(bool updateEntities)
        {
            GameDriverContext.NeedUpdateEntities |= updateEntities;
            GameDriverContext.NeedRender = true;
        }
        public void RequireRender()
        {
            GameDriverContext.NeedRender = true;
        }

        public ProcessingList ProcessingList = new ProcessingList();
        ProcessingList _processingList = new ProcessingList();
        public RenderPipeline.RenderPipelineContext RPContext = new RenderPipeline.RenderPipelineContext();

        public bool swapChainReady;
        //public long[] StopwatchTimes = new long[8];
        //System.Diagnostics.Stopwatch stopwatch1 = new System.Diagnostics.Stopwatch();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public GraphicsContext graphicsContext2 = new GraphicsContext();
        Task RenderTask1;
        private bool RenderFrame2()
        {
            if (!GameDriver.Next(GameDriverContext))
            {
                return false;
            }
            lock (deviceResources)
            {
                #region Render Preparing

                RPContext.dynamicContext1.ClearCollections();
                RPContext.dynamicContext1.frameRenderIndex = RPContext.frameRenderIndex;
                RPContext.dynamicContext1.EnableDisplay = GameDriverContext.EnableDisplay;
                RPContext.frameRenderIndex++;
                CurrentScene.DealProcessList(physics3DScene);
                lock (CurrentScene)
                {
                    RPContext.dynamicContext1.entities.AddRange(CurrentScene.Entities);
                    for (int i = 0; i < CurrentScene.Lightings.Count; i++)
                    {
                        RPContext.dynamicContext1.lightings.Add(CurrentScene.Lightings[i].GetLightingData());
                    }
                }

                var entities = RPContext.dynamicContext1.entities;
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
                RPContext.dynamicContext1.cameras.Add(camera.GetCameraData());
                RPContext.dynamicContext1.settings = settings;
                RPContext.dynamicContext1.inShaderSettings = inShaderSettings;
                RPContext.dynamicContext1.Preprocess();


                bool needUpdateEntities = GameDriverContext.NeedUpdateEntities;
                GameDriverContext.NeedUpdateEntities = false;

                void _ResetPhysics()
                {
                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.ResetPhysics(physics3DScene);
                    }
                    physics3DScene.Simulate(1 / 60.0);
                    physics3DScene.FetchResults();
                }

                double t1 = GameDriverContext.DeltaTime;
                void _BoneUpdate()
                {
                    UpdateEntities((float)GameDriverContext.PlayTime);

                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                    }
                    if (t1 >= 0)
                        physics3DScene.Simulate(t1);
                    else
                        physics3DScene.Simulate(-t1);

                    physics3DScene.FetchResults();
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
                    RPContext.dynamicContext1.Time = GameDriverContext.PlayTime;
                    RPContext.dynamicContext1.DeltaTime = GameDriverContext.DeltaTime;
                }
                else
                {
                    RPContext.dynamicContext1.Time = GameDriverContext.PlayTime;
                    RPContext.dynamicContext1.DeltaTime = 0;
                }

                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].boneComponent.WriteMatriticesData();
                }

                if (RenderTask1 != null && RenderTask1.Status != TaskStatus.RanToCompletion) RenderTask1.Wait();
                #region Render preparing
                var temp1 = RPContext.dynamicContext1;
                RPContext.dynamicContext1 = RPContext.dynamicContext;
                RPContext.dynamicContext = temp1;


                ProcessingList.MoveToAnother(_processingList);
                int SceneObjectVertexCount = RPContext.dynamicContext.GetSceneObjectVertexCount();
                if (!_processingList.IsEmpty() || GameDriverContext.RequireInterruptRender || SceneObjectVertexCount > RPContext.SkinningMeshBufferSize)
                {
                    GameDriverContext.RequireInterruptRender = false;
                    if (GameDriverContext.NeedReloadModel)
                    {
                        deviceResources.WaitForGpu();
                        GameDriverContext.NeedReloadModel = false;
                        ModelReloader.ReloadModels(CurrentScene, mainCaches, _processingList, GameDriverContext);
                    }
                    GraphicsContext.BeginAlloctor(deviceResources);
                    graphicsContext.BeginCommand();
                    RPContext.ChangeShadowMapsQuality(_processingList, performaceSettings.HighResolutionShadow);
                    if (GameDriverContext.RequireResize)
                    {
                        GameDriverContext.RequireResize = false;
                        deviceResources.SetLogicalSize(GameDriverContext.NewSize);

                        RPContext.ReloadTextureSizeResources(_processingList);
                    }
                    _processingList._DealStep1(graphicsContext);
                    graphicsContext.EndCommand();
                    graphicsContext.Execute();
                    deviceResources.WaitForGpu();

                    _processingList._DealStep2(graphicsContext, deviceResources);
                    RPAssetsManager._DealStep3(deviceResources, _processingList);
                    _processingList.Clear();
                    if (SceneObjectVertexCount > RPContext.SkinningMeshBufferSize)
                    {
                        RPContext.SkinningMeshBuffer.Reload(deviceResources, SceneObjectVertexCount);
                        RPContext.SkinningMeshBufferSize = SceneObjectVertexCount;
                        RPContext.LightCacheBuffer.Initialize(deviceResources, SceneObjectVertexCount * 16);
                    }
                }
                #endregion
                if (!RPContext.dynamicContext.EnableDisplay)
                {
                    VirtualRenderCount++;
                    return true;
                }

                GraphicsContext.BeginAlloctor(deviceResources);

                miscProcessContext.MoveToAnother(_miscProcessContext);
                _miscProcessContext.graphicsContext = graphicsContext2;
                miscProcess.Process(_miscProcessContext);

                if (swapChainReady)
                {
                    RPContext.graphicsContext = graphicsContext;

                    graphicsContext.BeginCommand();
                    graphicsContext.SetDescriptorHeapDefault();

                    var entities1 = RPContext.dynamicContext.entities;
                    for (int i = 0; i < entities1.Count; i++)
                    {
                        entities1[i].UpdateGpuResources(graphicsContext);
                    }

                    var currentRenderPipeline = _currentRenderPipeline;//避免在渲染时切换
                    currentRenderPipeline.PrepareRenderData(RPContext);
                    postProcess.PrepareRenderData(RPContext);
                    #endregion

                    void _RenderFunction()
                    {
                        graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._PRESENT, D3D12ResourceStates._RENDER_TARGET);
                        RPContext.UpdateGPUResource();
                        if (RPAssetsManager.Ready && currentRenderPipeline.Ready && postProcess.Ready)
                        {
                            currentRenderPipeline.RenderCamera(RPContext, 1);
                            postProcess.RenderCamera(RPContext, 1);
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
                        GameDriver.AfterRender(graphicsContext, GameDriverContext);
                        graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._RENDER_TARGET, D3D12ResourceStates._PRESENT);
                        graphicsContext.EndCommand();
                        graphicsContext.Execute();
                        deviceResources.Present(performaceSettings.VSync);
                        RenderCount++;
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
            if (!RPContext.LoadTask.IsCompleted)
                await RPContext.LoadTask;
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
        public bool AutoReloadModels;
        public bool VSync;
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
