using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace VfxTool
{
    [DebuggerDisplay("{name}")]
    public class FxVfxNodeDefinition
    {
        public string name { get; set; }
        public IList<FxVfxNodePropertyDefinition> properties { get; set; }
    }
}
