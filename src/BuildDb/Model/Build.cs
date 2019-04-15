using System;
using System.Collections.Generic;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class Build
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Succeeded { get; set; }

        public virtual IList<BuildProperty> Environment { get; set; }
        public virtual IList<Project> Projects { get; set; }
    }
}
