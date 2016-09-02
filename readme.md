# NSourceMap

A .NET library for reading and writing sourcemaps.

# Usage

See the [tests](blob/master/tests/NSourceMap.Tests/SourceMapTest.cs)

# Development environment
- [Visual Studio Code](https://code.visualstudio.com/Download)
- [.NET Core SDK](https://github.com/dotnet/cli#installers-and-binaries)

# Credits

Some parts have been ported from
[google/closure-compiler (java)](https://github.com/google/closure-compiler/tree/master/src/com/google/debugging/sourcemap) 
and [mozilla/source-map (javascript)](https://github.com/mozilla/source-map/tree/master/lib)

This [MSDN article](https://blogs.msdn.microsoft.com/davidni/2016/03/14/source-maps-under-the-hood-vlq-base64-and-yoda/) has been a great resource for learning about base64 VLQ encoding.

Great online tools for viewing and debugging sourcemaps:

- http://murzwin.com/base64vlq.html
- http://evanw.github.io/source-map-visualization/
