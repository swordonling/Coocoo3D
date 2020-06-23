using System;
using System.Collections.Generic;
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
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Controls;
using Coocoo3DGraphics;
using Coocoo3D.FileFormat;
using Coocoo3D.Core;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Coocoo3D
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Coocoo3DMain appBody;
        public MainPage()
        {
            this.InitializeComponent();
            appBody = new Coocoo3DMain()
            {
                worldViewer = worldViewer,
                mediaElement = mediaElement,
            };
            worldViewer.AppBody = appBody;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Frame frame1 = new Frame();
            frame1.Navigate(typeof(PropertiesPages.CommonPage), appBody);
            tabViewL1.TabItems.Add(new TabViewItem()
            {
                Header = "通常",
                Content = frame1,
            });

            frame1 = new Frame();
            frame1.Navigate(typeof(PropertiesPages.ScenePage), appBody);
            tabViewR1.TabItems.Add(new TabViewItem()
            {
                Header = "场景",
                Content = frame1,
            });

            frame1 = new Frame();
            frame1.Navigate(typeof(PropertiesPages.ResourcesPage), appBody);
            tabViewB1.TabItems.Add(new TabViewItem()
            {
                Header = "资源",
                Content = frame1,
            });


            frame1 = new Frame();
            frame1.Navigate(typeof(PropertiesPages.EmptyPropertiesPage));
            appBody.frameViewProperties = frame1;
            tabViewR2.TabItems.Add(new TabViewItem()
            {
                Header = "细节",
                Content = frame1,
            });
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            Stream texStream = (await file.OpenReadAsync()).AsStreamForRead();
            byte[] texBytes = new byte[texStream.Length];
            texStream.Read(texBytes, 0, (int)texStream.Length);
            var tex = Texture2D.LoadFromImage(appBody.deviceResources, texBytes);
        }
        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            await UI.UISharedCode.OpenResourceFolder(appBody);
        }
        private async void OpenMedia_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker mediaPicker = new FileOpenPicker
            {
                FileTypeFilter =
                {
                    ".mp3",
                    ".m4a",
                    ".wav",
                    ".mp4",
                },
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SettingsIdentifier = "media",
            };
            var file = await mediaPicker.PickSingleFileAsync();
            if (file == null) return;
            mediaElement.SetSource(await file.OpenReadAsync(), "");
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UI.UISharedCode.Play(appBody);
        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            UI.UISharedCode.Pause(appBody);
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            UI.UISharedCode.Stop(appBody);
        }
        private void Rewind_Click(object sender, RoutedEventArgs e)
        {
            appBody.Playing = true;
            appBody.PlaySpeed = -2.0f;
            appBody.ForceAudioAsync();
        }
        private void FastForward_Click(object sender, RoutedEventArgs e)
        {
            appBody.Playing = true;
            appBody.PlaySpeed = 2.0f;
            appBody.ForceAudioAsync();
        }
        private void Front_Click(object sender, RoutedEventArgs e)
        {
            appBody.PlayTime = 0;
            appBody.RenderFrame(true);
        }
        private void Rear_Click(object sender, RoutedEventArgs e)
        {
            appBody.PlayTime = 9999;
            appBody.RenderFrame(true);
        }


        private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var x = args.Tab;
            args.Data.Properties.Add("Tab", x);
            args.Data.Properties.Add("Owner", sender);
        }

        private void TabView_DragOver(object sender, DragEventArgs e)
        {
            var container = (sender as TabView);
            if (e.DataView.Properties.TryGetValue("Owner", out object ownerData) &&
                container != ownerData)
            {
                if (e.DataView.Properties.TryGetValue("Tab", out object tab))
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                }
            }
        }

        private void TabView_Drop(object sender, DragEventArgs e)
        {
            var container = (sender as TabView);
            if (e.DataView.Properties.TryGetValue("Owner", out object ownerData) &&
                container != ownerData)
            {
                if (e.DataView.Properties.TryGetValue("Tab", out object tab))
                {
                    (ownerData as TabView).TabItems.Remove(tab);
                    container.TabItems.Add(tab);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            appBody.ShowDetailPage(typeof(PropertiesPages.SoftwareInfoPropertiesPage), appBody);
        }
    }
}
