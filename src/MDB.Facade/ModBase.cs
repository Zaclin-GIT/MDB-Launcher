namespace GameSDK.ModHost
{
    /// <summary>
    /// Base class for all MDB mods. Facade stub for MetadataLoadContext resolution.
    /// </summary>
    public abstract class ModBase
    {
        public ModInfo Info { get; set; }
        public ModLogger Logger { get; set; }

        public virtual void OnLoad() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnGUI() { }
    }
}
