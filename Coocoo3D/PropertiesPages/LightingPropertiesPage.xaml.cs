using Coocoo3D.Present;
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
    public sealed partial class LightingPropertiesPage : Page, INotifyPropertyChanged
    {
        public LightingPropertiesPage()
        {
            this.InitializeComponent();
        }

        Coocoo3DMain appBody;
        Lighting lighting;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.Parameter is Coocoo3DMain _appBody)
            {
                appBody = _appBody;
                lighting = _appBody.SelectedLighting[0];
                appBody.FrameUpdated += FrameUpdated;
                _cachePos = lighting.Rotation;
            }
            else
            {
                Frame.Navigate(typeof(ErrorPropertiesPage), "显示属性错误");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        PropertyChangedEventArgs eaVDX = new PropertyChangedEventArgs("VRX");//防止莫名其妙的gc
        PropertyChangedEventArgs eaVDY = new PropertyChangedEventArgs("VRY");
        PropertyChangedEventArgs eaVDZ = new PropertyChangedEventArgs("VRZ");
        PropertyChangedEventArgs eaVCR = new PropertyChangedEventArgs("VCR");
        PropertyChangedEventArgs eaVCG = new PropertyChangedEventArgs("VCG");
        PropertyChangedEventArgs eaVCB = new PropertyChangedEventArgs("VCB");
        PropertyChangedEventArgs eaVCA = new PropertyChangedEventArgs("VCA");

        private void FrameUpdated(object sender, EventArgs e)
        {
            if (_cachePos != lighting.Rotation)
            {
                _cachePos = lighting.Rotation;
                PropertyChanged?.Invoke(this, eaVDX);
                PropertyChanged?.Invoke(this, eaVDY);
                PropertyChanged?.Invoke(this, eaVDZ);
            }
            if (_cacheColor != lighting.Color)
            {
                _cacheColor = lighting.Color;
                PropertyChanged?.Invoke(this, eaVCR);
                PropertyChanged?.Invoke(this, eaVCG);
                PropertyChanged?.Invoke(this, eaVCB);
                PropertyChanged?.Invoke(this, eaVCA);
            }
        }

        public float VRX
        {
            get => _cachePos.X / MathF.PI * 180; set
            {
                _cachePos.X = value * MathF.PI / 180;
                UpdateDirectionFromUI();
            }
        }
        public float VRY
        {
            get => _cachePos.Y / MathF.PI * 180; set
            {
                _cachePos.Y = value * MathF.PI / 180;
                UpdateDirectionFromUI();
            }
        }
        public float VRZ
        {
            get => _cachePos.Z / MathF.PI * 180; set
            {
                _cachePos.Z = value * MathF.PI / 180;
                UpdateDirectionFromUI();
            }
        }
        Vector3 _cachePos;

        public float VCR
        {
            get => _cacheColor.X; set
            {
                _cacheColor.X = value;
                UpdateColorFromUI();
            }
        }
        public float VCG
        {
            get => _cacheColor.Y; set
            {
                _cacheColor.Y = value;
                UpdateColorFromUI();
            }
        }
        public float VCB
        {
            get => _cacheColor.Z; set
            {
                _cacheColor.Z = value;
                UpdateColorFromUI();
            }
        }
        public float VCA
        {
            get => _cacheColor.W; set
            {
                _cacheColor.W = value;
                UpdateColorFromUI();
            }
        }
        Vector4 _cacheColor;

        void UpdateDirectionFromUI()
        {
            lighting.Rotation = _cachePos;
            appBody.RequireRender();
        }

        void UpdateColorFromUI()
        {
            lighting.Color = _cacheColor;
            appBody.RequireRender();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (appBody != null)
            {
                appBody.FrameUpdated -= FrameUpdated;
            }
        }

        PropertyChangedEventArgs eaName = new PropertyChangedEventArgs("Name");
        public string vName
        {
            get => lighting.Name;
            set { lighting.Name = value; lighting.PropChange(eaName); }
        }
    }
}
