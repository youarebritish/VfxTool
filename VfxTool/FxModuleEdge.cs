using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VfxTool
{
    public class FxModuleEdge : IXmlSerializable
    {
        private byte sourceNodeIndex;
        private byte targetNodeIndex;
        private byte sourcePortType;
        private byte sourcePortIndex;
        private byte targetPortType;
        private byte targetPortIndex;

        public static FxModuleEdge Read(BinaryReader reader)
        {
            var edge = new FxModuleEdge();

            edge.sourceNodeIndex = reader.ReadByte();
            edge.targetNodeIndex = reader.ReadByte();
            edge.sourcePortType = reader.ReadByte();
            edge.sourcePortIndex = reader.ReadByte();
            edge.targetPortType = reader.ReadByte();
            edge.targetPortIndex = reader.ReadByte();

            return edge;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)this.sourceNodeIndex);
            writer.Write((byte)this.targetNodeIndex);
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
            this.sourceNodeIndex = byte.Parse(reader["sourceNodeIndex"]);
            this.targetNodeIndex = byte.Parse(reader["targetNodeIndex"]);
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
