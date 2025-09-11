using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    public class FxModuleEdge : IXmlSerializable
    {
        public enum NodeIndexSize
        {
            UInt8,
            UInt16
        }

        public ushort sourceNodeIndex;
        public ushort targetNodeIndex;
        public byte sourcePortType;
        public byte sourcePortIndex;
        public byte targetPortType;
        public byte targetPortIndex;

        public static FxModuleEdge Read(BinaryReader reader, NodeIndexSize size)
        {
            var edge = new FxModuleEdge();
            if (size == NodeIndexSize.UInt8)
            {
                edge.sourceNodeIndex = reader.ReadByte();
                edge.targetNodeIndex = reader.ReadByte();
            }
            else
            {
                edge.sourceNodeIndex = reader.ReadUInt16();
                edge.targetNodeIndex = reader.ReadUInt16();
            }

            edge.sourcePortType = reader.ReadByte();
            edge.sourcePortIndex = reader.ReadByte();
            edge.targetPortType = reader.ReadByte();
            edge.targetPortIndex = reader.ReadByte();

            return edge;
        }

        public void Write(BinaryWriter writer, NodeIndexSize nodeIndexSize)
        {
            if (nodeIndexSize == NodeIndexSize.UInt8)
            {
                writer.Write((byte)this.sourceNodeIndex);
                writer.Write((byte)this.targetNodeIndex);
            }
            else
            {
                writer.Write((ushort)this.sourceNodeIndex);
                writer.Write((ushort)this.targetNodeIndex);
            }
            writer.Write((byte)this.sourcePortType);
            writer.Write((byte)this.sourcePortIndex);
            writer.Write((byte)this.targetPortType);
            writer.Write((byte)this.targetPortIndex);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            this.sourceNodeIndex = ushort.Parse(reader["sourceNodeIndex"]);
            this.targetNodeIndex = ushort.Parse(reader["targetNodeIndex"]);
            this.sourcePortType = byte.Parse(reader["sourcePortType"]);
            this.sourcePortIndex = byte.Parse(reader["sourcePortIndex"]);
            this.targetPortType = byte.Parse(reader["targetPortType"]);
            this.targetPortIndex = byte.Parse(reader["targetPortIndex"]);

            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(sourceNodeIndex), this.sourceNodeIndex.ToString());
            writer.WriteAttributeString(nameof(targetNodeIndex), this.targetNodeIndex.ToString());
            writer.WriteAttributeString(nameof(sourcePortType), this.sourcePortType.ToString());
            writer.WriteAttributeString(nameof(sourcePortIndex), this.sourcePortIndex.ToString());
            writer.WriteAttributeString(nameof(targetPortType), this.targetPortType.ToString());
            writer.WriteAttributeString(nameof(targetPortIndex), this.targetPortIndex.ToString());
        }
    }
}
