using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    /// <summary>
    /// Represents arbitrary key-value pairs in the MSBuild file (includes Properties and Item metadata).
    /// </summary>
    public class Property
    {
        public int Id { get; set; }
        public int DefinitionId { get; set; }
        public string Value { get; set; }

        public virtual PropertyDefinition Definition { get; set; }
    }
}
