using System.Collections.Generic;

namespace VfxTool
{
    public enum Type
    {
        Bool = 0,
        Int32 = 1,
        Float = 2,
        Vector4 = 3,
        String = 4,
        INVALID = 5,
        Int64 = 6
    };

public class DumpedPropertyDefinition
    {
        public uint name;
        public byte type;
    }

    public class DumpedNodeDefinition
    {
        public uint name;
        public List<DumpedPropertyDefinition> properties;
    }
}
