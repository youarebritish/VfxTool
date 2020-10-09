using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LbaTool
{
    [DebuggerDisplay("x = {X}, y = {Y}, z = {Z}")]
    public class Vector3 : IXmlSerializable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static Vector3 Read(BinaryReader reader)
        {
            return new Vector3 {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
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
            writer.WriteAttributeString("x", X.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("y", Y.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("z", Z.ToString(CultureInfo.InvariantCulture));
        }
    }
}