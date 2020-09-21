﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Coocoo3D.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Coocoo3D.FileFormat;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Coocoo3D.PropertiesPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CommonPage : Page, INotifyPropertyChanged
    {
        public CommonPage()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        Coocoo3DMain appBody;

        uint[] comboBox1Values = new uint[6];
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Coocoo3DMain _appBody)
            {
                appBody = _appBody;
                appBody.FrameUpdated += FrameUpdated;
                _cachePos = appBody.camera.LookAtPoint;
                _cacheRot = appBody.camera.Angle;
                _cacheFOV = appBody.camera.Fov;
                _cacheDistance = appBody.camera.Distance;
                if (appBody.deviceResources.IsRayTracingSupport())
                {
                    VRayTracingSupport.Text = "使用的显卡支持光线追踪";
                }
                for (int i = 0; i < comboBox1Values.Length; i++)
                {
                    comboBox1Values[i] = (uint)i;
                }
                vQuality.ItemsSource = comboBox1Values;
            }
            else
            {
                Frame.Navigate(typeof(ErrorPropertiesPage), "显示属性错误");
            }
        }

        #region view property
        PropertyChangedEventArgs eaVPX = new PropertyChangedEventArgs("VPX");//防止莫名其妙的gc
        PropertyChangedEventArgs eaVPY = new PropertyChangedEventArgs("VPY");
        PropertyChangedEventArgs eaVPZ = new PropertyChangedEventArgs("VPZ");
        PropertyChangedEventArgs eaVRX = new PropertyChangedEventArgs("VRX");
        PropertyChangedEventArgs eaVRY = new PropertyChangedEventArgs("VRY");
        PropertyChangedEventArgs eaVRZ = new PropertyChangedEventArgs("VRZ");
        PropertyChangedEventArgs eaVFOV = new PropertyChangedEventArgs("VFOV");
        PropertyChangedEventArgs eaVD = new PropertyChangedEventArgs("VD");
        PropertyChangedEventArgs eaVCameraMotionOn = new PropertyChangedEventArgs("VCameraMotionOn");
        //long[] txs = new long[8];
        private void FrameUpdated(object sender, EventArgs e)
        {
            if (_cachePos != appBody.camera.LookAtPoint)
            {
                _cachePos = appBody.camera.LookAtPoint;
                PropertyChanged?.Invoke(this, eaVPX);
                PropertyChanged?.Invoke(this, eaVPY);
                PropertyChanged?.Invoke(this, eaVPZ);
            }
            if (_cacheRot != appBody.camera.Angle)
            {
                _cacheRot = appBody.camera.Angle;
                PropertyChanged?.Invoke(this, eaVRX);
                PropertyChanged?.Invoke(this, eaVRY);
                PropertyChanged?.Invoke(this, eaVRZ);
            }
            if (_cacheFOV != appBody.camera.Fov)
            {
                _cacheFOV = appBody.camera.Fov;
                PropertyChanged?.Invoke(this, eaVFOV);
            }
            if (_cacheFOV != appBody.camera.Distance)
            {
                _cacheDistance = appBody.camera.Distance;
                PropertyChanged?.Invoke(this, eaVD);
            }
            if (_cacheCameraMotionOn != appBody.camera.CameraMotionOn)
            {
                _cacheCameraMotionOn = appBody.camera.CameraMotionOn;
                PropertyChanged?.Invoke(this, eaVCameraMotionOn);
            }
            DateTime Now = DateTime.Now;
            if (Now - PrevUpdateTime > TimeSpan.FromSeconds(1))
            {
                ViewFrameRate.Text = string.Format("帧率：{0}", (TimeSpan.FromSeconds(1) / (Now - PrevUpdateTime) * (appBody.RenderCount - prevRenderCount)).ToString(".0"));
                PrevUpdateTime = Now;
                prevRenderCount = appBody.RenderCount;
            }
            //Array.Copy(appBody.StopwatchTimes, txs, appBody.StopwatchTimes.Length);
            //showt1.Text = txs[0].ToString();
            //showt2.Text = txs[1].ToString();
            //showt3.Text = txs[2].ToString();
            //showt4.Text = txs[3].ToString();
            //showt5.Text = txs[4].ToString();
        }
        int prevRenderCount = 0;
        DateTime PrevUpdateTime = DateTime.Now;

        public float VPX
        {
            get => _cachePos.X; set
            {
                _cachePos.X = value;
                UpdatePositionFromUI();
            }
        }
        public float VPY
        {
            get => _cachePos.Y; set
            {
                _cachePos.Y = value;
                UpdatePositionFromUI();
            }
        }
        public float VPZ
        {
            get => _cachePos.Z; set
            {
                _cachePos.Z = value;
                UpdatePositionFromUI();
            }
        }
        Vector3 _cachePos;

        public float VRX
        {
            get => _cacheRot.X * 180.0f / MathF.PI; set
            {
                _cacheRot.X = value * MathF.PI / 180.0f;
                UpdateRotationFromUI();
            }
        }
        public float VRY
        {
            get => _cacheRot.Y * 180.0f / MathF.PI; set
            {
                _cacheRot.Y = value * MathF.PI / 180.0f;
                UpdateRotationFromUI();
            }
        }
        public float VRZ
        {
            get => _cacheRot.Z * 180.0f / MathF.PI; set
            {
                _cacheRot.Z = value * MathF.PI / 180.0f;
                UpdateRotationFromUI();
            }
        }
        Vector3 _cacheRot;
        public float VFOV
        {
            get => _cacheFOV * 180.0f / MathF.PI; set
            {
                _cacheFOV = value * MathF.PI / 180.0f;
                appBody.camera.Fov = value * MathF.PI / 180.0f;
                appBody.RequireRender();
            }
        }
        float _cacheFOV;
        public float VD
        {
            get => -_cacheDistance; set
            {
                _cacheDistance = -value;
                appBody.camera.Distance = -value;
                appBody.RequireRender();
            }
        }
        float _cacheDistance;
        void UpdatePositionFromUI()
        {
            appBody.camera.LookAtPoint = _cachePos;
            appBody.RequireRender();
        }
        void UpdateRotationFromUI()
        {
            appBody.camera.Angle = _cacheRot;
            appBody.RequireRender();
        }

        public bool VViewBone
        {
            get
            {
                return appBody.settings.viewSelectedEntityBone;
            }
            set
            {
                appBody.settings.viewSelectedEntityBone = value;
                appBody.RequireRender();
            }
        }

        public float VSetFps
        {
            get => appBody.Fps; set
            {
                appBody.Fps = Math.Max(value, 1);
                appBody.GameDriverContext.FrameInterval = TimeSpan.FromSeconds(1 / appBody.Fps);
            }
        }

        public bool VSaveCpuPower
        {
            get => appBody.performaceSettings.SaveCpuPower; set
            {
                appBody.performaceSettings.SaveCpuPower = value;
            }
        }

        public bool VMultiThreadRendering
        {
            get => appBody.performaceSettings.MultiThreadRendering; set
            {
                appBody.performaceSettings.MultiThreadRendering = value;
            }
        }

        public bool VHighResolutionShadow
        {
            get
            {
                return appBody.performaceSettings.HighResolutionShadow;
            }
            set
            {
                appBody.performaceSettings.HighResolutionShadow = value;
                appBody.GameDriverContext.RequireInterruptRender = true;
                appBody.RequireRender();
            }
        }
        public bool VZPrepass
        {
            get => appBody.settings.ZPrepass; set
            {
                appBody.settings.ZPrepass = value;
                appBody.RequireRender();
            }
        }
        public bool VEnableAO
        {
            get => appBody.inShaderSettings.EnableAO; set
            {
                appBody.inShaderSettings.EnableAO = value;
                appBody.RequireRender();
            }
        }
        public bool VEnableShadow
        {
            get => appBody.inShaderSettings.EnableShadow; set
            {
                appBody.inShaderSettings.EnableShadow = value;
                appBody.RequireRender();
            }
        }
        public bool VCameraMotionOn
        {
            get => appBody.camera.CameraMotionOn; set
            {
                appBody.camera.CameraMotionOn = value;
                appBody.RequireRender();
            }
        }
        bool _cacheCameraMotionOn;
        #endregion

        private void VRenderPipeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (appBody == null) return;
            int selectedIndex = (sender as ComboBox).SelectedIndex;
            if (!appBody.deviceResources.IsRayTracingSupport() && selectedIndex == 1)
            {
                (sender as ComboBox).SelectedIndex = 0;
            }
            else
            {
                appBody.SwitchToRenderPipeline(selectedIndex);
            }
            appBody.RequireRender();
        }

        private void VQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            appBody.inShaderSettings.Quality = (uint)(sender as ComboBox).SelectedValue;
            appBody.RequireRender();
        }

        private void VRenderStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (appBody == null) return;
            appBody.settings.RenderStyle = (uint)(sender as ComboBox).SelectedIndex;
            appBody.RequireRender();
        }

        private void PhysicsReset_Click(object sender, RoutedEventArgs e)
        {
            appBody.GameDriverContext.RequireResetPhysics = true;
            appBody.RequireRender(true);
        }

        private void NewFun_Click(object sender, RoutedEventArgs e)
        {
            appBody.UseNewFun = !appBody.UseNewFun;
        }

        private bool StrEq(string a, string b)
        {
            return a.Equals(b, StringComparison.CurrentCultureIgnoreCase);
        }
        private bool IsImageExtName(string extName)
        {
            return StrEq(".vmd", extName);
        }
        private void Page_DragOver(object sender, DragEventArgs e)
        {
            Image image = sender as Image;
            if (e.DataView.Properties.TryGetValue("ExtName", out object object1))
            {
                string extName = object1 as string;
                if (extName != null && IsImageExtName(extName))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Properties.TryGetValue("ExtName", out object object1)) return;
            string extName = object1 as string;
            if (extName != null)
            {
                e.DataView.Properties.TryGetValue("File", out object object2);
                StorageFile storageFile = object2 as StorageFile;
                e.DataView.Properties.TryGetValue("Folder", out object object3);
                StorageFolder storageFolder = object3 as StorageFolder;
                if (StrEq(".vmd", extName))
                {
                    VMDFormat motionFile = new VMDFormat();
                    motionFile.Reload(new BinaryReader(await storageFile.OpenStreamForReadAsync()));
                    appBody.camera.cameraMotion.cameraKeyFrames = motionFile.CameraKeyFrames;
                    vCameraMotionOn.IsEnabled = true;
                    appBody.camera.CameraMotionOn = true;
                }
            }
        }

        private void ShaderCacheReset_Click(object sender, RoutedEventArgs e)
        {
            appBody.mainCaches.ResetShaderCache();
        }

        private void TextureCacheReset_Click(object sender, RoutedEventArgs e)
        {
            appBody.mainCaches.ResetTextureCache();
        }
    }
}