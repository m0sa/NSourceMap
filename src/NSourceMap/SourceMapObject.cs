namespace NSourceMap
{
    public sealed class SourceMapObject
    {
        public int? version { get; set; }

        public int? lineCount { get; set; }

        public string sourceRoot { get; set; }

        public string file { get; set; }

        public string mappings { get; set; }

        public string[] sources { get; set; }

        public string[] sourcesContent { get; set; }

        public string[] names { get; set; }
    }
}