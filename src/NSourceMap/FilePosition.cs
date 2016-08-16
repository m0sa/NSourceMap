namespace NSourceMap
{
    public class FilePosition
    {
        public FilePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; }
        public int Column { get; }

        public FilePosition Prefix(FilePosition prefixPosition) =>
            new FilePosition(
                Line + prefixPosition.Line,
                Line != 0 // Only the first line needs the character position adjusted.
                    ? Column
                    : Column + prefixPosition.Column);
    }
}