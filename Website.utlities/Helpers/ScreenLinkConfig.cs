using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Website.utlities.Helpers
{
    public class ScreenLinkConfig
    {
        public required string Domain { get; set; }
        public required string SnapshotFolder { get; set; }
        public int? NestedLevel { get; set; }
        public bool AllowExternal { get; set; }
        public bool NestedFolder { get; set; }
    }
}
