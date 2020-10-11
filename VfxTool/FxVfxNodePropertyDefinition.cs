using System.Diagnostics;

namespace VfxTool
{

    [DebuggerDisplay("{name} ({type})")]
    public class FxVfxNodePropertyDefinition
    {
        public string name { get; set; }
        public string type { get; set; }
    }
}
