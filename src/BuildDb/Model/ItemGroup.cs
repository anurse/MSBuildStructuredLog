using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class ItemGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual IList<Item> Items { get; set; }
    }
}
