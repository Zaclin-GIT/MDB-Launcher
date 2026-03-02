using System;

namespace GameSDK.ModHost
{
    /// <summary>
    /// Mod logger. Facade stub for MetadataLoadContext resolution.
    /// </summary>
    public class ModLogger
    {
        private readonly string _modName;

        public ModLogger(string modName)
        {
            _modName = modName;
        }

        public void Info(string message) { }
        public void Info(string message, ConsoleColor color) { }
        public void Info(string format, params object[] args) { }
        public void Warning(string message) { }
        public void Warning(string format, params object[] args) { }
        public void Error(string message) { }
        public void Error(string message, Exception ex) { }
        public void Error(string format, params object[] args) { }
        public void Debug(string message) { }
    }
}
