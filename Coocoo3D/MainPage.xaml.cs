﻿using System;
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
            appBody = new Coocoo3DMain();
            worldViewer.AppBody = appBody;
            appBody.FrameUpdated += AppBody_FrameUpdated;
        }

        private void AppBody_FrameUpdated(object sender, EventArgs e)
        {
            ForceAudioAsync();
        }

        public void ForceAudioAsync() => AudioAsync(appBody.GameDriverContext.PlayTime, appBody.GameDriverContext.Playing);
        TimeSpan audioMaxInaccuracy = TimeSpan.FromSeconds(1.0 / 30.0);
        private void AudioAsync(double time, bool playing)
        {
            if (playing && appBody.GameDriverContext.PlaySpeed == 1.0f)
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

        private void AddPage(TabView tabView, string header, Type pageType, object navParam)
        {
            Frame frame1 = new Frame();
            frame1.Navigate(pageType, navParam);
            tabView.TabItems.Add(new TabViewItem()
            {
                Header = header,
                Content = frame1,
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            AddPage(tabViewL1, "通常", typeof(PropertiesPages.CommonPage), appBody);
            AddPage(tabViewL1, "天空盒", typeof(PropertiesPages.SkyBoxPage), appBody);
            AddPage(tabViewL1, "录制", typeof(PropertiesPages.RecordPage), appBody);
            AddPage(tabViewR1, "场景", typeof(PropertiesPages.ScenePage), appBody);
            AddPage(tabViewR1, "后处理", typeof(PropertiesPages.PostProcessPage), appBody);
            AddPage(tabViewB1, "资源", typeof(PropertiesPages.ResourcesPage), appBody);

            Frame frame1 = new Frame();
            frame1.Navigate(typeof(PropertiesPages.EmptyPropertiesPage));
            appBody.frameViewProperties = frame1;
            tabViewR2.TabItems.Add(new TabViewItem()
            {
                Header = "细节",
                Content = frame1,
            });
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
            ForceAudioAsync();

        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            UI.UISharedCode.Pause(appBody);
            ForceAudioAsync();
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            UI.UISharedCode.Stop(appBody);
            ForceAudioAsync();
        }
        private void Rewind_Click(object sender, RoutedEventArgs e)
        {
            appBody.GameDriverContext.Playing = true;
            appBody.GameDriverContext.PlaySpeed = -2.0f;
            ForceAudioAsync();
        }
        private void FastForward_Click(object sender, RoutedEventArgs e)
        {
            appBody.GameDriverContext.Playing = true;
            appBody.GameDriverContext.PlaySpeed = 2.0f;
            ForceAudioAsync();
        }
        private void Front_Click(object sender, RoutedEventArgs e)
        {
            if (appBody.Recording)
            {
                appBody.GameDriver = appBody._GeneralGameDriver;
                appBody.Recording = false;
            }
            appBody.GameDriverContext.PlayTime = 0;
            appBody.RequireRender(true);
        }
        private void Rear_Click(object sender, RoutedEventArgs e)
        {
            if (appBody.Recording)
            {
                appBody.GameDriver = appBody._GeneralGameDriver;
                appBody.Recording = false;
            }
            appBody.GameDriverContext.PlayTime = 9999;
            appBody.RequireRender(true);
        }
        private async void Record_Click(object sender, RoutedEventArgs e)
        {
            if (!appBody.Recording)
            {
                FolderPicker folderPicker = new FolderPicker()
                {
                    FileTypeFilter =
                    {
                        "*"
                    },
                    SuggestedStartLocation = PickerLocationId.VideosLibrary,
                    ViewMode = PickerViewMode.Thumbnail,
                    SettingsIdentifier = "RecordFolder",
                };
                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null) return;
                appBody._RecorderGameDriver.saveFolder = folder;
                appBody._RecorderGameDriver.SwitchEffect();
                appBody.GameDriver = appBody._RecorderGameDriver;
                appBody.Recording = true;
            }
            else
            {
                appBody.GameDriver = appBody._GeneralGameDriver;
                appBody.Recording = false;
            }
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

        private void worldViewer_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
