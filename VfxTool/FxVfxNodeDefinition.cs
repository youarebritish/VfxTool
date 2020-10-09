using System.Collections;
using System.Collections.Generic;

namespace VfxTool
{
    public class FxVfxNodeDefinition
    {
        public string name { get; set; }
        public IList<FxVfxNodePropertyDefinition> properties { get; set; }
    }
}
