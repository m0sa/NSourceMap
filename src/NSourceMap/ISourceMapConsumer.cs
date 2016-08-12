namespace NSourceMap
{
    public interface ISourceMapConsumer : ISourceMapping
    {
        void Parse(string contents); // throws SourceMapParseException
    }
}