using System.Linq;
using Xunit;
using NSourceMap;

namespace NSourceMap.Tests
{
    public class SourceLineMapTest
    {
        private static readonly SourceLineMap endsWithNewLine = SourceLineMap.From("0 zero\n1 one\r\n2 two\r\n");

        [Theory]
        [InlineData(0, 1)]
        [InlineData(6, 1)]
        [InlineData(7, 2)]
        [InlineData(8, 2)]
        [InlineData(13, 2)]
        [InlineData(14, 3)]
        [InlineData(null, 4)]
        public void EndsWithNewLine_CountLinesWithLength(int? length, int expectedLines) =>
            Assert.Equal(expectedLines, endsWithNewLine.CountLines(length));

        [Theory]
        [InlineData(0, 0, 7)]
        [InlineData(1, 7, 7)]
        [InlineData(2, 14, 7)]
        [InlineData(3, 21, 0)]
        public void EndsWithNewLine_LineMap(int line, int expectedOffset, int expectedCount)
        {
            Assert.Equal(expectedOffset, endsWithNewLine[line].Offset);
            Assert.Equal(expectedCount, endsWithNewLine[line].Count);
        }
        private static readonly SourceLineMap endsWithString = SourceLineMap.From("0 zero\n1 one\r\n2 two\r\n3 three");

        [Theory]
        [InlineData(0, 1)]
        [InlineData(6, 1)]
        [InlineData(7, 2)]
        [InlineData(8, 2)]
        [InlineData(13, 2)]
        [InlineData(14, 3)]
        [InlineData(null, 4)]
        public void EndsWithString_CountLinesWithLength(int? length, int expectedLines) =>
            Assert.Equal(expectedLines, endsWithString.CountLines(length));

        [Theory]
        [InlineData(0, 0, 7)]
        [InlineData(1, 7, 7)]
        [InlineData(2, 14, 7)]
        [InlineData(3, 21, 7)]
        public void EndsWithString_LineMap(int line, int expectedOffset, int expectedCount)
        {
            Assert.Equal(expectedOffset, endsWithString[line].Offset);
            Assert.Equal(expectedCount, endsWithString[line].Count);
        }

        [Theory]
        [InlineData(6, 4)]
        //[InlineData(13, 7)]
        public void EndsWithNewLine_Substring(int offset, int count)
        {
            var expectedSource = new string(endsWithNewLine.Source.ToArray()).Substring(offset, count);
            var subMap = endsWithNewLine.Substring(offset, count);
            Assert.Equal(expectedSource, new string(subMap.Source.ToArray()));
            
            var expectedMap = SourceLineMap.From(expectedSource);
            Assert.Equal(expectedMap.CountLines(null), subMap.CountLines(null));
            Assert.Equal(expectedMap.ToArray(), subMap.ToArray());
        }

    }
}