using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    public class FxVfxFile : IXmlSerializable
    {
        private ushort nodeCount;
        private ushort edgeCount;
        private readonly IList<FxVfxNode> nodes = new List<FxVfxNode>();
        private readonly IList<FxModuleEdge> edges = new List<FxModuleEdge>();
        private readonly IDictionary<ulong, FxVfxNodeDefinition> definitions;

        public FxVfxFile(IDictionary<ulong, FxVfxNodeDefinition> definitions)
        {
            this.definitions = definitions;
        }

        public void Read(BinaryReader reader)
        {
            var signature = reader.ReadChars(3);
            var version = reader.ReadUInt16();

            if (version != 2)
            {
                Console.WriteLine("Unexpected version (" + version + "). Results may be unpredictable.");
            }

            this.nodeCount = reader.ReadUInt16();
            this.edgeCount = reader.ReadUInt16();

            reader.BaseStream.Position += 6;

            for(var i = 0; i < nodeCount; i++)
            {
                TryReadNode(reader, definitions);
            }

            for (var i = 0; i < edgeCount; i++)
            {
                ReadEdge(reader);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write('v');
            writer.Write('f');
            writer.Write('x');
            writer.Write((byte)2);

            writer.Write((ushort)this.nodes.Count);
            writer.Write((ushort)this.edges.Count);

            writer.Write(0);
            writer.Write((ushort)0);

            foreach(var node in this.nodes)
            {
                node.Write(writer);
            }

            foreach (var edge in this.edges)
            {
                edge.Write(writer);
            }
        }

        private void TryReadNode(BinaryReader reader, IDictionary<ulong, FxVfxNodeDefinition> definitions)
        {
            var hash = reader.ReadUInt64();
            if (!definitions.ContainsKey(hash))
            {
                throw new IOException($"Unsupported node type {hash} encountered at offset {reader.BaseStream.Position - 8}");
            }

            var definition = definitions[hash];
            var node = FxVfxNode.Read(reader, definition);
            nodes.Add(node);
        }

        private void ReadEdge(BinaryReader reader)
        {
            var edge = FxModuleEdge.Read(reader);
            edges.Add(edge);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            reader.ReadStartElement("vfx");
            reader.ReadStartElement("nodes");

            while (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.LocalName == "edges")
                {
                    break;
                }

                var type = reader.GetAttribute("class");
                var typeHash = Program.HashString(type);

                if (!this.definitions.ContainsKey(typeHash))
                {
                    throw new FormatException($"Unrecognized node type {type}");
                }

                var definition = this.definitions[typeHash];
                var node = FxVfxNode.FromTemplate(definition);
                node.ReadXml(reader);

                this.nodes.Add(node);
            }

            reader.ReadStartElement("edges");
            while (reader.NodeType == XmlNodeType.Element)
            {
                var edge = new FxModuleEdge();
                edge.ReadXml(reader);
                this.edges.Add(edge);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("vfx");

            writer.WriteStartElement("nodes");
            foreach (var node in nodes)
            {
                writer.WriteStartElement("node");
                node.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("edges");
            foreach (var edge in edges)
            {
                writer.WriteStartElement("edge");
                edge.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteEndDocument();
        }
    }
}
