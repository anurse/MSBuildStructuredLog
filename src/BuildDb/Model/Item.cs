using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class Item
    {
        public int Id { get; set; }
        public int ItemGroupId { get; set; }
        public string ItemSpec { get; set; }

        public virtual ItemGroup ItemGroup { get; set; }
        public virtual IList<ItemMetadata> Metadata { get; set; }
    }
}
