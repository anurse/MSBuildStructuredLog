using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class Project
    {
        public int Id { get; set; }
        public int BuildId { get; set; }
        public int? ParentId { get; set; }
        public int ProjectContextId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ProjectFile { get; set; }
        public string Targets { get; set; }
        public string ToolsVersion { get; set; }

        public virtual Build Build { get; set; }
        public virtual Project Parent { get; set; }
        public virtual IList<ProjectProperty> Properties { get; set; }
    }
}
