namespace NSourceMap
{
    public interface ISourceMapping
    {
        /**
        * Returns the original mapping for the line number and column position found
        * in the source map. Returns null if none is found.
        *
        * @param lineNumber The line number, with the first being '1'.
        * @param columnIndex The column index, with the first being '1'.
        */
        OriginalMapping GetMappingForLine(int lineNumber, int columnIndex);
    }
}