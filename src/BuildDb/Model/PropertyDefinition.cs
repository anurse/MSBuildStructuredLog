using System.Collections.Generic;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class PropertyDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsMetadata { get; set; }

        public virtual IList<Property> Properties { get; set; }
    }
}
