using System;
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


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.Parameter is Coocoo3DMain _appBody)
            {
                appBody = _appBody;
                appBody.FrameUpdated += FrameUpdated;
                _cachePos = appBody.camera.LookAtPoint;
                _cacheRot = appBody.camera.Angle;
                _cacheFOV = appBody.camera.Fov;
                _cacheDistance = appBody.camera.Distance;
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
            DateTime Now = DateTime.Now;
            if (Now - PrevUpdateTime > TimeSpan.FromSeconds(1))
            {
                ViewFrameRate.Text = string.Format("帧率：{0}", (TimeSpan.FromSeconds(1) / (Now - PrevUpdateTime) * (appBody.RenderCount - prevRenderCount)).ToString(".0"));
                PrevUpdateTime = Now;
                prevRenderCount = appBody.RenderCount;
            }
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
            get => _cacheDistance; set
            {
                _cacheDistance = value;
                appBody.camera.Distance = value;
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

        public bool VHighResolutionShadow
        {
            get
            {
                return appBody.settings.HighResolutionShadow;
            }
            set
            {
                appBody.settings.HighResolutionShadow = value;
                if (value == true)
                {
                    lock (appBody.deviceResources)
                    {
                        appBody.defaultResources.DepthStencil0.ReloadAsDepthStencil(appBody.deviceResources, 8192, 8192);
                        appBody.mainCaches.AddRenderTextureToUpdateList(appBody.defaultResources.DepthStencil0);
                    }
                }
                else
                {
                    lock (appBody.deviceResources)
                    {
                        appBody.defaultResources.DepthStencil0.ReloadAsDepthStencil(appBody.deviceResources, 4096, 4096);
                        appBody.mainCaches.AddRenderTextureToUpdateList(appBody.defaultResources.DepthStencil0);
                    }
                }
                appBody.RequireRender();
            }
        }
        #endregion
    }
}
