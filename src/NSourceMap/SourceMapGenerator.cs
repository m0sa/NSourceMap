using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NSourceMap.Constants;

namespace NSourceMap
{
    public class SourceMapGenerator
    {
        /// <summary>A pre-order traversal ordered list of mappings stored in this map.</summary>
        private readonly List<Mapping> _mappings = new List<Mapping>();

        private readonly Dictionary<string, string> _sourceFileContentMap = new Dictionary<string, string>();

        private readonly StringIndex _sourceFileMap = new StringIndex();

        private readonly StringIndex _nameMap = new StringIndex();

        public string SourceRoot { get; set; }

        public void Reset()
        {
            _mappings.Clear();
            _sourceFileContentMap.Clear();
            _sourceFileMap.Clear();
            _nameMap.Clear();
        }

        /// <summary>Adds a mapping for the given node.</summary>
        public void AddMapping (Mapping m)
        {
            var sourceName = m.Source;
            var sourceStartPosition = m.Original;

            // Don't bother if there is not sufficient information to be useful.
            if (sourceName == null || sourceStartPosition.Line < 0)
            {
                return;
            }

            // Create the new mapping.
            var mapping = new Mapping(sourceName, sourceStartPosition, m.Generated, m.Name);

            _mappings.Add(mapping);
        }

        public void AddSourcesContent(string source, string content)
        {
            _sourceFileContentMap.Add(source, content);
        }

        public SourceMapObject Generate()
        {
            int lineCount;
            var mappings = SerializeMappings(out lineCount);

            return
                new SourceMapObject
                {
                    version = 3,
                    file = File,
                    lineCount = lineCount,
                    sourceRoot = SourceRoot,
                    sources = _sourceFileMap.Items.ToArray(),
                    mappings = mappings,
                    sourcesContent = _sourceFileContentMap.Any() 
                        ? _sourceFileMap.Items.Select(x =>
                        {
                            string src;
                            return _sourceFileContentMap.TryGetValue(x, out src) ? src : null;
                        }).ToArray()
                        : null,
                    names = _nameMap.Items.ToArray(),
                };
        }

        public string File { get; set;}

        public int? LineCount { get; set; }

        private string SerializeMappings(out int lineCount)
        {
            var mappingsBuilder = new StringBuilder();

            // The destination.
            var previousLine = UNMAPPED;
            var previousColumn = UNMAPPED;

            // Previous values used for storing relative ids.
            var previousSourceFileId = 0;
            var previousSourceLine = 0;
            var previousSourceColumn = 0;
            var previousNameId = 0;
            
            lineCount = LineCount ??  (Mappings(sort: false).Max(x => x.Generated?.Line ?? 0) + 1);

            foreach(var m in Mappings(sort: true))
            {
                var current = m.Generated;
                if (previousLine != current.Line)
                {
                    previousColumn = 0;
                }

                if (current.Line != previousLine || current.Column != previousColumn)
                {
                    for (var i = Math.Max(0, previousLine); i < current.Line && i < lineCount; i++)
                    {
                        mappingsBuilder.Append(";"); // close line
                    }

                    if (current.Line < lineCount)
                    {
                        if (previousLine == current.Line)
                        {
                            // not the first entry for the line
                            mappingsBuilder.Append(',');
                        }
                        var column = current.Column;
                        // The relative generated column number
                        Base64VLQ.VLQEncode(column - previousColumn, mappingsBuilder);
                        previousColumn = column;
                        if (m != null)
                        {
                            // The relative source file id
                            var sourceId = _sourceFileMap.IndexFor(m.Source);
                            Base64VLQ.VLQEncode(sourceId - previousSourceFileId, mappingsBuilder);
                            previousSourceFileId = sourceId;

                            // The relative source file line and column
                            var srcline = m.Original.Line;
                            var srcColumn = m.Original.Column;
                            Base64VLQ.VLQEncode(srcline - previousSourceLine, mappingsBuilder);
                            previousSourceLine = srcline;

                            Base64VLQ.VLQEncode(srcColumn - previousSourceColumn, mappingsBuilder);
                            previousSourceColumn = srcColumn;

                            if (m.Name != null)
                            {
                                // The relative id for the associated symbol name
                                var nameId = _nameMap.IndexFor(m.Name);
                                Base64VLQ.VLQEncode(nameId - previousNameId, mappingsBuilder);
                                previousNameId = nameId;
                            }
                        } else {

                        }

                        previousLine = current.Line;
                        previousColumn = current.Column;
                    }
                    else
                    {
                        Preconditions.checkState(m == null);
                    }
                } else {

                }
            }

            mappingsBuilder.Append(";");
            var mappings = mappingsBuilder.ToString();
            return mappings;
        }

        private class StringIndex
        {
            private string _lastString = null;
            private int _lastIndex = UNMAPPED;

            private readonly Dictionary<string, int> _mappingsLookup = new Dictionary<string, int>();

            private readonly List<string> _mappingsOrdered = new List<string>();

            public int IndexFor(string name)
            {
                if (name == _lastString) return _lastIndex;

                int newIndex;
                if (!_mappingsLookup.TryGetValue(name, out newIndex))
                {
                    newIndex = _mappingsLookup.Count;
                    _mappingsLookup.Add(name, newIndex);
                    _mappingsOrdered.Add(name);
                }

                _lastString = name;
                _lastIndex = newIndex;

                return newIndex;
            }

            public IEnumerable<string> Items => _mappingsOrdered.AsEnumerable();

            public void Clear()
            {
                _mappingsLookup.Clear();
                _mappingsOrdered.Clear();
            }
        }

        public IEnumerable<Mapping> Mappings(bool sort = false)
        {
            var result = _mappings.AsEnumerable();
            if (sort)
            {
                result = result
                    .OrderBy(x => x.Generated.Line)
                    .ThenBy(x => x.Generated.Column)
                    .ThenBy(x => x.Source)
                    .ThenBy(x => x.Original.Line)
                    .ThenBy(x => x.Original.Column)
                    .ThenBy(x => x.Name);
            }
            return result;
        }
    }
}