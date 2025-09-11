using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VfxTool
{
    internal static class Program
    {
        private const string NodeDefinitionsPath = "/Definitions/";
        private const string TppDefinitionsPath = "TPP/";
        private const string GzDefinitionsPath = "GZ/";

        public static bool IsVerbose;

        private const string PathDictionaryName = "vfx_path_dictionary.txt";
        private const string StrDictionaryName = "vfx_string_dictionary.txt";

        private const string PathHashDump = "vfx_path_hashdump.txt";
        private const string StrHashDump = "vfx_string_hashdump.txt";

        private static void Main(string[] args)
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var tppDefinitions = ReadDefinitions(directory + NodeDefinitionsPath + TppDefinitionsPath);
            var gzDefinitions = ReadDefinitions(directory + NodeDefinitionsPath + GzDefinitionsPath);
            var shouldKeepWindowOpen = false;

            var pathDictionary = GetLookupTable(GetStringLiterals(directory + '\\' + PathDictionaryName), true);
            var strDictionary = GetLookupTable(GetStringLiterals(directory + '\\' + StrDictionaryName), false);

            var pathHashes = new List<ulong>();
            var strHashes = new List<ulong>();

            foreach (var arg in args)
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
                    if (vfx != null)
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                        string outPath = Path.Combine(Path.GetDirectoryName(path), fileNameWithoutExtension);

                        WriteToBinary(vfx, outPath);
                        continue;
                    }

                    shouldKeepWindowOpen = true;
                }
                else if (fileExtension.Equals(".vfx", StringComparison.OrdinalIgnoreCase))
                {
                    var vfx = ReadFromBinary(path, tppDefinitions, gzDefinitions, pathDictionary, strDictionary);
                    if (vfx != null)
                    {
                        string outPath = path + ".xml";

                        WriteToXml(vfx, outPath);

                        GetPathHashes(vfx, pathHashes);
                        GetStrHashes(vfx, strHashes);

                        continue;
                    }

                    shouldKeepWindowOpen = true;
                }
            }

            WriteHashDump(directory + '\\' + PathHashDump, pathHashes);
            WriteHashDump(directory + '\\' + StrHashDump, strHashes);

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

        private static FxVfxFile ReadFromBinary(string path, IDictionary<ulong, FxVfxNodeDefinition> tppDefinitions, IDictionary<ulong, FxVfxNodeDefinition> gzDefinitions, Dictionary<ulong, string> pathDictionary, Dictionary<ulong, string> strDictionary)
        {
            var vfx = new FxVfxFile(tppDefinitions, gzDefinitions);
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                if (vfx.Read(reader, Path.GetFileNameWithoutExtension(path), pathDictionary, strDictionary))
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
                var success = vfx.ReadXml(reader);
                if (!success)
                {
                    return null;
                }
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

        public static string Enbasen64(string strVal)
        {
            // GZ string hack since the encrypted paths contain invalid XML characters - base 64 encode them
            var extension = strVal.Substring(strVal.LastIndexOf('.'));
            var path = strVal.Substring(4, strVal.Length - extension.Length - 4);
            var bytes = System.Text.Encoding.UTF8.GetBytes(path);
            var base64 = Convert.ToBase64String(bytes);

            return $"/as/{base64}{extension}";
        }
        public static string Debasen64(string strVal)
        {
            var extension = strVal.Substring(strVal.LastIndexOf('.'));
            var path = strVal.Substring(4, strVal.Length - extension.Length - 4);
            var base64 = Convert.FromBase64String(path);
            var bytes = System.Text.Encoding.UTF8.GetString(base64);
            return $"/as/{bytes}{extension}";
        }
        public static List<string> GetStringLiterals(string path)
        {
            List<string> stringLiterals = new List<string>();
            stringLiterals.Add(string.Empty);
            using (StreamReader file = new StreamReader(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                    stringLiterals.Add(line);
            }
            return stringLiterals;
        }
        public static Dictionary<ulong,string> GetLookupTable(List<string> stringLiterals, bool isPath)
        {
            ConcurrentDictionary<ulong, string> table = new ConcurrentDictionary<ulong, string>();

            Parallel.ForEach(stringLiterals, (string entry) =>
            {
                ulong hash;
                if (isPath)
                    hash = Extensions.HashFileNameWithExtension(entry);
                else
                    hash = Extensions.StrCode64(entry);
                table.TryAdd(hash, entry);
            });

            return new Dictionary<ulong, string>(table);
        }
        public static void GetStrHashes(FxVfxFile vfx, List<ulong> hashes)
        {
            foreach (FxVfxNode node in vfx.nodes)
                foreach (KeyValuePair<string, System.Collections.IList> property in node.properties)
                    if (property.Key == "StrCode")
                        foreach (object value in property.Value)
                            if (ulong.TryParse(value as string, out ulong propertyHash))
                                hashes.Add(propertyHash);
                            else if (value is string)
                            {

                            }

        }
        public static void GetPathHashes(FxVfxFile vfx, List<ulong> hashes)
        {
            foreach (FxVfxNode node in vfx.nodes)
                foreach (KeyValuePair<string, System.Collections.IList> property in node.properties)
                    if (property.Key == "PathCode64Ext")
                        foreach (object value in property.Value)
                            if (ulong.TryParse(value as string, out ulong propertyHash))
                                hashes.Add(propertyHash);
        }
        private static void WriteHashDump(string path, List<ulong> hashList)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                foreach (ulong hash in hashList)
                {
                    file.WriteLine(hash.ToString());
                }
            }
        }
    }
}
