using System.Collections.Generic;

namespace NSourceMap
{
    /// <summary>
    /// A SourceMappingReversable is a SourceMapping that can provide the reverse (source → target) source mapping.
    /// </summary>
    public interface ISourceMappingReversable : ISourceMapping
    {
        ICollection<string> OriginalSources { get; }

        /// <summary>
        /// Given a source file, line, and column, return the reverse mapping (source → target).
        /// A collection is returned as in some cases (like a function being inlined), one source line
        /// may map to more then one target location. An empty collection is returned if there were
        /// no matches.
        /// </summary>
        /// <param name="originalFile">the source file</param>
        /// <param name="line">the source line</param>
        /// <param name="column">the source column</param>
        /// <returns>the reverse mapping (source → target)</returns>
        ICollection<OriginalMapping> GetReverseMapping(string originalFile, int line, int column);
    }
}