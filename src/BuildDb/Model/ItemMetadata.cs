namespace Microsoft.Build.Logging.BuildDb.Model
{
    public class ItemMetadata
    {
        public int ItemId { get; set; }
        public int PropertyId { get; set; }

        public virtual Item Item { get; set; }
        public virtual Property Property { get; set; }
    }
}
