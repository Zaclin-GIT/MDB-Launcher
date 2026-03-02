using System;

namespace GameSDK.ModHost
{
    /// <summary>
    /// Attribute used to declare mod metadata on a ModBase subclass.
    /// This is a facade matching the real MDB Framework attribute signature
    /// so that MetadataLoadContext can resolve mod DLL attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModAttribute : Attribute
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; set; }
        public string Description { get; set; }

        public ModAttribute(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
            Author = "Unknown";
            Description = "";
        }
    }
}
