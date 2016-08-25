namespace NSourceMap
{
    public sealed class OriginalMapping
    {
        public OriginalMapping(string source = null, int lineNumber = 0, int columnPosition = 0, string name = null)
        {
            Source = source;
            LineNumber = lineNumber;
            ColumnPosition = columnPosition;
            Name = name;
        }

        public string Source { get; }

        public int LineNumber { get; }

        public int ColumnPosition { get; }

        public string Name { get; }
    }
}