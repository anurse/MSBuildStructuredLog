using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class BuildProperty
    {
        public int BuildId { get; set; }
        public int PropertyId { get; set; }

        public virtual Build Build { get; set; }
        public virtual Property Property { get; set; }
    }
}
