
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    public class FxVariation : IXmlSerializable
    {
        public uint name;
        public List<ushort> nodesA = new List<ushort>();
        public List<ushort> nodesB = new List<ushort>();
        public static FxVariation Read(BinaryReader reader)
        {
            if (Program.IsVerbose)
                Console.WriteLine($"@{reader.BaseStream.Position} start of variation read");
            var effectVariation = new FxVariation();
            effectVariation.name = reader.ReadUInt32();
            if (Program.IsVerbose)
                Console.WriteLine($"Variation name: {effectVariation.name}");
            var nodePairCount = reader.ReadUInt32();
            effectVariation.nodesA.Capacity = (int)nodePairCount;
            effectVariation.nodesB.Capacity = (int)nodePairCount;
            if (Program.IsVerbose)
                Console.WriteLine($"Node pair count: {nodePairCount}");
            for (var i = 0; i < nodePairCount; i++)
            {
                if (Program.IsVerbose)
                    Console.WriteLine($"Read start @{reader.BaseStream.Position}");
                effectVariation.nodesA.Add(reader.ReadUInt16());
                effectVariation.nodesB.Add(reader.ReadUInt16());
                if (Program.IsVerbose)
                    Console.WriteLine($"Node A {effectVariation.nodesA[i]}, Node B {effectVariation.nodesB[i]}");
            }
            return effectVariation;
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write((uint)nodesA.Count);
            for (var i = 0; i < nodesA.Count; i++)
            {
                writer.Write(nodesA[i]);
                writer.Write(nodesB[i]);
            }
        }
        public void ReadXml(XmlReader reader)
        {
            var nameString = reader.GetAttribute(nameof(name));
            reader.ReadStartElement("variation");
            this.name = uint.TryParse(nameString, out this.name) ? this.name : (uint)Program.HashString(nameString);
            this.nodesA = new List<ushort>();
            this.nodesB = new List<ushort>();
            while (reader.NodeType == XmlNodeType.Element)
            {
                nodesA.Add(ushort.Parse(reader["targetNode"]));
                nodesB.Add(ushort.Parse(reader["newNode"]));

                reader.Read();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(name), this.name.ToString());
            for (var i = 0; i < nodesA.Count; i++)
            {
                writer.WriteStartElement("variationNodes");
                writer.WriteAttributeString("targetNode", this.nodesA[i].ToString());
                writer.WriteAttributeString("newNode", this.nodesB[i].ToString());
                writer.WriteEndElement();
            }
        }
        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}