using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace VfxTool
{
    internal static class Program
    {
        private const string NodeDefinitionsPath = "Definitions/";

        private static void Main(string[] args)
        {
            var definitions = ReadDefinitions(NodeDefinitionsPath);

            foreach (var path in args)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var fileExtension = Path.GetExtension(path);
                if (fileExtension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO
                    //var vfx = ReadFromXml(path);
                    //WriteToBinary(vfx, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)) + ".vfx");
                }
                else if (fileExtension.Equals(".vfx", StringComparison.OrdinalIgnoreCase))
                {
                    var vfx = ReadFromBinary(path, definitions);
                    WriteToXml(vfx, Path.GetFileNameWithoutExtension(path) + ".vfx.xml");
                }
                else
                {
                    throw new IOException("Unrecognized input type.");
                }
            }
        }

        private static IDictionary<ulong, FxVfxNodeDefinition> ReadDefinitions(string path)
        {
            return (from file in Directory.GetFiles(path)
                   select JsonConvert.DeserializeObject<FxVfxNodeDefinition>(File.ReadAllText(file)))
                   .ToDictionary(definition => HashString(definition.name), definition => definition);
        }

        private static FxVfxFile ReadFromBinary(string path, IDictionary<ulong, FxVfxNodeDefinition> definitions)
        {
            var vfx = new FxVfxFile();
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                vfx.Read(reader, definitions);
            }

            return vfx;
        }

        private static void WriteToXml(FxVfxFile vfx, string path)
        {
            var xmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };

            using (var writer = XmlWriter.Create(path, xmlWriterSettings))
            {
                vfx.WriteXml(writer);
            }
        }

        private static ulong HashString(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            const ulong seed0 = 0x9ae16a3b2f90404f;
            ulong seed1 = text.Length > 0 ? (uint)((text[0]) << 16) + (uint)text.Length : 0;
            return CityHash.CityHash.CityHash64WithSeeds(text + "\0", seed0, seed1) & 0xFFFFFFFFFFFF;
        }
    }
}
