using System.Collections.Generic;
using System.Diagnostics;

namespace VfxTool
{
    public class FxVfxNodePortDefinition
    {
        public ushort index { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    [DebuggerDisplay("{name}")]
    public class FxVfxNodeDefinition
    {
        public string name { get; set; }
        public IList<FxVfxNodePropertyDefinition> properties { get; set; }
        public IList<FxVfxNodePortDefinition> inputPorts { get; set; } = new List<FxVfxNodePortDefinition>();
        public IList<FxVfxNodePortDefinition> outputPorts { get; set; } = new List<FxVfxNodePortDefinition>();
    }
}
