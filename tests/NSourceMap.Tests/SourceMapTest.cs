using System.Linq;
using System.Collections.Generic;
using Xunit;
using NSourceMap;

namespace NSourceMap.Tests
{
    public class SourceMapTest
    {
        const string SimpleSourceMap = @"
            {
                ""version"":3,
                ""file"":""compiled.js"",
                ""sources"":[""<stdin>""],
                ""mappings"":""AAAA;AAAC,GAAC;AAAC,GAAC;AAAC,GAAC;AAAC,MAAG,IAAI,EAAC;OAAG;AAAC,SAAK;QAAG,KAAK,EAAC;SAAG;AAAC,WAAK;SAAG;AAAC;;CAAC"",
                ""sourcesContent"": [""{1;2;3;if(true)100;else if(false)200;else 300;}""]
            }";
            /* <stdin>:
{1;2;3;if(true)100;else if(false)200;else 300;}
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
            /* mappings (RAW, interpreted):
0)  [0,0,0,0]                                       ([0,0](#0)=>[0,0])
1)  [0,0,0,1], [3,0,0,1]                            ([0,1](#0)=>[1,0]) | ([0,2](#0)=>[1,3])
2)  [0,0,0,1], [3,0,0,1]                            ([0,3](#0)=>[2,0]) | ([0,4](#0)=>[2,3])
3)  [0,0,0,1], [3,0,0,1]                            ([0,5](#0)=>[3,0]) | ([0,6](#0)=>[3,3])
4)  [0,0,0,1], [6,0,0,3], [4,0,0,4], [2,0,0,1]      ([0,7](#0)=>[4,0]) | ([0,10](#0)=>[4,6]) | ([0,14](#0)=>[4,10]) | ([0,15](#0)=>[4,12])
5)  [7,0,0,3]                                       ([0,18](#0)=>[5,7])
6)  [0,0,0,1], [9,0,0,5]                            ([0,19](#0)=>[6,0]) | ([0,24](#0)=>[6,9])
7)  [8,0,0,3], [5,0,0,5], [2,0,0,1]                 ([0,27](#0)=>[7,8]) | ([0,32](#0)=>[7,13]) | ([0,33](#0)=>[7,15])
8)  [9,0,0,3]                                       ([0,36](#0)=>[8,9])
9)  [0,0,0,1], [11,0,0,5]                           ([0,37](#0)=>[9,0]) | ([0,42](#0)=>[9,11])
10) [9,0,0,3]                                       ([0,45](#0)=>[10,9])
11) [0,0,0,1]                                       ([0,46](#0)=>[11,0])
13) [1,0,0,1]                                       ([0,47](#0)=>[13,1])
            */

        [Fact]
        public void SimpleSampleTest()
        {
            // from http://evanw.github.io/source-map-visualization/
            var consumer = new SourceMapConsumer();
            consumer.Parse(SimpleSourceMap);

            OriginalMapping mapping;

            mapping = consumer.GetMappingForLine(new FilePosition(1, 3)); // semicolon on line 2
            Assert.Equal(0, mapping.LineNumber);
            Assert.Equal(2, mapping.ColumnPosition);
            
            mapping = consumer.GetMappingForLine(new FilePosition(4, 8)); // the true in if(true)
            Assert.Equal(0, mapping.LineNumber);
            Assert.Equal(10, mapping.ColumnPosition);

            mapping = consumer.GetMappingForLine(new FilePosition(7, 8)); // the false in if(false)
            Assert.Equal(0, mapping.LineNumber);
            Assert.Equal(27, mapping.ColumnPosition);
        }

        [Fact]
        public void SimpleGeneratorTest()
        {
            var generator = new SourceMapGenerator();
            var stdin = "<stdin>";
            generator.AddMapping(new Mapping(stdin, new FilePosition(0, 0), new FilePosition(0, 0)));
            generator.AddMapping(new Mapping(stdin, new FilePosition(0, 1), new FilePosition(1, 0)));
            generator.AddMapping(new Mapping(stdin, new FilePosition(0, 2), new FilePosition(1, 3)));
            generator.AddMapping(new Mapping(stdin, new FilePosition(0, 3), new FilePosition(2, 0)));
            
            var map = generator.Generate();

            Assert.StartsWith("AAAA;AAAC,GAAC;AAAC", map.mappings);
        }
    }
}