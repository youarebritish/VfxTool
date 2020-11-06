using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VfxTool
{
    public class PropertyNameUnhasher
    {
        public static void Unhash()
        {
            var hashes = (from line in File.ReadAllLines("E:\\Development\\Reverse Engineering\\MGSV\\Vfx\\vfx_property_names_unmatchedHashes.txt")
                          select uint.Parse(line)).ToHashSet();

            var words = (from line in File.ReadAllLines("E:\\Development\\Reverse Engineering\\MGSV\\Vfx\\brute_force_words.txt")
                        select line.Replace("\t", string.Empty))
                        .ToHashSet();
            var lookup = MakeLookup(words);

            var matches = new HashSet<string>();

            foreach(var hash in hashes)
            {
                if (lookup.ContainsKey(hash))
                {
                    matches.Add(lookup[hash]);
                }
            }

            return;
        }

        private static IDictionary<uint, string> MakeLookup(HashSet<string> words)
        {
            IDictionary<uint, string> result = new Dictionary<uint, string>();

            foreach(var word in words)
            {
                foreach(var word2 in words)
                {
                    if (result.Count > 47995000)
                    {
                        return result;
                    }

                    var str = word + char.ToUpper(word2[0]) + word2.Substring(1);
                    var hash = (uint)Program.HashString(str);

                    if (result.ContainsKey(hash))
                    {
                        continue;
                    }

                    result.Add(hash, str);
                }
            }

            return result;
        }
    }
}
