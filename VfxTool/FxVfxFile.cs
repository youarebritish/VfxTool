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
        private string filename;

        public FxVfxFile(IDictionary<ulong, FxVfxNodeDefinition> definitions)
        {
            this.definitions = definitions;
        }

        public bool Read(BinaryReader reader, string filename)
        {
            this.filename = filename;

            var signature = reader.ReadChars(3);
            var version = reader.ReadUInt16();

            if (version != 2)
            {
                Console.WriteLine("Unexpected version (" + version + "). Results may be unpredictable.");
            }

            this.nodeCount = reader.ReadUInt16();
            this.edgeCount = reader.ReadUInt16();

            if (Program.IsVerbose)
            {
                Console.WriteLine($"Node count: {this.nodeCount}");
                Console.WriteLine($"Edge count: {this.edgeCount}");
            }

            reader.BaseStream.Position += 6;

            for(var i = 0; i < nodeCount; i++)
            {
                if (!TryReadNode(reader, i, definitions))
                {
                    return false;
                }
            }

            for (var i = 0; i < edgeCount; i++)
            {
                ReadEdge(reader);
            }

            return true;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write('v');
            writer.Write('f');
            writer.Write('x');
            writer.Write((ushort)2);

            writer.Write((ushort)this.nodes.Count);
            writer.Write((ushort)this.edges.Count);

            writer.Write(0);
            writer.Write((ushort)0);

            foreach (var node in this.nodes)
            {
                node.Write(writer);
            }

            foreach (var edge in this.edges)
            {
                edge.Write(writer);
            }
        }

        private bool TryReadNode(BinaryReader reader, int index, IDictionary<ulong, FxVfxNodeDefinition> definitions)
        {
            var hash = reader.ReadUInt64();
            if (!definitions.ContainsKey(hash))
            {
                Console.WriteLine("---");
                Console.WriteLine($"[{this.filename}] Unsupported node type {hash} encountered at offset {reader.BaseStream.Position - 8}");
                return false;
            }

            var definition = definitions[hash];
            if (Program.IsVerbose)
            {
                Console.WriteLine("---");
                Console.WriteLine($"Reading node #{index} ({definition.name}) at {reader.BaseStream.Position - 8}");
                Console.WriteLine("---");
            }

            var node = FxVfxNode.Read(reader, definition);
            if (Program.IsVerbose)
            {
                Console.WriteLine($"Finished {definition.name} at {reader.BaseStream.Position}");
            }

            nodes.Add(node);
            return true;
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

            reader.ReadEndElement();
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
