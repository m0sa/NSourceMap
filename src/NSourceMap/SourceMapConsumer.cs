using System;
using System.Collections.Generic;
using System.Linq;
using Jil;
using static NSourceMap.Constants;
    
namespace NSourceMap
{
    public sealed class SourceMapConsumer
    {
        private int _lineCount;

        // Slots in the _lines list will be null if the line does not have any entries.
        private List<List<Entry>> _lines;
        private string[] _names;

        private string[] _sources;

        public string SourceRoot { get; private set; }

        private List<Mapping> _mappings;

        public void Parse(string contents)
        {
            try
            {
                var sourceMapObject = JSON.Deserialize<SourceMapObject>(contents);
                Parse(sourceMapObject);
            }
            catch (DeserializationException ex)
            {
                throw new SourceMapParseException("Failed parsing contents", ex);
            }
        }

        public Mapping GetMappingForLine(FilePosition pos)
        {
            var lineNumber = pos.Line;
            var column = pos.Column;

            if (lineNumber < 0 || lineNumber >= _lines.Count)
            {
                return null;
            }

            Preconditions.checkState(lineNumber >= 0);
            Preconditions.checkState(column >= 0);

            // If the line is empty return the previous mapping.
            var entries = _lines[lineNumber];
            if (entries == null)
            {
                return GetPreviousMapping(lineNumber);
            }

            Preconditions.checkState(entries.Any());
            if (entries[0].GeneratedColumn > column)
            {
                return GetPreviousMapping(lineNumber);
            }

            var index = Search(entries, column, 0, entries.Count - 1);
            Preconditions.checkState(index >= 0, "unexpected:{0}", index);
            return GetOriginalMappingForEntry(entries[index], lineNumber);
        }
        
        // originalFile path ==> original line ==> target mappings 
        private Dictionary<string, Dictionary<int, List<Mapping>>> _reverseSourceMapping;

        public ICollection<string> OriginalSources => _sources.ToArray();

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
        public ICollection<Mapping> GetReverseMapping(string originalFile, FilePosition position)
        {
            // TODO(user): This implementation currently does not make use of the column parameter.
            // Synchronization needs to be handled by callers.
            if (_reverseSourceMapping == null)
            {
                _reverseSourceMapping = CreateReverseMapping();
            }

            Dictionary<int, List<Mapping>> sourceLineToCollectionMap;
            List<Mapping> mappings;


            if (_reverseSourceMapping.TryGetValue(originalFile, out sourceLineToCollectionMap) &&
                sourceLineToCollectionMap.TryGetValue(position.Line, out mappings))
            {
                return mappings;
            }
            return new Mapping[0];
        }

        private void Parse(SourceMapObject sourceMapObject)
        {
            if (sourceMapObject.version != 3)
            {
                throw new SourceMapParseException("Unknown version: " + sourceMapObject.version);
            }

            if (string.IsNullOrWhiteSpace(sourceMapObject.file))
            {
                throw new SourceMapParseException("File entry is empty");
            }

            _lineCount = sourceMapObject.lineCount ?? -1;
            SourceRoot = sourceMapObject.sourceRoot;
            _sources = sourceMapObject.sources;
            _names = sourceMapObject.names;

            _lines = _lineCount >= 0 ? new List<List<Entry>>(_lineCount) : new List<List<Entry>>();
            _mappings = new List<Mapping>();

            new MappingBuilder(this, sourceMapObject.mappings).Build();
        }

        /// <summary>
        /// Perform a binary search on the array to find a section that covers the target column.
        /// </summary>
        private static int Search(List<Entry> entries, int target, int start, int end)
        {
            while (true)
            {
                var mid = (end - start)/2 + start;
                var compare = entries[mid].GeneratedColumn - target;
                if (compare == 0)
                {
                    return mid;
                }
                if (compare < 0)
                {
                    // it is in the upper half
                    start = mid + 1;
                    if (start > end)
                    {
                        return end;
                    }
                }
                else
                {
                    // it is in the lower half
                    end = mid - 1;
                    if (end < start)
                    {
                        return end;
                    }
                }
            }
        }

        /// <summary>
        /// Compare an array entry's column value to the target column value.
        /// </summary>
        private static int CompareEntry(List<Entry> entries, int entry, int target) =>
            entries[entry].GeneratedColumn - target;

        /// <summary>
        /// Returns the mapping entry that proceeds the supplied line or null if no such entry exists.
        /// </summary>
        private Mapping GetPreviousMapping(int lineNumber)
        {
            do
            {
                if (lineNumber == 0)
                {
                    return null;
                }
                lineNumber--;
            } while (_lines[lineNumber] == null);
            var entries = _lines[lineNumber];
            return GetOriginalMappingForEntry(entries.Last(), lineNumber);
        }

        private Mapping GetOriginalMappingForEntry(Entry entry, int generatedLine) =>
            entry.SourceFileId == UNMAPPED
                ? null
                : new Mapping(_sources[entry.SourceFileId], new FilePosition(entry.SourceLine, entry.SourceColumn), new FilePosition(generatedLine, entry.GeneratedColumn), entry.NameId == UNMAPPED ? null : _names[entry.NameId]);

        /// <summary>
        /// Reverse the source map; the created mapping will allow us to quickly go 
        /// from a source file and line number to a collection of target OriginalMappings.
        /// </summary>
        private Dictionary<string, Dictionary<int, List<Mapping>>> CreateReverseMapping()
        {
            var result = new Dictionary<string, Dictionary<int, List<Mapping>>>();

            for (var targetLine = 0; targetLine < _lines.Count; targetLine++)
            {
                var entries = _lines[targetLine];

                if (entries == null) continue;

                foreach (var entry in entries)
                {
                    if (entry.SourceFileId == UNMAPPED || entry.SourceLine == UNMAPPED) continue;

                    var originalFile = _sources[entry.SourceFileId];

                    if (!result.ContainsKey(originalFile))
                    {
                        result.Add(originalFile, new Dictionary<int, List<Mapping>>());
                    }

                    var lineToCollectionMap = result[originalFile];

                    var sourceLine = entry.SourceLine;

                    if (!lineToCollectionMap.ContainsKey(sourceLine))
                    {
                        lineToCollectionMap.Add(sourceLine, new List<Mapping>(1));
                    }

                    var mappings = lineToCollectionMap[sourceLine];

                    mappings.Add(GetOriginalMappingForEntry(entry, targetLine));
                }
            }
            return result;
        }
        
        public IEnumerable<Mapping> Mappings => _mappings; // _lines.SelectMany((entries, line) => entries?.Select(entry => GetOriginalMappingForEntry(entry, line)) ?? Enumerable.Empty<Mapping>());

        private class MappingBuilder
        {
            private const int MAX_ENTRY_VALUES = 5;
            private readonly string _lineMap;

            private readonly SourceMapConsumer _parent;
            private int _line;
            private int _previousCol;
            private int _previousNameId;
            private int _previousSrcColumn;
            private int _previousSrcId;
            private int _previousSrcLine;

            public MappingBuilder(SourceMapConsumer parent, string lineMap)
            {
                _lineMap = lineMap;
                _parent = parent;
            }

            public void Build()
            {
                var entries = new List<Entry>();
                var temp = new List<int>(MAX_ENTRY_VALUES);
                for (var i = 0; i < _lineMap.Length;)
                {
                    switch (_lineMap[i])
                    {
                        case ',':
                            // ',' denotes a new entry on the same _line, just consume it
                            i++;
                            break;
                        case ';':
                            // ';' denotes a new _line.
                            // The _line is complete, store the result
                            CompleteLine(entries);

                            i++; // consume token
                            break;
                        default:
                            // grab the next entry for the current _line.
                            for (; i < _lineMap.Length && _lineMap[i] != ';' && _lineMap[i] != ','; i++)
                            {
                                i = Base64VLQ.VLQDecode(_lineMap, i, temp);
                            }
                            var entry = DecodeEntry(temp);
                            ValidateEntry(entry);
                            entries.Add(entry);
                            break;
                    }
                }

                // Some source map generator (e.g.UglifyJS) generates _lines without
                // a trailing _line separator. So add the rest of the content.
                if (entries.Any())
                {
                    CompleteLine(entries);
                }
            }

            private void CompleteLine(List<Entry> entries)
            {
                // The _line is complete, store the result for the _line,
                // null if the _line is empty.
                if (entries.Any())
                {
                    _parent._mappings.AddRange(entries.Select(x => _parent.GetOriginalMappingForEntry(x, _parent._lines.Count)));
                    _parent._lines.Add(entries.ToList());
                    entries.Clear(); // empty list for the next _line.
                }
                else
                {
                    _parent._lines.Add(null);
                }
                _line++;
                _previousCol = 0;
            }

            private void ValidateEntry(Entry entry)
            {
                Preconditions.checkState((_parent._lineCount < 0) || (_line < _parent._lineCount), "_line={0}, _lineCount={1}",
                    _line, _parent._lineCount);
                Preconditions.checkState(entry.SourceFileId == UNMAPPED || entry.SourceFileId < _parent._sources.Length);
                Preconditions.checkState(entry.NameId == UNMAPPED || entry.NameId < _parent._names.Length);
            }

            private Entry DecodeEntry(List<int> vals)
            {
                Entry entry;
                var entryValues = vals.Count;
                switch (entryValues)
                {
                    // The first values, if present are in the following order:
                    //   0: the starting column in the current _line of the generated file
                    //   1: the id of the original source file
                    //   2: the starting _line in the original source
                    //   3: the starting column in the original source
                    //   4: the id of the original symbol name
                    // The values are relative to the last encountered value for that field.
                    // Note: the previously column value for the generated file is reset
                    // to '0' when a new _line is encountered.  This is done in the 'Build'
                    // method.

                    case 1:
                        // An unmapped section of the generated file.
                        entry = Entry.Unmapped(
                            vals[0] + _previousCol);
                        // Set the values see for the next entry.
                        _previousCol = entry.GeneratedColumn;
                        break;

                    case 4:
                        // A mapped section of the generated file.
                        entry = Entry.Unnamed(
                            vals[0] + _previousCol,
                            vals[1] + _previousSrcId,
                            vals[2] + _previousSrcLine,
                            vals[3] + _previousSrcColumn);
                        // Set the values see for the next entry.
                        _previousCol = entry.GeneratedColumn;
                        _previousSrcId = entry.SourceFileId;
                        _previousSrcLine = entry.SourceLine;
                        _previousSrcColumn = entry.SourceColumn;
                        break;

                    case 5:
                        // A mapped section of the generated file, that has an associated
                        // name.
                        entry = Entry.Named(
                            vals[0] + _previousCol,
                            vals[1] + _previousSrcId,
                            vals[2] + _previousSrcLine,
                            vals[3] + _previousSrcColumn,
                            vals[4] + _previousNameId);
                        // Set the values see for the next entry.
                        _previousCol = entry.GeneratedColumn;
                        _previousSrcId = entry.SourceFileId;
                        _previousSrcLine = entry.SourceLine;
                        _previousSrcColumn = entry.SourceColumn;
                        _previousNameId = entry.NameId;
                        break;

                    default:
                        throw new InvalidOperationException("Unexpected number of values for entry:" + entryValues);
                }

                vals.Clear();
                return entry;
            }
        }

        private struct Entry
        {
            public int GeneratedColumn { get; }
            public int SourceFileId { get; }
            public int SourceLine { get; }
            public int SourceColumn { get; }
            public int NameId { get; }

            private Entry(int generatedColumn = UNMAPPED, int sourceFileId = UNMAPPED, int sourceLine = UNMAPPED, int sourceColumn = UNMAPPED, int nameId = UNMAPPED)
            {
                GeneratedColumn = generatedColumn;
                SourceFileId = sourceFileId;
                SourceLine = sourceLine;
                SourceColumn = sourceColumn;
                NameId = nameId;
            }

            public static Entry Unmapped(int column) => new Entry(column);
            public static Entry Unnamed(int column, int srcFile, int srcLine, int srcColumn) => new Entry(column, srcFile, srcLine, srcColumn);
            public static Entry Named(int column, int srcFile, int srcLine, int srcColumn, int name) => new Entry(column, srcFile, srcLine, srcColumn, name);
        }
    }
}