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
using Coocoo3D.Present;
using System.ComponentModel;
using System.Numerics;
using Coocoo3D.Core;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Coocoo3D.PropertiesPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EntityPropertiesPage : Page, INotifyPropertyChanged
    {
        public EntityPropertiesPage()
        {
            this.InitializeComponent();
        }
        Coocoo3DMain appBody;
        MMD3DEntity mmd3dEntity;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.Parameter is Coocoo3DMain _appBody)
            {
                appBody = _appBody;
                mmd3dEntity = _appBody.SelectedEntities[0];
                appBody.FrameUpdated += FrameUpdated;
                _cacheP = mmd3dEntity.Position;
                _cacheR = QuaternionToEularYXZ(mmd3dEntity.Rotation);
            }
            else
            {
                Frame.Navigate(typeof(ErrorPropertiesPage), "显示属性错误");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        PropertyChangedEventArgs eaVPX = new PropertyChangedEventArgs("VPX");//防止莫名其妙的gc
        PropertyChangedEventArgs eaVPY = new PropertyChangedEventArgs("VPY");
        PropertyChangedEventArgs eaVPZ = new PropertyChangedEventArgs("VPZ");
        PropertyChangedEventArgs eaVRX = new PropertyChangedEventArgs("VRX");
        PropertyChangedEventArgs eaVRY = new PropertyChangedEventArgs("VRY");
        PropertyChangedEventArgs eaVRZ = new PropertyChangedEventArgs("VRZ");
        private void FrameUpdated(object sender, EventArgs e)
        {
            if (_cacheP != mmd3dEntity.Position)
            {
                _cacheP = mmd3dEntity.Position;
                PropertyChanged?.Invoke(this, eaVPX);
                PropertyChanged?.Invoke(this, eaVPY);
                PropertyChanged?.Invoke(this, eaVPZ);
            }
            if (_cacheRQ != mmd3dEntity.Rotation)
            {
                _cacheRQ = mmd3dEntity.Rotation;
                _cacheR = QuaternionToEularYXZ(_cacheRQ) * 180 / MathF.PI;
                PropertyChanged?.Invoke(this, eaVRX);
                PropertyChanged?.Invoke(this, eaVRY);
                PropertyChanged?.Invoke(this, eaVRZ);
            }
        }

        public float VPX
        {
            get => _cacheP.X; set
            {
                _cacheP.X = value;
                UpdatePositionFromUI();
            }
        }
        public float VPY
        {
            get => _cacheP.Y; set
            {
                _cacheP.Y = value;
                UpdatePositionFromUI();
            }
        }
        public float VPZ
        {
            get => _cacheP.Z; set
            {
                _cacheP.Z = value;
                UpdatePositionFromUI();
            }
        }
        Vector3 _cacheP;

        public float VRX
        {
            get => _cacheR.X; set
            {
                _cacheR.X = value;
                UpdateRotationFromUI();
            }
        }
        public float VRY
        {
            get => _cacheR.Y; set
            {
                _cacheR.Y = value;
                UpdateRotationFromUI();
            }
        }
        public float VRZ
        {
            get => -_cacheR.Z; set
            {
                _cacheR.Z = -value;
                UpdateRotationFromUI();
            }
        }
        Vector3 _cacheR;
        Quaternion _cacheRQ;

        void UpdatePositionFromUI()
        {
            mmd3dEntity.Position = _cacheP;
            appBody.RenderFrame();
        }
        void UpdateRotationFromUI()
        {
            _cacheRQ = EularToQuaternionYXZ(_cacheR / 180 * MathF.PI);
            mmd3dEntity.Rotation = _cacheRQ;
            appBody.RenderFrame();
        }

        PropertyChangedEventArgs eaName = new PropertyChangedEventArgs("Name");
        public string vName
        {
            get => mmd3dEntity.Name;
            set { mmd3dEntity.Name = value; mmd3dEntity.PropChange(eaName); }
        }
        public string vDesc
        {
            get => mmd3dEntity.Description;
            set { mmd3dEntity.Description = value; }
        }
        public string vModelInfo
        {
            get
            {
                return string.Format("顶点数：{0}\n三角形数：{1}\n骨骼数：{2}\n",
                    mmd3dEntity.rendererComponent.mesh.m_vertexCount, mmd3dEntity.rendererComponent.mesh.m_indexCount / 3, mmd3dEntity.boneComponent.bones.Count);
            }
        }


        static Vector3 QuaternionToEularYXZ(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Asin(2.0 * (ei - jk));
            result.Y = (float)Math.Atan2(2.0 * (ej + ik), 1 - 2.0 * (ii + jj));
            result.Z = (float)Math.Atan2(2.0 * (ek + ij), 1 - 2.0 * (ii + kk));
            return result;
        }
        static Quaternion EularToQuaternionYXZ(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5);
            double sx = Math.Sin(euler.X * 0.5);
            double cy = Math.Cos(euler.Y * 0.5);
            double sy = Math.Sin(euler.Y * 0.5);
            double cz = Math.Cos(euler.Z * 0.5);
            double sz = Math.Sin(euler.Z * 0.5);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (appBody != null)
            {
                appBody.FrameUpdated -= FrameUpdated;
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue("ExtName", out object object1))
            {
                string extName = object1 as string;
                if (extName != null)
                {
                    e.DataView.Properties.TryGetValue("File", out object object2);
                    StorageFile storageFile = object2 as StorageFile;
                    e.DataView.Properties.TryGetValue("Folder", out object object3);
                    StorageFolder storageFolder = object3 as StorageFolder;
                    if (".vmd".Equals(extName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        await UI.UISharedCode.LoadVMD(appBody, storageFile, mmd3dEntity);
                    }
                    if (".ccshader".Equals(extName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        await UI.UISharedCode.LoadShaderForEntities(appBody, storageFile, storageFolder, new MMD3DEntity[] { mmd3dEntity });
                    }
                }
            }
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue("ExtName", out object object1))
            {
                string extName = object1 as string;
                if (extName != null)
                {
                    if (".vmd".Equals(extName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (".ccshader".Equals(extName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
            }
        }
    }
}
