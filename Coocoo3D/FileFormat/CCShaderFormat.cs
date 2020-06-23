using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace Coocoo3D.FileFormat
{
    public class CCShaderFormat
    {

        static XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
        {
            IgnoreComments = true,
        };
        static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
        };

        public string Name;
        public string Description;
        public string VSPath;
        public string GSPath;
        public string PSPath;
        public string CSPath;

        public static CCShaderFormat Load(Stream stream)
        {
            CCShaderFormat ccShaderFormat = new CCShaderFormat();
            ccShaderFormat.Reload(stream);
            return ccShaderFormat;
        }
        public void Reload(Stream stream)
        {
            Name = "";
            Description = "";
            VSPath = null;
            GSPath = null;
            PSPath = null;
            CSPath = null;

            XmlReader reader = XmlReader.Create(stream, xmlReaderSettings);
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
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "CCShader")
                {
                    ReadXmlElementAction("CCShader", LoadCCContent);
                }
                void LoadCCContent()
                {
                    if (reader.Name == "Name")
                    {
                        Name = reader.ReadElementContentAsString();
                    }
                    else if(reader.Name == "Description")
                    {
                        Description = reader.ReadElementContentAsString();
                    }
                    else if(reader.Name == "VSPath")
                    {
                        VSPath = reader.ReadElementContentAsString();
                    }
                    else if(reader.Name == "GSPath")
                    {
                        GSPath = reader.ReadElementContentAsString();
                    }
                    else if(reader.Name == "PSPath")
                    {
                        PSPath = reader.ReadElementContentAsString();
                    }
                    else if(reader.Name == "CSPath")
                    {
                        CSPath = reader.ReadElementContentAsString();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }

        }
    }
}
