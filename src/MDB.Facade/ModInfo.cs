namespace GameSDK.ModHost
{
    /// <summary>
    /// Runtime mod metadata. Facade stub for MetadataLoadContext resolution.
    /// </summary>
    public class ModInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }

        public ModInfo()
        {
            Id = "unknown";
            Name = "Unknown Mod";
            Version = "1.0.0";
            Author = "Unknown";
            Description = "";
            FilePath = "";
        }

        public override string ToString()
        {
            return $"{Name} v{Version} by {Author}";
        }
    }
}
