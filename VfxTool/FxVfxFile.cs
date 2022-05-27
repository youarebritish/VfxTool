using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using static VfxTool.FxModuleEdge;
using static VfxTool.FxVfxNode;

namespace VfxTool
{
    public class FxVfxFile
    {
        private IDictionary<ulong, FxVfxNodeDefinition> Definitions
        {
            get
            {
                if (this.version == Version.Gz)
                {
                    return this.gzDefinitions;
                }
                else
                {
                    return this.tppDefinitions;
                }
            }
        }

        public ushort nodeCount;
        private ushort edgeCount;
        public ushort unknown1;
        public readonly IList<FxVfxNode> nodes = new List<FxVfxNode>();
        public readonly IList<FxModuleEdge> edges = new List<FxModuleEdge>();
        public readonly IDictionary<ulong, FxVfxNodeDefinition> tppDefinitions;
        public readonly IDictionary<ulong, FxVfxNodeDefinition> gzDefinitions;
        private string filename;
        private Version version;

        public enum Version
        {
            Gz = 0,
            Tpp = 2
        }

        public FxVfxFile(IDictionary<ulong, FxVfxNodeDefinition> tppDefinitions, IDictionary<ulong, FxVfxNodeDefinition> gzDefinitions)
        {
            this.tppDefinitions = tppDefinitions;
            this.gzDefinitions = gzDefinitions;
        }

        public bool Read(BinaryReader reader, string filename)
        {
            this.filename = filename;

            var signature = reader.ReadChars(3);
            if (new string(signature) != "vfx")
            {
                Console.WriteLine("Not a valid VFX file.");
                return false;
            }

            this.version = (Version)reader.ReadUInt16();
            this.nodeCount = reader.ReadUInt16();
            this.edgeCount = reader.ReadUInt16();
            this.unknown1 = reader.ReadUInt16();

            var nodeIndexSize = this.GetNodeIndexSize();

            // TODO: Figure out how to handle GZ hashes
            if (Program.IsVerbose)
            {
                Console.WriteLine($"Version: {this.version}");
                Console.WriteLine($"Node count: {this.nodeCount}");
                Console.WriteLine($"Edge count: {this.edgeCount}");

                if (this.unknown1 != 0)
                {
                    Console.WriteLine($"Unknown flag detected ({this.unknown1}). May have unexpected output.");
                }
            }

            reader.BaseStream.Position += 4;
            for (var i = 0; i < nodeCount; i++)
            {
                if (!TryReadNode(reader, i, this.Definitions))
                {
                    return false;
                }
            }

            for (var i = 0; i < edgeCount; i++)
            {
                ReadEdge(reader, nodeIndexSize);
            }

            return true;
        }

        private NodeIndexSize GetNodeIndexSize()
        {
            if (this.nodeCount > byte.MaxValue)
            {
                return NodeIndexSize.UInt16;
            }
            else
            {
                return NodeIndexSize.UInt8;
            }
        }

        private StringType GetStringType()
        {
            if (this.version == Version.Gz)
            {
                return StringType.Gz;
            }

            return StringType.Tpp;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write('v');
            writer.Write('f');
            writer.Write('x');
            writer.Write((ushort)this.version);

            writer.Write((ushort)this.nodes.Count);
            writer.Write((ushort)this.edges.Count);

            writer.Write(unknown1);
            writer.Write(0);

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

            var node = FxVfxNode.Read(reader, definition, this.GetStringType());
            if (Program.IsVerbose)
            {
                Console.WriteLine($"Finished {definition.name} at {reader.BaseStream.Position}");
            }

            nodes.Add(node);
            return true;
        }

        private void ReadEdge(BinaryReader reader, FxModuleEdge.NodeIndexSize edgeIndexSize)
        {
            var edge = FxModuleEdge.Read(reader, edgeIndexSize);
            edges.Add(edge);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public bool ReadXml(XmlReader reader)
        {
            reader.Read();
            reader.ReadToFollowing("vfx");
            reader.MoveToAttribute("version");

            var version = reader.Value;
            if (version == "GZ")
            {
                this.version = Version.Gz;
            }
            else if (version == "TPP")
            {
                this.version = Version.Tpp;
            }
            else
            {
                Console.WriteLine("No valid version specified. Add a version attribute to the <vfx> node with a value of GZ or TPP.");
                return false;
            }

            reader.MoveToAttribute("unknown1");
            this.unknown1 = ushort.Parse(reader.Value);

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

                if (!this.Definitions.ContainsKey(typeHash))
                {
                    throw new FormatException($"Unrecognized node type {type}");
                }

                var definition = this.Definitions[typeHash];
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

            return true;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("vfx");

            var versionString = "TPP";
            if (this.version == Version.Gz)
            {
                versionString = "GZ";
            }

            writer.WriteAttributeString("version", versionString);
            writer.WriteAttributeString("unknown1", unknown1.ToString());

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
