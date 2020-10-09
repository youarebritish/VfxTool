using LbaTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    [DebuggerDisplay("{type}")]
    internal class FxVfxNode : IXmlSerializable
    {
        private string type;
        private IDictionary<string, IList> properties = new Dictionary<string, IList>();

        public static FxVfxNode Read(BinaryReader reader, FxVfxNodeDefinition definition)
        {
            var node = new FxVfxNode();
            node.type = definition.name;

            foreach(var property in definition.properties)
            {
                var values = new List<object>();
                node.properties.Add(property.name, values);

                var arraySize = reader.ReadByte();
                for(var i = 0; i < arraySize; i++)
                {
                    values.Add(ReadValue(reader, property.type));
                }
            }

            return node;
        }

        private static object ReadValue(BinaryReader reader, string type)
        {
            switch(type)
            {
                case "int8":
                    return reader.ReadSByte();
                case "uint8":
                    return reader.ReadByte();
                case "int16":
                    return reader.ReadInt16();
                case "uint16":
                    return reader.ReadUInt16();
                case "int":
                case "int32":
                    return reader.ReadInt32();
                case "uint":
                case "uint32":
                    return reader.ReadUInt32();
                case "int64":
                    return reader.ReadInt64();
                case "uint64":
                    return reader.ReadUInt64();
                case "float":
                    return reader.ReadSingle();
                case "double":
                    return reader.ReadDouble();
                case "bool":
                    return reader.ReadBoolean();
                case "Vector3":
                    return Vector3.Read(reader);
                case "Vector4":
                case "Quaternion":
                    return Vector4.Read(reader);
                case "string":
                    var size = reader.ReadUInt16();
                    var str = new string(reader.ReadChars(size));
                    reader.ReadChar();

                    return str;
                default:
                    throw new FormatException($"Unknown property type {type}");
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("class", this.type);

            foreach(var property in this.properties)
            {
                writer.WriteStartElement(property.Key);

                foreach(var val in property.Value)
                {
                    WriteItemXml(writer, val);
                }

                writer.WriteEndElement();
            }
        }

        private void WriteItemXml(XmlWriter writer, object val)
        {
            writer.WriteStartElement("value");

            // HACK
            if (val is Vector3)
            {
                (val as Vector3).WriteXml(writer);
            }
            else if (val is Vector4)
            {
                (val as Vector4).WriteXml(writer);
            }
            else
            {
                writer.WriteValue(val);
            }

            writer.WriteEndElement();
        }
    }
}
