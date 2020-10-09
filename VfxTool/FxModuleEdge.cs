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

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new System.NotImplementedException();
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
