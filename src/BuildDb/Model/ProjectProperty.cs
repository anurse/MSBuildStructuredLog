using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class ProjectProperty
    {
        public int ProjectId { get; set; }
        public int PropertyId { get; set; }
        public bool Global { get; set; }

        public virtual Project Project { get; set; }
        public virtual Property Property { get; set; }
    }
}
