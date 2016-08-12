using System.Linq;
using System.Collections.Generic;
using Xunit;
using static NSourceMap.Base64VLQ;

namespace NSourceMap.Tests
{
    public class Base64VLQTest
    {
        // examples from https://blogs.msdn.microsoft.com/davidni/2016/03/14/source-maps-under-the-hood-vlq-base64-and-yoda/
        [Theory]
        [InlineData("A", "0")]
        [InlineData("CuBwcO", "1|23|456|7")]
        [InlineData("AACKA", "0|0|1|5|0")]
        [InlineData("IACIC", "4|0|1|4|1")]
        [InlineData("MACTC", "6|0|1|-9|1")]
        public void VLQTest(string vlq, string numbers)
        {
            var numbersToEncode = numbers.Split('|').Select(int.Parse).ToArray();

            Assert.Equal(vlq, VLQEncode(numbersToEncode));

            var decodedNums = new List<int>();
            for(var i = 0; i < vlq.Length; i++)
                i = VLQDecode(vlq, i, decodedNums);
            Assert.Equal(numbers, string.Join("|", decodedNums));
        }
    }
}