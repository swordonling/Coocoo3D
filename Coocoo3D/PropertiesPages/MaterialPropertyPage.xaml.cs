using Coocoo3D.Components;
using Coocoo3D.Core;
using Coocoo3D.Present;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Coocoo3D.PropertiesPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MaterialPropertyPage : Page, INotifyPropertyChanged
    {
        public MaterialPropertyPage()
        {
            this.InitializeComponent();
        }

        Coocoo3DMain appBody;
        MMD3DEntity mmd3dEntity;
        MMDMatLit matLit;

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.Parameter is Bundle_Main_Entity_Mat _bundle)
            {
                appBody = _bundle.main;
                mmd3dEntity = _bundle.entity;
                appBody.FrameUpdated += FrameUpdated;
                matLit = _bundle.matLit;
            }
            else
            {
                Frame.Navigate(typeof(ErrorPropertiesPage), "显示属性错误");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            appBody.FrameUpdated -= FrameUpdated;
        }

        private void FrameUpdated(object sender, EventArgs e)
        {

        }

        public float VDiffuseR
        {
            get => matLit.innerStruct.DiffuseColor.X;
            set
            {
                matLit.innerStruct.DiffuseColor.X = value;
                appBody.RequireRender();
            }
        }
        public float VDiffuseG
        {
            get => matLit.innerStruct.DiffuseColor.Y;
            set
            {
                matLit.innerStruct.DiffuseColor.Y = value;
                appBody.RequireRender();
            }
        }
        public float VDiffuseB
        {
            get => matLit.innerStruct.DiffuseColor.Z;
            set
            {
                matLit.innerStruct.DiffuseColor.Z = value;
                appBody.RequireRender();
            }
        }
        public float VDiffuseA
        {
            get => matLit.innerStruct.DiffuseColor.W;
            set
            {
                matLit.innerStruct.DiffuseColor.W = value;
                appBody.RequireRender();
            }
        }

        public float VSpecularR
        {
            get => matLit.innerStruct.SpecularColor.X;
            set
            {
                matLit.innerStruct.SpecularColor.X = value;
                appBody.RequireRender();
            }
        }
        public float VSpecularG
        {
            get => matLit.innerStruct.SpecularColor.Y;
            set
            {
                matLit.innerStruct.SpecularColor.Y = value;
                appBody.RequireRender();
            }
        }
        public float VSpecularB
        {
            get => matLit.innerStruct.SpecularColor.Z;
            set
            {
                matLit.innerStruct.SpecularColor.Z = value;
                appBody.RequireRender();
            }
        }
        public float VSpecularA
        {
            get => matLit.innerStruct.SpecularColor.W;
            set
            {
                matLit.innerStruct.SpecularColor.W = value;
                appBody.RequireRender();
            }
        }

        public float VAmbientR
        {
            get => matLit.innerStruct.AmbientColor.X;
            set
            {
                matLit.innerStruct.AmbientColor.X = value;
                appBody.RequireRender();
            }
        }
        public float VAmbientG
        {
            get => matLit.innerStruct.AmbientColor.Y;
            set
            {
                matLit.innerStruct.AmbientColor.Y = value;
                appBody.RequireRender();
            }
        }
        public float VAmbientB
        {
            get => matLit.innerStruct.AmbientColor.Z;
            set
            {
                matLit.innerStruct.AmbientColor.Z = value;
                appBody.RequireRender();
            }
        }
        public float VMetallic
        {
            get => matLit.innerStruct.Metallic;
            set
            {
                matLit.innerStruct.Metallic = value;
                appBody.RequireRender();
            }
        }
        public float VSmoothness
        {
            get => matLit.innerStruct.Smoothness;
            set
            {
                matLit.innerStruct.Smoothness = value;
                appBody.RequireRender();
            }
        }
        public float VEmission
        {
            get => matLit.innerStruct.Emission;
            set
            {
                matLit.innerStruct.Emission = value;
                appBody.RequireRender();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
