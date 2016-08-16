using System.Linq;
using System.Collections.Generic;
using Xunit;
using NSourceMap;

namespace NSourceMap.Tests
{
    public class SourceMapConsumerTest
    {
        [Fact]
        public void SimpleSampleTest()
        {
            // from http://evanw.github.io/source-map-visualization/
            var consumer = new SourceMapConsumer();
            consumer.Parse(@"
            {
                ""version"":3,
                ""file"":""compiled.js"",
                ""sources"":[""<stdin>""],
                ""mappings"":""AAAA;AAAC,GAAC;AAAC,GAAC;AAAC,GAAC;AAAC,MAAG,IAAI,EAAC;OAAG;AAAC,SAAK;QAAG,KAAK,EAAC;SAAG;AAAC,WAAK;SAAG;AAAC;;CAAC"",
                ""sourcesContent"": [""{1;2;3;if(true)100;else if(false)200;else 300;}""]
            }");

            /* compiled.js:
{
  1;
  2;
  3;
  if (true) {
    100;
  } else {
    if (false) {
      200;
    } else {
      300;
    }
  }
}
             */

            OriginalMapping mapping;

            mapping = consumer.GetMappingForLine(2, 4); // semicolon on line 2
            Assert.Equal(1, mapping.LineNumber);
            Assert.Equal(3, mapping.ColumnPosition);
            
            mapping = consumer.GetMappingForLine(5, 9); // the true in if(true)
            Assert.Equal(1, mapping.LineNumber);
            Assert.Equal(11, mapping.ColumnPosition);

            mapping = consumer.GetMappingForLine(8, 9); // the false in if(false)
            Assert.Equal(1, mapping.LineNumber);
            Assert.Equal(28, mapping.ColumnPosition);
        }

        [Fact]
        public void SimpleGeneratorTest()
        {
            var generator = new SourceMapGenerator();
            var stdin = "<stdin>";
            generator.AddMapping(stdin, new FilePosition(0, 0), new FilePosition(0, 0), new FilePosition(1, 0));
            generator.AddMapping(stdin, new FilePosition(0, 1), new FilePosition(1, 0), new FilePosition(1, 3));
            generator.AddMapping(stdin, new FilePosition(0, 2), new FilePosition(1, 3), new FilePosition(2, 0));
            generator.AddMapping(stdin, new FilePosition(0, 3), new FilePosition(2, 0), new FilePosition(3, 0));
            
            var map = generator.Generate();

            Assert.StartsWith("AAAA;AAAC,GAAC;AAAC", map.mappings);
        }
    }
}