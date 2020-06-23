using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Windows.Storage;

namespace Coocoo3D.FileFormat
{
    /// <summary>是上下文</summary>
    public class CCSceneFormat
    {
        #region constants
        const string c_sCCScene = "CCScene";
        const string c_sResources = "Resources";
        const string c_sRecordedTime = "RecordedTime";
        const string c_sRecordedTimeData = "RecordedTimeData";
        const string c_sFile = "File";
        const string c_sPath = "Path";
        const string c_sDisplayName = "DisplayName";
        const string c_sGuid = "Guid";

        #endregion
        public StorageFolder storageFolder;
        public StorageFile storageFile;
        public List<CCResouce> resourceList = new List<CCResouce>();
        public Dictionary<Guid, CCResouce> resourceTable = new Dictionary<Guid, CCResouce>();
        public DateTime LastUpdateTime = DateTime.MinValue;
        public string Name;
        public string RelatedFolderPath;

        static XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
        {
            IgnoreComments = true,
        };
        static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
        };
        public static async Task<CCSceneFormat> Load(StorageFolder storageFolder, string Name)
        {
            var ccScene = new CCSceneFormat();
            ccScene.Name = Name;
            ccScene.storageFolder = storageFolder;
            ccScene.storageFile = await storageFolder.TryGetItemAsync(Name) as StorageFile;
            ccScene.RelatedFolderPath = storageFolder.Path;

            await ccScene.Reload();
            return ccScene;
        }
        public async Task Reload()
        {
            resourceList.Clear();
            if (storageFile == null)
            {
                storageFile = await storageFolder.CreateFileAsync(Name);
                await SaveFile();
            }
            if (LastUpdateTime == DateTime.MinValue)
            {
                await LoadFile();
                LastUpdateTime = DateTime.Now;
            }
        }
        public async Task LoadFile()
        {
            XmlReader reader = XmlReader.Create((await storageFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream(), xmlReaderSettings);
            void ReadXmlElementAction(string endElementName, Action action)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        action();
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == endElementName)
                        break;
                }
            }
            while (reader.Read())
            {
                if (reader.Name == c_sCCScene)
                {
                    ReadXmlElementAction(c_sCCScene, LoadResourceList);
                }
                void LoadResourceList()
                {
                    if (reader.Name == c_sFile)
                    {
                        CCResouce ccResouce = new CCResouce();
                        string sData = reader.GetAttribute(c_sDisplayName);
                        if (!string.IsNullOrEmpty(sData))
                            ccResouce.DisplayName = sData;

                        sData = reader.GetAttribute(c_sPath);
                        if (!string.IsNullOrEmpty(sData))
                            ccResouce.Path = sData;

                        sData = reader.GetAttribute(c_sRecordedTimeData);
                        if (!string.IsNullOrEmpty(sData) && long.TryParse(sData, out long t1))
                            ccResouce.RecordedTime = DateTime.FromFileTime(t1);

                        sData = reader.GetAttribute(c_sGuid);
                        if (!string.IsNullOrEmpty(sData) && Guid.TryParse(sData, out Guid guid1))
                            ccResouce.Guid = guid1;


                        if (string.IsNullOrEmpty(ccResouce.DisplayName)) ccResouce.DisplayName = string.Format("{0}", Path.GetFileName(ccResouce.Path));
                        resourceList.Add(ccResouce);
                        reader.Skip();
                    }
                }
            }


            reader.Dispose();
        }
        public async Task SaveFile()
        {
            LastUpdateTime = DateTime.Now;
            XmlWriter writer = XmlWriter.Create((await storageFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream(), xmlWriterSettings);
            writer.WriteStartDocument();
            writer.WriteStartElement(c_sCCScene);
            writer.WriteStartElement(c_sResources);
            for (int i = 0; i < resourceList.Count; i++)
            {
                writer.WriteStartElement(c_sFile);
                writer.WriteAttributeString(c_sDisplayName, resourceList[i].DisplayName);
                writer.WriteAttributeString(c_sPath, resourceList[i].Path);
                writer.WriteAttributeString(c_sRecordedTime, resourceList[i].RecordedTime.ToString("yyyy/MM/dd-hh:mm:ss"));
                writer.WriteAttributeString(c_sRecordedTimeData, resourceList[i].RecordedTime.ToFileTime().ToString());
                writer.WriteAttributeString(c_sGuid, resourceList[i].Guid.ToString());

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Dispose();
        }
        private CCSceneFormat()
        {

        }
    }
    public class CCResouce
    {
        public string DisplayName;
        public string Path;
        public Guid Guid;
        public DateTime RecordedTime;
    }
}
