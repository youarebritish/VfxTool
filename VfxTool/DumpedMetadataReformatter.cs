using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VfxTool
{
    public static class DumpedMetadataReformatter
    {
        private readonly static string[] Types = { "control", "emit", "life", "material", "program_effect", "vector", "shape" };

        private static IEnumerable<DumpedNodeDefinition> ParseDumpedDefinitions()
        {
            return from type in Types
                   from definition in JsonConvert.DeserializeObject<DumpedNodeDefinition[]>(File.ReadAllText($"{type}_node_dumps.json"))
                   select definition;
        }

        private static IDictionary<uint, string> MakeNodeNameLookupTable()
        {
            var lines = from type in Types
                        from line in File.ReadAllLines($"{type}_nodes.txt")
                        select line;

            var result = new Dictionary<uint, string>();
            foreach(var line in lines)
            {
                var tokens = line.Split(' ');
                result.Add((uint)ulong.Parse(tokens[1]), tokens[0]);
            }

            return result;
        }

        private static IDictionary<uint, string> MakePropertyNameLookupTable()
        {
            return File.ReadAllLines("E:\\Development\\Reverse Engineering\\MGSV\\Vfx\\vfx_property_name_strings.txt")
                .ToDictionary(val => (uint)Program.HashString(val), val => val);
        }

        public static void GenerateNodeDefinitions()
        {
            var parsedDefinitions = ParseDumpedDefinitions();
            var nameLookup = MakeNodeNameLookupTable();
            var propertyNameLookup = MakePropertyNameLookupTable();

            foreach(var definition in parsedDefinitions)
            {
                var generatedDefinition = GenerateNodeDefinition(definition, propertyNameLookup, nameLookup[definition.name]);
                var json = JsonConvert.SerializeObject(generatedDefinition, Formatting.Indented);
                File.WriteAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/GeneratedDefinitions/TPP/{generatedDefinition.name}.json", json);
            }
        }

        public static HashSet<uint> GetAllPropertyNames()
        {
            var parsedDefinitions = ParseDumpedDefinitions();
            return (from def in parsedDefinitions
                    from prop in def.properties
                    select prop.name).Distinct().ToHashSet();
        }

        private static FxVfxNodeDefinition GenerateNodeDefinition(DumpedNodeDefinition dumpedDefinition, IDictionary<uint, string> propertyNameLookup, string name)
        {
            var result = new FxVfxNodeDefinition
            {
                name = name,
                properties = new List<FxVfxNodePropertyDefinition>()
            };

            foreach (var property in dumpedDefinition.properties)
            {
                var propertyName = property.name.ToString();
                if (propertyNameLookup.ContainsKey(property.name))
                {
                    propertyName = propertyNameLookup[property.name];
                }

                var newProp = new FxVfxNodePropertyDefinition
                {
                    name = propertyName,
                    type = ParseType((Type)property.type)
                };

                result.properties.Add(newProp);
            }

            return result;
        }

        private static string ParseType(Type type)
        {
            switch (type)
            {
                case Type.Bool:
                    return "bool";
                case Type.Int32:
                    return "uint32";
                case Type.Float:
                    return "float";
                case Type.Vector4:
                    return "Vector4";
                case Type.String:
                    return "string";
                case Type.Int64:
                    return "uint64";
                default:
                    return "INVALID";
            }
        }
    }
}
