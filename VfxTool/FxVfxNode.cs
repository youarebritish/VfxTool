﻿using LbaTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    [DebuggerDisplay("{type}")]
    internal class FxVfxNode : IXmlSerializable
    {
        private FxVfxNodeDefinition definition;
        private IDictionary<string, IList> properties = new Dictionary<string, IList>();

        private FxVfxNode() { }

        public static FxVfxNode FromTemplate(FxVfxNodeDefinition definition)
        {
            var node = new FxVfxNode
            {
                definition = definition
            };

            foreach (var property in definition.properties)
            {
                node.properties.Add(property.name, new List<object>());
            }

            return node;
        }

        public static FxVfxNode Read(BinaryReader reader, FxVfxNodeDefinition definition)
        {
            var node = new FxVfxNode
            {
                definition = definition
            };

            foreach (var property in definition.properties)
            {
                var values = new List<object>();
                node.properties.Add(property.name, values);

                var arraySize = reader.ReadByte();
                if (Program.IsVerbose)
                {
                    Console.WriteLine($"Reading {property.name} ({arraySize}) at {reader.BaseStream.Position - 1}");
                }

                for(var i = 0; i < arraySize; i++)
                {
                    values.Add(ReadValue(reader, property.type));
                }
            }

            return node;
        }

        public void Write(BinaryWriter writer)
        {
            var hash = Program.HashString(this.definition.name);
            writer.Write(hash);

            foreach(var propertyDefinition in this.definition.properties)
            {
                var values = this.properties[propertyDefinition.name];
                writer.Write((byte)values.Count);

                foreach(var value in values)
                {
                    WriteValue(writer, value, propertyDefinition.type);
                }
            }
        }

        private static bool ParseBool(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            return bool.Parse(str);
        }

        private static void WriteValue(BinaryWriter writer, object value, string type)
        {
            var str = value as string;
            switch (type)
            {
                case "int8":
                    writer.Write(sbyte.Parse(str));
                    return;
                case "uint8":
                    writer.Write(byte.Parse(str));
                    return;
                case "int16":
                    writer.Write(short.Parse(str));
                    return;
                case "uint16":
                    writer.Write(ushort.Parse(str));
                    return;
                case "int":
                case "int32":
                    writer.Write(int.Parse(str));
                    return;
                case "uint":
                case "uint32":
                    writer.Write(uint.Parse(str));
                    return;
                case "int64":
                    writer.Write(long.Parse(str));
                    return;
                case "uint64":
                    writer.Write(ulong.Parse(str));
                    return;
                case "float":
                    writer.Write(VfxTool.Extensions.ParseFloatRoundtrip(str));
                    return;
                case "double":
                    writer.Write(VfxTool.Extensions.ParseDoubleRoundtrip(str));
                    return;
                case "bool":
                    writer.Write(ParseBool(str));
                    return;
                case "Vector3":
                    (value as Vector3)?.Write(writer);
                    return;
                case "Vector4":
                case "Quaternion":
                    (value as Vector4)?.Write(writer);
                    return;
                case "string":
                    writer.Write((ushort)str.Length);
                    foreach(var character in str)
                    {
                        writer.Write(character);
                    }

                    writer.Write((byte)0);
                    return;
                default:
                    throw new FormatException($"Unknown property type {type}");
            }
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
                    if (Program.IsVerbose)
                    {
                        Console.WriteLine($"String length: {size}");
                    }

                    var str = new string(reader.ReadChars(size));
                    reader.ReadChar();

                    if (Program.IsVerbose)
                    {
                        Console.WriteLine($"String: {str}");
                    }

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
            reader.ReadStartElement("node");
            if (this.definition.properties.Count == 0)
            {
                return;
            }

            while (reader.NodeType == XmlNodeType.Element)
            {
                var name = reader.GetAttribute("name");

                var propertyDefinitions = definition.properties.ToDictionary(prop => prop.name, prop => prop);
                if (!propertyDefinitions.ContainsKey(name))
                {
                    throw new FormatException($"Unexpected property '{name}' in node type '{this.definition.name}'");
                }

                var propertyDefinition = propertyDefinitions[name];
                var property = this.properties[name];
                reader.ReadStartElement("property");

                while (reader.NodeType == XmlNodeType.Element)
                {
                    object val = null;
                    if (propertyDefinition.type == "Vector3")
                    {
                        val = new Vector3();
                        (val as Vector3)?.ReadXml(reader);
                        reader.Read();
                    }
                    else if (propertyDefinition.type == "Vector4" || propertyDefinition.type == "Quaternion")
                    {
                        val = new Vector4();
                        (val as Vector4)?.ReadXml(reader);
                        reader.Read();
                    }
                    else
                    {
                        val = reader.ReadElementContentAsObject();
                    }

                    property.Add(val);
                }

                reader.ReadEndElement();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("class", this.definition.name);

            foreach(var property in this.properties)
            {
                writer.WriteStartElement("property");
                writer.WriteAttributeString("name", property.Key);

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
