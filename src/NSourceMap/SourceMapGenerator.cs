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

        /**
         * The position that the current source map is offset in the
         * buffer being used to generated the compiled source file.
         */
        private FilePosition _offsetPosition = new FilePosition(0, 0);

        /**
         * The position that the current source map is offset in the
         * generated the compiled source file by the addition of a
         * an output wrapper prefix.
         */
        private FilePosition _prefixPosition = new FilePosition(0, 0);

        /**
         * The source root path for relocating source fails or avoid duplicate values
         * on the source entry.
         */
        public string SourceRoot { get; set; }

        public void Reset()
        {
            _mappings.Clear();
            _sourceFileContentMap.Clear();
            _sourceFileMap.Clear();
            _nameMap.Clear();
            _offsetPosition = new FilePosition(0, 0);
            _prefixPosition = new FilePosition(0, 0);
        }

        /**
         * Sets the prefix used for wrapping the generated source file before
         * it is written. This ensures that the source map is adjusted for the
         * change in character offsets.
         *
         * @param prefix The prefix that is added before the generated source code.
         */
        public void SetWrapperPrefix(string prefix)
        {
            // Determine the current line and character position.
            int prefixLine = 0;
            int prefixIndex = 0;

            for (int i = 0; i < prefix.Length; ++i)
            {
                if (prefix[i] == '\n')
                {
                    prefixLine++;
                    prefixIndex = 0;
                }
                else
                {
                    prefixIndex++;
                }
            }

            _prefixPosition = new FilePosition(prefixLine, prefixIndex);
        }

        /**
         * Sets the source code that exists in the buffer for which the
         * generated code is being generated. This ensures that the source map
         * accurately reflects the fact that the source is being appended to
         * an existing buffer and as such, does not start at line 0, position 0
         * but rather some other line and position.
         *
         * @param offsetLine The index of the current line being printed.
         * @param offsetIndex The column index of the current character being printed.
         */
        public void SetStartingPosition(int offsetLine, int offsetIndex)
        {
            Preconditions.checkState(offsetLine >= 0);
            Preconditions.checkState(offsetIndex >= 0);
            _offsetPosition = new FilePosition(offsetLine, offsetIndex);
        }

        /// <summary>Adds a mapping for the given node.  Mappings must be added in order.///
        public void AddMapping (Mapping m)
        {
            var sourceName = m.Source;
            var sourceStartPosition = m.Original;

            // Don't bother if there is not sufficient information to be useful.
            if (sourceName == null || sourceStartPosition.Line < 0)
            {
                return;
            }

            var startPosition = m.Generated;
            var adjustedStart = startPosition;

            if (_offsetPosition.Line != 0
                || _offsetPosition.Column != 0)
            {
                // If the mapping is found on the first line, we need to offset
                // its character position by the number of characters found on
                // the *last* line of the source file to which the code is
                // being generated.
                var offsetLine = _offsetPosition.Line;
                var startOffsetPosition = _offsetPosition.Column;
                var endOffsetPosition = _offsetPosition.Column;

                if (startPosition.Line > 0)
                {
                    startOffsetPosition = 0;
                }

                adjustedStart = new FilePosition(
                    startPosition.Line + offsetLine,
                    startPosition.Column + startOffsetPosition);
            }

            // Create the new mapping.
            var mapping = new Mapping(sourceName, sourceStartPosition, adjustedStart, m.Name);

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
                    sourcesContent = _sourceFileMap.Items.Select(x =>
                    {
                        string src;
                        return _sourceFileContentMap.TryGetValue(x, out src) ? src : null;
                    }).ToArray(),
                    names = _nameMap.Items.ToArray(),
                };
        }

        public string File { get; set;}

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

            // Mark any unused mappings.
            var maxLine = Mappings(sort: false).Max(x => x.Generated?.Line ?? 0);

            // Adjust for the prefix.
            lineCount = maxLine + _prefixPosition.Line + 1;

            foreach(var m in Mappings(sort: true))
            {
                var current = m.Generated;
                if (previousLine != current.Line)
                {
                    previousColumn = 0;
                }

                if (current.Line != previousLine || current.Column != previousColumn)
                {
                    for (var i = Math.Max(0, previousLine); i < current.Line && i < maxLine; i++)
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
    
    public class Mapping
    {
        public Mapping(string source, FilePosition original, FilePosition generated, string name = null, FilePosition generatedEnd = null)
        {
            Source = source;
            Original = original;
            Generated = generated;
            Name = name;
            GeneratedEnd = generatedEnd;
        }

        /// <summary>The original source file (relative to the sourceRoot).</summary>
        public string Source { get; }

        /// <summary>An object with the original line and column positions.</summary>
        public FilePosition Original { get; }

        /// <summary>An object with the generated line and column positions.</summary>
        public FilePosition Generated { get; }

        /// <summary>An object with the generated line and column positions.</summary>
        public FilePosition GeneratedEnd { get; }

        /// <summary>An optional original token name for this mapping.</summary>
        public string Name { get; }
    }
}