using System;

namespace NSourceMap
{
    public class SourceMapParseException : Exception
    {
        public SourceMapParseException()
        {
        }

        public SourceMapParseException(string message) : base(message)
        {
        }

        public SourceMapParseException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}