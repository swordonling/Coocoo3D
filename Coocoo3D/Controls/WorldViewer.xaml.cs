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
using Coocoo3DGraphics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.Devices.Input;
using System.Numerics;
using Coocoo3D.Core;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace Coocoo3D.Controls
{
    public sealed partial class WorldViewer : UserControl
    {
        public Coocoo3DMain AppBody
        {
            get => _appBody;
            set { _appBody = value; SetupSwapChain(); }
        }
        Coocoo3DMain _appBody;

        public WorldViewer()
        {
            this.InitializeComponent();
        }

        private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppBody == null) return;
            SetupSwapChain();
        }
        private void SetupSwapChain()
        {
            if (!swapChainPanel.IsLoaded) return;
            if (_appBody == null) return;
            AppBody.GameDriverContext.AspectRatio = (float)(ActualWidth / ActualHeight);
            AppBody.deviceResources.SetSwapChainPanel(swapChainPanel);
            AppBody.GameDriverContext.NewSize = new Size(ActualWidth, ActualHeight);
            AppBody.GameDriverContext.RequireResizeOuter = true;
            AppBody.swapChainReady = true;
            AppBody.RequireRender();
            swapChainPanel.SizeChanged -= SwapChainPanel_SizeChanged;
            swapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AppBody.GameDriverContext.AspectRatio = (float)(ActualWidth / ActualHeight);
            AppBody.GameDriverContext.NewSize = e.NewSize;
            AppBody.GameDriverContext.RequireResizeOuter = true;
            AppBody.RequireRender();
        }

        private void InkCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            InkCanvas inkCanvas = sender as InkCanvas;
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen;
            inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += Canvas_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += Canvas_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += Canvas_PointerReleased;
            inkCanvas.PointerWheelChanged += InkCanvas_PointerWheelChanged;
        }

        private void Canvas_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            this.Focus(FocusState.Pointer);
            if (args.CurrentPoint.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                var PointerProperties = args.CurrentPoint.Properties;
                if (PointerProperties.IsRightButtonPressed)
                    MouseDevice.GetForCurrentView().MouseMoved += WorldViewer_MouseMoved_Rotate;
                else if (PointerProperties.IsMiddleButtonPressed)
                    MouseDevice.GetForCurrentView().MouseMoved += WorldViewer_MouseMoved_Drag;
                else return;
            }
        }

        private void WorldViewer_MouseMoved_Rotate(MouseDevice sender, MouseEventArgs args)
        {
            Vector3 delta = new Vector3();
            delta.X = -args.MouseDelta.Y;
            delta.Y = -args.MouseDelta.X;
            AppBody.camera.RotateDelta(delta / 200);
            AppBody.RequireRender();
        }

        private void WorldViewer_MouseMoved_Drag(MouseDevice sender, MouseEventArgs args)
        {
            Vector3 delta = new Vector3();
            delta.X = -args.MouseDelta.X;
            delta.Y = args.MouseDelta.Y;
            AppBody.camera.MoveDelta(delta / 50);
            AppBody.RequireRender();
        }

        private void Canvas_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {

        }

        private void Canvas_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            MouseDevice.GetForCurrentView().MouseMoved -= WorldViewer_MouseMoved_Rotate;
            MouseDevice.GetForCurrentView().MouseMoved -= WorldViewer_MouseMoved_Drag;
        }

        private void InkCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            AppBody.camera.Distance += e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta / 20.0f;
            AppBody.RequireRender();
        }
    }
}
