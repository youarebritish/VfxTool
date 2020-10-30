using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace VfxTool
{
    internal static class Program
    {
        private const string NodeDefinitionsPath = "/Definitions/";
        private const string TppDefinitionsPath = "TPP/";
        private const string GzDefinitionsPath = "GZ/";

        public static bool IsVerbose;

        private static void Main(string[] args)
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var tppDefinitions = ReadDefinitions(directory + NodeDefinitionsPath + TppDefinitionsPath);
            var gzDefinitions = ReadDefinitions(directory + NodeDefinitionsPath + GzDefinitionsPath);
            var shouldKeepWindowOpen = false;

            foreach(var arg in args)
            {
                if (arg == "-verbose")
                {
                    IsVerbose = true;
                    break;
                }
            }

            foreach (var path in args)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var fileExtension = Path.GetExtension(path);
                if (fileExtension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var vfx = ReadFromXml(path, tppDefinitions, gzDefinitions);
                    WriteToBinary(vfx, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)) + ".vfx");
                }
                else if (fileExtension.Equals(".vfx", StringComparison.OrdinalIgnoreCase))
                {
                    var vfx = ReadFromBinary(path, tppDefinitions, gzDefinitions);
                    if (vfx != null)
                    {
                        WriteToXml(vfx, Path.GetFileNameWithoutExtension(path) + ".vfx.xml");
                        continue;
                    }

                    shouldKeepWindowOpen = true;
                }
            }

            if (shouldKeepWindowOpen)
            {
                Console.ReadLine();
            }
        }

        private static IDictionary<ulong, FxVfxNodeDefinition> ReadDefinitions(string path)
        {
            return (from file in Directory.GetFiles(path)
                   select JsonConvert.DeserializeObject<FxVfxNodeDefinition>(File.ReadAllText(file)))
                   .ToDictionary(definition => HashString(definition.name), definition => definition);
        }

        private static FxVfxFile ReadFromBinary(string path, IDictionary<ulong, FxVfxNodeDefinition> tppDefinitions, IDictionary<ulong, FxVfxNodeDefinition> gzDefinitions)
        {
            var vfx = new FxVfxFile(tppDefinitions, gzDefinitions);
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                if (vfx.Read(reader, Path.GetFileNameWithoutExtension(path)))
                {
                    return vfx;
                }
            }

            return null;
        }

        public static FxVfxFile ReadFromXml(string path, IDictionary<ulong, FxVfxNodeDefinition> tppDefinitions, IDictionary<ulong, FxVfxNodeDefinition> gzDefinitions)
        {
            var xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };

            var vfx = new FxVfxFile(tppDefinitions, gzDefinitions);
            using (var reader = XmlReader.Create(path, xmlReaderSettings))
            {
                vfx.ReadXml(reader);
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

        private static void WriteToBinary(FxVfxFile vfx, string path)
        {
            using (var writer = new BinaryWriter(new FileStream(path, FileMode.OpenOrCreate)))
            {
                vfx.Write(writer);
            }
        }

        public static ulong HashString(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            const ulong seed0 = 0x9ae16a3b2f90404f;
            ulong seed1 = text.Length > 0 ? (uint)((text[0]) << 16) + (uint)text.Length : 0;
            return CityHash.CityHash.CityHash64WithSeeds(text + "\0", seed0, seed1) & 0xFFFFFFFFFFFF;
        }
    }
}
