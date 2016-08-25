using System.Linq;
using System.Collections.Generic;
using Xunit;
using NSourceMap;

namespace NSourceMap.Tests
{
    public static class Program
    {
        public static void Main(params string[] args)
        {
            // System.Diagnostics.Debugger.Launch();
            new SourceMapTest().DecodeEncodeTest();
        }
    }
}