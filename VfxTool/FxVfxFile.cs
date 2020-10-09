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

        public void Read(BinaryReader reader, IDictionary<ulong, FxVfxNodeDefinition> definitions)
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

            // TODO read edges
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

            writer.WriteEndDocument();
        }
    }
}
