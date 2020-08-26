﻿using Coocoo3D.Core;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Coocoo3D.PropertiesPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SkyBoxPage : Page
    {
        public SkyBoxPage()
        {
            this.InitializeComponent();
        }
        Coocoo3DMain appBody;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Coocoo3DMain _appBody)
            {
                appBody = _appBody;
            }
            else
            {
                Frame.Navigate(typeof(ErrorPropertiesPage), "显示属性错误");
            }
        }
        private bool StrEq(string a, string b)
        {
            return a.Equals(b, StringComparison.CurrentCultureIgnoreCase);
        }
        private bool IsImageExtName(string extName)
        {
            return StrEq(".jpg", extName) || StrEq(".jpeg", extName) || StrEq(".png", extName) || StrEq(".bmp", extName) || StrEq(".tif", extName) || StrEq(".tiff", extName) || StrEq(".gif", extName);
        }
        private void _img0_DragOver(object sender, DragEventArgs e)
        {
            Image image = sender as Image;
            if (e.DataView.Properties.TryGetValue("ExtName", out object object1))
            {
                string extName = object1 as string;
                if (extName != null)
                {
                    if (IsImageExtName(extName))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
            }
        }
        StorageFile[] files = new StorageFile[6];
        int2[] imgSize = new int2[6];
        struct int2
        {
            public int x;
            public int y;
        }
        int prevRenderFrame = 0;
        private async Task Task1(RenderPipeline.MiscProcessType miscProcessType)
        {
            for (int i = 0; i < 6; i++)
            {
                if (files[i] == null)
                {
                    showInfo.Text = "天空盒图片未完全填充";
                    return;
                }
            }
            showInfo.Text = "正在处理";
            appBody.defaultResources.EnvironmentCube.ReloadFromImage(appBody.wicFactory, imgSize[0].x, imgSize[0].y,
                await FileIO.ReadBufferAsync(files[0]),
                await FileIO.ReadBufferAsync(files[1]),
                await FileIO.ReadBufferAsync(files[2]),
                await FileIO.ReadBufferAsync(files[3]),
                await FileIO.ReadBufferAsync(files[4]),
                await FileIO.ReadBufferAsync(files[5]));
            int t1 = appBody.RenderCount;
            if (prevRenderFrame == t1)
            {

            }
            prevRenderFrame = t1;
            appBody.ProcessingList.AddObject(appBody.defaultResources.EnvironmentCube);
            appBody.miscProcessContext.Add(new RenderPipeline.MiscProcessPair<TextureCube, RenderTextureCube>(appBody.defaultResources.EnvironmentCube, appBody.defaultResources.IrradianceMap, miscProcessType));
            appBody.RequireRender();
            showInfo.Text = "操作完成";
        }
        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            await Task1(RenderPipeline.MiscProcessType.GenerateIrradianceMap);
        }

        private async void Apply1_Click(object sender, RoutedEventArgs e)
        {
            await Task1(RenderPipeline.MiscProcessType.GenerateIrradianceMapQ1);

        }

        private async void _img0_Drop(object sender, DragEventArgs e)
        {
            Image image = sender as Image;
            if (e.DataView.Properties.TryGetValue("ExtName", out object object1))
            {
                string extName = object1 as string;
                if (extName != null)
                {
                    e.DataView.Properties.TryGetValue("File", out object object2);
                    StorageFile storageFile = object2 as StorageFile;
                    e.DataView.Properties.TryGetValue("Folder", out object object3);
                    StorageFolder storageFolder = object3 as StorageFolder;
                    if (IsImageExtName(extName))
                    {
                        var bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(await storageFile.OpenReadAsync());
                        image.Source = bitmap;
                        if (int.TryParse(image.Tag as string, out int i))
                        {
                            files[i] = storageFile;
                            imgSize[i].x = bitmap.PixelWidth;
                            imgSize[i].y = bitmap.PixelHeight;
                        }
                    }
                }
            }
        }

        public float VSkyBoxMultiple
        {
            get => appBody.settings.SkyBoxLightMultiple;
            set
            {
                appBody.settings.SkyBoxLightMultiple = value;
                appBody.RequireRender();
            }
        }
    }
}