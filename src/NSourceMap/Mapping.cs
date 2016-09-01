namespace NSourceMap
{
    public class Mapping
    {
        public Mapping(string source, FilePosition original, FilePosition generated, string name = null)
        {
            Source = source;
            Original = original;
            Generated = generated;
            Name = name;
        }

        /// <summary>The original source file (relative to the sourceRoot).</summary>
        public string Source { get; }

        /// <summary>An object with the original line and column positions.</summary>
        public FilePosition Original { get; }

        /// <summary>An object with the generated line and column positions.</summary>
        public FilePosition Generated { get; }

        /// <summary>An optional original token name for this mapping.</summary>
        public string Name { get; }
    }
}