namespace NSourceMap
{
    public sealed class OriginalMapping
    {
        private OriginalMapping(string originalFile, int lineNumber, int columnPosition, string identifier)
        {
            OriginalFile = originalFile;
            LineNumber = lineNumber;
            ColumnPosition = columnPosition;
            Identifier = identifier;
        }

        public string OriginalFile { get; }

        public int LineNumber { get; }

        public int ColumnPosition { get; }

        public string Identifier { get; }

        public Builder ToBuilder()
        {
            return new Builder()
                .SetOriginalFile(OriginalFile)
                .SetLineNumber(LineNumber)
                .SetColumnPosition(ColumnPosition)
                .SetIdentifier(Identifier);
        }

        public sealed class Builder
        {
            private int _columnPosition;
            private string _identifier;
            private int _lineNumber;
            private string _originalFile;

            public Builder SetOriginalFile(string value)
            {
                _originalFile = value;
                return this;
            }

            public Builder SetLineNumber(int value)
            {
                _lineNumber = value;
                return this;
            }

            public Builder SetColumnPosition(int value)
            {
                _columnPosition = value;
                return this;
            }

            public Builder SetIdentifier(string value)
            {
                _identifier = value;
                return this;
            }

            public OriginalMapping Build()
            {
                return new OriginalMapping(_originalFile, _lineNumber, _columnPosition, _identifier);
            }
        }
    }
}