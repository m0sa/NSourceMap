using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jil;
using static NSourceMap.Constants;

namespace NSourceMap
{
    public class SourceMapGenerator
    {
        /// <summary>A pre-order traversal ordered list of mappings stored in this map.</summary>
        private readonly List<Mapping> _mappings = new List<Mapping>();

        /// <summary>A map of source names to source name index</summary>
        private readonly Dictionary<string, int> _sourceFileMap = new Dictionary<string, int>();

        /// <summary>A map of source names to source file contents</summary>
        private readonly Dictionary<string, string> _sourceFileContentMap = new Dictionary<string, string>();

        /// <summary>A map of source names to source name index</summary> 
        private readonly Dictionary<string, int> _originalNameMap = new Dictionary<string, int>();

        private string _lastSourceFile;

        private int _lastSourceFileIndex = UNMAPPED;

        // For validation store the last mapping added.
        private Mapping _lastMapping;

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
            _lastMapping = null;
            _sourceFileMap.Clear();
            _sourceFileContentMap.Clear();
            _originalNameMap.Clear();
            _lastSourceFile = null;
            _lastSourceFileIndex = UNMAPPED;
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

        /**
         * Adds a mapping for the given node.  Mappings must be added in order.
         * @param startPosition The position on the starting line
         * @param endPosition The position on the ending line.
         */
        public void AddMapping(
            string sourceName,
            FilePosition sourceStartPosition,
            FilePosition startPosition, FilePosition endPosition, string symbolName = null)
        {

            // Don't bother if there is not sufficient information to be useful.
            if (sourceName == null || sourceStartPosition.Line < 0)
            {
                return;
            }

            var adjustedStart = startPosition;
            var adjustedEnd = endPosition;

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

                if (endPosition.Line > 0)
                {
                    endOffsetPosition = 0;
                }

                adjustedStart = new FilePosition(
                    startPosition.Line + offsetLine,
                    startPosition.Column + startOffsetPosition);

                adjustedEnd = new FilePosition(
                    endPosition.Line + offsetLine,
                    endPosition.Column + endOffsetPosition);
            }

            // Create the new mapping.
            var mapping = new Mapping
            {
                sourceFile = sourceName,
                originalPosition = sourceStartPosition,
                symbolName = symbolName,
                startPosition = adjustedStart,
                endPosition = adjustedEnd
            };

            // Validate the mappings are in a proper order.
            if (_lastMapping != null)
            {
                var lastLine = _lastMapping.startPosition.Line;
                var lastColumn = _lastMapping.startPosition.Column;
                var nextLine = mapping.startPosition.Line;
                var nextColumn = mapping.startPosition.Column;
                Preconditions.checkState(nextLine > lastLine
                    || (nextLine == lastLine && nextColumn >= lastColumn),
                    "Incorrect source mappings order, previous : ({0},{1})\nnew : ({2},{3})",
                    lastLine, lastColumn, nextLine, nextColumn);
            }

            _lastMapping = mapping;
            _mappings.Add(mapping);
        }

        public void AddSourcesContent(string source, string content)
        {
            _sourceFileContentMap.Add(source, content);
        }

        /**
         * Writes out the source map in the following format (line numbers are for
         * reference only and are not part of the format):
         *
         * 1.  {
         * 2.    version: 3,
         * 3.    file: "out.js",
         * 4.    lineCount: 2,
         * 5.    sourceRoot: "",
         * 6.    sources: ["foo.js", "bar.js"],
         * 7.    names: ["src", "maps", "are", "fun"],
         * 8.    mappings: "a;;abcde,abcd,a;"
         * 9.    x_org_extension: value
         * 10. }
         *
         * Line 1: The entire file is a single JSON object
         * Line 2: File revision (always the first entry in the object)
         * Line 3: The name of the file that this source map is associated with.
         * Line 4: The number of lines represented in the source map.
         * Line 5: An optional source root, useful for relocating source files on a
         *     server or removing repeated prefix values in the "sources" entry.
         * Line 6: A list of sources used by the "mappings" entry relative to the
         *     sourceRoot.
         * Line 7: A list of symbol names used by the "mapping" entry.  This list
         *     may be incomplete.
         * Line 8: The mappings field.
         * Line 9: Any custom field (extension).
         */
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
                    sources = _sourceFileMap.OrderBy(x => x.Value).Select(x => x.Key).ToArray(),
                    mappings = mappings,
                    sourcesContent = _sourceFileMap.OrderBy(x => x.Value).Select(x =>
                    {
                        string src;
                        return _sourceFileContentMap.TryGetValue(x.Key, out src) ? src : null;
                    }).ToArray(),
                    names = _originalNameMap.OrderBy(x => x.Value).Select(x => x.Key).ToArray(),
                };
        }

        public string File { get; set;}

        private string SerializeMappings(out int lineCount)
        {
            var mappingsBuilder = new StringBuilder();

            // The destination.
            var previousLine = UNMAPPED;
            var previousColumn = 0;

            // Previous values used for storing relative ids.
            var previousSourceFileId = UNMAPPED;
            var previousSourceLine = UNMAPPED;
            var previousSourceColumn = UNMAPPED;
            var previousNameId = UNMAPPED;

            var maxLine = 0;
            // Mark any unused mappings.
            new MappingTraversal(this).Traverse((m, start, end) => maxLine = Math.Max(maxLine, m.endPosition?.Line ?? 0));

            // Adjust for the prefix.
            lineCount = maxLine + _prefixPosition.Line + 1;


            new MappingTraversal(this).Traverse((m, current, next) =>

            {
                if (previousLine != current.Line)
                {
                    previousColumn = 0;
                }

                if (current.Line != next.Line || current.Column != next.Column)
                {
                    // TODO(johnlenz): For some reason, we have mappings beyond the max line.
                    // So far they're just null mappings and we can ignore them.
                    // (If they're non-null, we assert-fail.)
                    if (current.Line < maxLine)
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
                            var sourceId = GetSourceId(m.sourceFile);
                            Base64VLQ.VLQEncode(sourceId - previousSourceFileId, mappingsBuilder);
                            previousSourceFileId = sourceId;

                            // The relative source file line and column
                            var srcline = m.originalPosition.Line;
                            var srcColumn = m.originalPosition.Column;
                            Base64VLQ.VLQEncode(srcline - previousSourceLine, mappingsBuilder);
                            previousSourceLine = srcline;

                            Base64VLQ.VLQEncode(srcColumn - previousSourceColumn, mappingsBuilder);
                            previousSourceColumn = srcColumn;

                            if (m.symbolName != null)
                            {
                                // The relative id for the associated symbol name
                                var nameId = GetNameId(m.symbolName);
                                Base64VLQ.VLQEncode(nameId - previousNameId, mappingsBuilder);
                                previousNameId = nameId;
                            }
                        }

                        previousLine = current.Line;
                        previousColumn = current.Column;
                    }
                    else
                    {
                        Preconditions.checkState(m == null);
                    }
                }

                for (var i = current.Line; i <= next.Line && i < maxLine; i++)
                {
                    if (i == next.Line)
                    {
                        break;
                    }

                    mappingsBuilder.Append(";"); // close line
                }
            });
            mappingsBuilder.Append(";");
            var mappings = mappingsBuilder.ToString();
            return mappings;
        }

        /**
         * A mapping from a given position in an input source file to a given position
         * in the generated code.
         */
        class Mapping
        {
            public string sourceFile;
            public FilePosition originalPosition;
            public FilePosition startPosition;
            public FilePosition endPosition;
            public string symbolName;
            public bool used;
        }

        private delegate void MappingVisitor(Mapping m, FilePosition start, FilePosition end);

        /**
         * Walk the mappings and visit each segment of the _mappings, unmapped
         * segments are visited with a null mapping, unused mapping are not visited.
         */
        private class MappingTraversal
        {
            // The last line and column written
            private FilePosition _previousPosition;
            private readonly SourceMapGenerator _parent;

            public MappingTraversal(SourceMapGenerator parent)
            {
                _parent = parent;
            }

            // Append the line mapping entries.
            public void Traverse(MappingVisitor visitor)
            {
                // The mapping list is ordered as a pre-order traversal.  The mapping
                // positions give us enough information to rebuild the stack and this
                // allows the building of the source map in O(n) time.
                var stack = new Stack<Mapping>();
                foreach (var m in _parent._mappings)
                {
                    // Find the closest ancestor of the current mapping:
                    // An overlapping mapping is an ancestor of the current mapping, any
                    // non-overlapping mappings are siblings (or cousins) and must be
                    // closed in the reverse order of when they encountered.
                    while (stack.Any() && !isOverlapped(stack.Peek(), m))
                    {
                        maybeVisit(visitor, stack.Pop());
                    }

                    // Any gaps between the current line position and the start of the
                    // current mapping belong to the parent.
                    maybeVisitParent(visitor, stack.Any() ? stack.Peek() : null, m);

                    stack.Push(m);
                }

                // There are no more children to be had, simply close the remaining
                // mappings in the reverse order of when they encountered.
                while (stack.Any())
                {
                    maybeVisit(visitor, stack.Pop());
                }
            }

            /**
             * @return Whether m1 ends before m2 starts.
             */
            private bool isOverlapped(Mapping m1, Mapping m2)
            {
                var l1 = m1.endPosition.Line;
                var l2 = m2.startPosition.Line;
                var c1 = m1.endPosition.Column;
                var c2 = m2.startPosition.Column;

                return (l1 == l2 && c1 >= c2) || l1 > l2;
            }

            /**
             * Write any needed entries from the current position to the end of the
             * provided mapping.
             */
            private void maybeVisit(MappingVisitor v, Mapping m)
            {
                maybeVisitParent(v, m, m);
            }

            /**
             * Write any needed entries to complete the provided mapping.
             */
            private void maybeVisitParent(MappingVisitor v, Mapping parent, Mapping m)

            {
                var nextPos = m.startPosition.Prefix(_parent._prefixPosition);

                // If the previous value is null, no mapping exists.
                Preconditions.checkState(_previousPosition.Line < nextPos.Line || _previousPosition.Column <= nextPos.Column);
                if (_previousPosition.Line < nextPos.Line || (_previousPosition.Line == nextPos.Line && _previousPosition.Column < nextPos.Column))
                {
                    visit(v, parent, nextPos);
                }
            }

            /**
             * Write any entries needed between the current position the next position
             * and update the current position.
             */
            private void visit(MappingVisitor v, Mapping m, FilePosition nextPos)
            {
                Preconditions.checkState(_previousPosition.Line <= nextPos.Line);
                Preconditions.checkState(_previousPosition.Line < nextPos.Line || _previousPosition.Column < nextPos.Column);

                if (_previousPosition.Line == nextPos.Line && _previousPosition.Column == nextPos.Column)
                {
                    // Nothing to do.
                    throw new InvalidOperationException();
                }

                v(m, _previousPosition, nextPos);

                _previousPosition = nextPos;
            }
        }

        private int GetSourceId(string sourceName)
        {
            if (sourceName == _lastSourceFile) return _lastSourceFileIndex;
            _lastSourceFile = sourceName;

            if (!_sourceFileMap.TryGetValue(sourceName, out _lastSourceFileIndex))
            {
                _sourceFileMap.Add(sourceName, _sourceFileMap.Count);
                _lastSourceFileIndex = _sourceFileMap.Count;
            }
            return _lastSourceFileIndex;
        }

        private int GetNameId(string symbolName)
        {
            int originalNameIndex;

            if (!_originalNameMap.TryGetValue(symbolName, out originalNameIndex))
            {
                originalNameIndex = _originalNameMap.Count;
                _originalNameMap.Add(symbolName, originalNameIndex);
            }
            return originalNameIndex;
        }

    }
}