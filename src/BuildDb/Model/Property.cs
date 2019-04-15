using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class Property
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
