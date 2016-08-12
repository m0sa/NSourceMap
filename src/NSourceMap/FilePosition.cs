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
    }
}