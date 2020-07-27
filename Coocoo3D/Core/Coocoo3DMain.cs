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
using Coocoo3DPhysics;
using System.Globalization;
using Coocoo3D.FileFormat;
using Coocoo3D.Components;

namespace Coocoo3D.Core
{
    ///<summary>是整个应用程序的上下文</summary>
    public class Coocoo3DMain
    {
        public DeviceResources deviceResources = new DeviceResources();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public DefaultResources defaultResources = new DefaultResources();
        public MainCaches mainCaches = new MainCaches();

        public MediaElement mediaElement;
        public Scene CurrentScene;

        private List<MMD3DEntity> Entities { get => CurrentScene.Entities; }
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
        public bool SaveCpuPower = true;
        public DateTime LatestUserOperating = DateTime.Now;

        public double PlayTime;
        public float PlaySpeed = 1.0f;
        public bool Playing;
        public DateTime LatestRenderTime = DateTime.Now;
        public TimeSpan FrameInterval = TimeSpan.FromSeconds(1 / 120.0);
        public float Fps = 120;
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
            ExtendShadowMapRange = 32,
            EnableAO = true,
            EnableShadow = true,
        };
        public Physics3D physics3D = new Physics3D();
        public Physics3DScene physics3DScene = new Physics3DScene();
        public float AspectRatio;
        IAsyncAction RenderLoop;
        public Coocoo3DMain()
        {
            _currentRenderPipeline = forwardRenderPipeline1;
            graphicsContext.Reload(deviceResources);
            defaultResources.LoadTask = Task.Run(async () =>
            {
                forwardRenderPipeline1.Reload(deviceResources);
                postProcess.Reload(deviceResources);
                if (deviceResources.IsRayTracingSupport())
                {
                    rayTracingRenderPipeline1.Reload(deviceResources);
                }
                await defaultResources.ReloadDefalutResources(deviceResources, mainCaches);
                await forwardRenderPipeline1.ReloadAssets(deviceResources);
                forwardRenderPipeline1.ChangeRenderTargetFormat(deviceResources, RTFormat);
                await postProcess.ReloadAssets(deviceResources);
                //widgetRenderer.Init(mainCaches, defaultResources, mainCaches.textureCaches);
                if (deviceResources.IsRayTracingSupport())
                {
                    await rayTracingRenderPipeline1.ReloadAssets(deviceResources);
                }
                RequireRender();
            });
            //PhysXAPI.SetAPIUsed(physics3D);
            BulletAPI.SetAPIUsed(physics3D);
            physics3D.Init();
            physics3DScene.Reload(physics3D);
            physics3DScene.SetGravitation(new Vector3(0, -98.01f, 0));

            CurrentScene = new Scene(this);
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(Tick, TimeSpan.FromSeconds(1 / 15.0));
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
                      if (SaveCpuPower)
                          System.Threading.Thread.Sleep(1);
                  }
              }, WorkItemPriority.Low, WorkItemOptions.TimeSliced);
        }
        #region Rendering
        RenderPipeline.ForwardRenderPipeline1 forwardRenderPipeline1 = new RenderPipeline.ForwardRenderPipeline1();
        RenderPipeline.RayTracingRenderPipeline1 rayTracingRenderPipeline1 = new RenderPipeline.RayTracingRenderPipeline1();
        public RenderPipeline.PostProcess postProcess = new RenderPipeline.PostProcess();
        public RenderPipeline.RenderPipeline CurrentRenderPipeline { get => _currentRenderPipeline; }
        RenderPipeline.RenderPipeline _currentRenderPipeline;
        public DxgiFormat RTFormat = DxgiFormat.DXGI_FORMAT_R16G16B16A16_UNORM;
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
                    Entities[i].SetMotionTime(playTime);
        }
        bool rendering = false;
        public void RequireRender(bool updateEntities = false)
        {
            NeedUpdateEntities |= updateEntities;
            NeedRender = true;
        }

        List<Texture2D> textureProcessing = new List<Texture2D>();
        List<RenderTexture2D> renderTextureProcessing = new List<RenderTexture2D>();
        List<MMDMesh> meshProcessing = new List<MMDMesh>();
        RenderPipeline.RenderPipelineContext renderPipelineContext = new RenderPipeline.RenderPipelineContext();

        public bool RequireResize;
        public Size NewSize;
        public bool swapChainReady;
        private bool RenderFrame2()
        {
            if (DateTime.Now - LatestRenderTime < FrameInterval || rendering)
            {
                NeedRender = true;
                return false;
            }

            rendering = true;
            NeedRender = false;
            DateTime now = DateTime.Now;
            double deltaTime = MathF.Min((float)(now - LatestRenderTime).TotalSeconds, 0.17f) * PlaySpeed;
            LatestRenderTime = now;
            lock (deviceResources)
            {
                #region Render Preparing
                mainCaches.textureLoadList.MoveTo_CC(textureProcessing);
                mainCaches.RenderTextureUpdateList.MoveTo_CC(renderTextureProcessing);
                mainCaches.mmdMeshLoadList.MoveTo_CC(meshProcessing);

                renderPipelineContext.ClearCollections();
                lock (CurrentScene)
                {
                    for (int i = 0; i < CurrentScene.EntityLoadList.Count; i++)
                    {
                        CurrentScene.Entities.Add(CurrentScene.EntityLoadList[i]);
                        var physics3DRigidBodys = CurrentScene.EntityLoadList[i].boneComponent.physics3DRigidBodys;
                        var rigidBodyDescs = CurrentScene.EntityLoadList[i].boneComponent.rigidBodyDescs;
                        for (int j = 0; j < rigidBodyDescs.Count; j++)
                        {
                            var desc = rigidBodyDescs[j];
                            physics3DScene.AddRigidBody(physics3DRigidBodys[j], desc.Position, MMDBoneComponent.ToQuaternion(desc.Rotation), desc.Dimemsions, desc.Mass, desc.Restitution, desc.Friction, desc.TranslateDamp, desc.RotateDamp, (byte)desc.Shape, (byte)desc.Type, desc.CollisionGroup, desc.CollisionMask);
                        }
                        var jointDescs = CurrentScene.EntityLoadList[i].boneComponent.jointDescs;
                        var joints = CurrentScene.EntityLoadList[i].boneComponent.physic3DJoints;
                        for (int j = 0; j < jointDescs.Count; j++)
                        {
                            var desc = jointDescs[j];
                            physics3DScene.AddJoint(joints[j], desc.Position, MMDBoneComponent.ToQuaternion(desc.Rotation), physics3DRigidBodys[desc.AssociatedRigidBodyIndex1], physics3DRigidBodys[desc.AssociatedRigidBodyIndex2],
                                desc.PositionMinimum, desc.PositionMaximum, desc.RotationMinimum, desc.RotationMaximum, desc.PositionSpring, desc.RotationSpring);
                        }
                    }
                    for (int i = 0; i < CurrentScene.LightingLoadList.Count; i++)
                    {
                        CurrentScene.Lightings.Add(CurrentScene.LightingLoadList[i]);
                    }
                    for (int i = 0; i < CurrentScene.EntityRemoveList.Count; i++)
                    {
                        var physics3DRigidBodys = CurrentScene.EntityRemoveList[i].boneComponent.physics3DRigidBodys;
                        for (int j = 0; j < physics3DRigidBodys.Count; j++)
                        {
                            physics3DScene.RemoveRigidBody(physics3DRigidBodys[j]);
                        }
                        var physics3DJoints = CurrentScene.EntityRemoveList[i].boneComponent.physic3DJoints;
                        for(int j=0;j< physics3DJoints.Count;j++)
                        {
                            physics3DScene.RemoveJoint(physics3DJoints[j]);
                        }
                    }
                    CurrentScene.EntityLoadList.Clear();
                    CurrentScene.LightingLoadList.Clear();
                    CurrentScene.EntityRemoveList.Clear();
                    renderPipelineContext.entities.AddRange(CurrentScene.Entities);
                    renderPipelineContext.lightings.AddRange(CurrentScene.Lightings);
                }
                var entities = renderPipelineContext.entities;
                for(int i=0;i<entities.Count;i++)
                {
                    var entity = entities[i];
                    if(entity.NeedTransform)
                    {
                        entity.NeedTransform = false;
                        entity.Position = entity.PositionNextFrame;
                        entity.Rotation = entity.RotationNextFrame;
                        entity.boneComponent.TransformToNew(physics3DScene, entity.Position, entity.Rotation);
                    }
                }
                camera.AspectRatio = AspectRatio;
                camera.Update();
                for (int i = 0; i < CurrentScene.Lightings.Count; i++)
                    CurrentScene.Lightings[i].UpdateLightingData(settings.ExtendShadowMapRange, camera);
                bool needUpdateEntities = NeedUpdateEntities;
                NeedUpdateEntities = false;
                double t1 = deltaTime;
                if (t1 >= 0)
                {
                    while (t1 > 1e-7f)
                    {
                        const float c_stepTime = 1 / 30.0f;
                        double t2 = t1 > c_stepTime ? c_stepTime : t1;
                        if (Playing)
                            PlayTime += t2;
                        if (Playing || needUpdateEntities)
                        {
                            UpdateEntities((float)PlayTime);
                        }
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                        }
                        physics3DScene.Simulate(t2);
                        t1 -= c_stepTime;
                        if (t1 < 0.0f)
                            t1 = 0.0f;
                        physics3DScene.FetchResults();
                    }
                }
                else
                {
                    if (Playing)
                        PlayTime += t1;
                    UpdateEntities((float)PlayTime);
                    for (int i = 0; i < entities.Count; i++)
                    {
                        entities[i].boneComponent.SetPhysicsPose(physics3DScene);
                    }
                    physics3DScene.Simulate(-t1);
                    physics3DScene.FetchResults();
                }
                for (int i = 0; i < entities.Count; i++)
                {
                    entities[i].boneComponent.SetPoseAfterPhysics(physics3DScene);
                }

                if (textureProcessing.Count > 0 || renderTextureProcessing.Count > 0 || meshProcessing.Count > 0 || RequireResize)
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
                    if (RequireResize)
                    {
                        RequireResize = false;
                        deviceResources.SetLogicalSize(NewSize);
                        int x = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Width), 1);
                        int y = Math.Max((int)Math.Round(deviceResources.GetOutputSize().Height), 1);
                        defaultResources.ScreenSizeRenderTextureOutput.ReloadAsRTVUAV(deviceResources, x, y, RTFormat);
                        renderTextureProcessing.Add(defaultResources.ScreenSizeRenderTextureOutput);
                        for (int i = 0; i < defaultResources.ScreenSizeRenderTextures.Length; i++)
                        {
                            defaultResources.ScreenSizeRenderTextures[i].ReloadAsRTVUAV(deviceResources, x, y, RTFormat);
                            renderTextureProcessing.Add(defaultResources.ScreenSizeRenderTextures[i]);
                        }
                        defaultResources.ScreenSizeDepthStencilOutput.ReloadAsDepthStencil(deviceResources, x, y);
                        renderTextureProcessing.Add(defaultResources.ScreenSizeDepthStencilOutput);
                    }
                    for (int i = 0; i < renderTextureProcessing.Count; i++)
                        graphicsContext.UpdateRenderTexture(renderTextureProcessing[i]);
                    for (int i = 0; i < textureProcessing.Count; i++)
                        textureProcessing[i].ReleaseUploadHeapResource();
                    for (int i = 0; i < meshProcessing.Count; i++)
                        meshProcessing[i].ReleaseUploadHeapResource();
                    textureProcessing.Clear();
                    meshProcessing.Clear();
                    renderTextureProcessing.Clear();
                }
                #endregion

                if (swapChainReady && !RequireResize)
                {
                    #region context preparing
                    renderPipelineContext.cameras.Add(camera);
                    renderPipelineContext.deviceResources = deviceResources;
                    renderPipelineContext.graphicsContext = graphicsContext;
                    renderPipelineContext.outputDSV = defaultResources.ScreenSizeDepthStencilOutput;
                    renderPipelineContext.outputRTV = defaultResources.ScreenSizeRenderTextureOutput;
                    //renderPipelineContext.scene = CurrentScene;
                    renderPipelineContext.TextureLoading = defaultResources.TextureLoading;
                    renderPipelineContext.TextureError = defaultResources.TextureError;
                    renderPipelineContext.settings = settings;
                    renderPipelineContext.ndcQuadMesh = defaultResources.quadMesh;
                    renderPipelineContext.DSV0 = defaultResources.DepthStencil0;
                    #endregion

                    GraphicsContext.BeginAlloctor(deviceResources);
                    graphicsContext.BeginCommand();
                    graphicsContext.BeginEvent();
                    graphicsContext.SetDescriptorHeapDefault();


                    for (int i = 0; i < entities.Count; i++)
                        entities[i].UpdateGpuResources(graphicsContext);

                    var currentRenderPipeline = _currentRenderPipeline;//避免在渲染时切换
                    if (Playing || needUpdateEntities)
                    {
                        currentRenderPipeline.TimeChange(PlayTime, deltaTime);
                    }
                    graphicsContext.ResourceBarrierScreen(D3D12ResourceStates._PRESENT, D3D12ResourceStates._RENDER_TARGET);
                    if (currentRenderPipeline.Ready)
                    {
                        currentRenderPipeline.PrepareRenderData(renderPipelineContext);
                        currentRenderPipeline.BeforeRenderCameras(renderPipelineContext);
                        currentRenderPipeline.RenderCamera(renderPipelineContext, 0);
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
                }
                rendering = false;
            }
            return true;
        }
        #endregion

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

        public void ForceAudioAsync() => AudioAsync(PlayTime, Playing);
        TimeSpan audioMaxInaccuracy = TimeSpan.FromSeconds(1.0 / 30.0);
        private void AudioAsync(double time, bool playing)
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
                    if (TimeSpan.FromSeconds(time) - mediaElement.Position > audioMaxInaccuracy ||
                        mediaElement.Position - TimeSpan.FromSeconds(time) > audioMaxInaccuracy)
                    {
                        mediaElement.Position = TimeSpan.FromSeconds(time);
                    }
                }
                else
                {
                    if (TimeSpan.FromSeconds(time) - mediaElement.Position > audioMaxInaccuracy ||
                           mediaElement.Position - TimeSpan.FromSeconds(time) > audioMaxInaccuracy)
                    {
                        mediaElement.Position = TimeSpan.FromSeconds(time);
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
        public bool EnableAO;
        public bool EnableShadow;
        public uint Quality;
    }
}
