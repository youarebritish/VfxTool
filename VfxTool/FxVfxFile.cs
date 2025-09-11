using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
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
        private ushort variationCount;
        public readonly IList<FxVfxNode> nodes = new List<FxVfxNode>();
        public readonly IList<FxModuleEdge> edges = new List<FxModuleEdge>();
        public readonly IList<FxVariation> variations = new List<FxVariation>();
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

        public bool Read(BinaryReader reader, string filename, Dictionary<ulong, string> pathDictionary, Dictionary<ulong, string> strDictionary)
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
            this.variationCount = reader.ReadUInt16();
            var unknown = reader.ReadUInt32();

            var nodeIndexSize = this.GetNodeIndexSize();

            // TODO: Figure out how to handle GZ hashes
            if (Program.IsVerbose)
            {
                Console.WriteLine($"Version: {this.version}");
                Console.WriteLine($"Node count: {this.nodeCount}");
                Console.WriteLine($"Edge count: {this.edgeCount}");
                Console.WriteLine($"Variation count: {this.variationCount}");
                Console.WriteLine($"Unknown: {unknown}");
            }

            for (var i = 0; i < nodeCount; i++)
            {
                if (!TryReadNode(reader, i, this.Definitions, pathDictionary, strDictionary))
                {
                    return false;
                }
            }

            for (var i = 0; i < edgeCount; i++)
            {
                ReadEdge(reader, nodeIndexSize);
            }

            for (var i = 0; i < variationCount; i++)
            {
                ReadVariation(reader);
            }

            return true;
        }

        private NodeIndexSize GetNodeIndexSize()
        {
            if (this.nodeCount >= byte.MaxValue)
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

            writer.Write((ushort)this.variations.Count);
            writer.Write(0);

            foreach (var node in this.nodes)
            {
                node.Write(writer);
            }

            var nodeIndexSize = this.GetNodeIndexSize();

            foreach (var edge in this.edges)
            {
                edge.Write(writer, nodeIndexSize);
            }

            foreach (var variation in this.variations)
            {
                variation.Write(writer);
            }
        }

        private bool TryReadNode(BinaryReader reader, int index, IDictionary<ulong, FxVfxNodeDefinition> definitions, Dictionary<ulong, string> pathDictionary, Dictionary<ulong, string> strDictionary)
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

            var node = FxVfxNode.Read(reader, definition, this.GetStringType(), pathDictionary, strDictionary);
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

        private void ReadVariation(BinaryReader reader)
        {
            var variation = FxVariation.Read(reader);
            variations.Add(variation);
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

            this.nodeCount = (ushort)this.nodes.Count;

            reader.ReadEndElement();
            reader.ReadStartElement("edges");
            while (reader.NodeType == XmlNodeType.Element)
            {
                var edge = new FxModuleEdge();
                edge.ReadXml(reader);
                this.edges.Add(edge);
            }

            this.edgeCount = (ushort)this.edges.Count;

            reader.ReadEndElement();
            reader.ReadStartElement("variations");
            while (reader.NodeType == XmlNodeType.Element)
            {
                var variation = new FxVariation();
                variation.ReadXml(reader);
                this.variations.Add(variation);
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

            writer.WriteStartElement("variations");
            foreach (var variation in variations)
            {
                writer.WriteStartElement("variation");
                variation.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }
}
