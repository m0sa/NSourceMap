using System;
using System.Collections;
using System.Collections.Generic;

namespace NSourceMap
{
    public sealed class SourceLineMap : IReadOnlyList<ArraySegment<char>>
    {
        private const char LF = '\n';
        private readonly ArraySegment<char>[] map;
        public ArraySegment<char> Source { get; }

        private SourceLineMap(ArraySegment<char> source)
        {
            Source = source;
            var mapBuilder = new List<ArraySegment<char>>();
            for (int pos = source.Offset, nextPos = source.Offset, maxPos = source.Offset + source.Count;;)
            {
                nextPos = Array.IndexOf(source.Array, LF, pos);
                if (nextPos == -1 || nextPos >= maxPos)
                {
                    mapBuilder.Add(new ArraySegment<char>(source.Array, pos, maxPos - pos));
                    break;
                }
                mapBuilder.Add(new ArraySegment<char>(source.Array, pos, nextPos - pos + 1));
                pos = nextPos + 1;
            }
            map = mapBuilder.ToArray();
        }

        public static SourceLineMap From(string source) => new SourceLineMap(new ArraySegment<char>(source.ToCharArray()));
        public SourceLineMap Substring(int offset) => new SourceLineMap(new ArraySegment<char>(Source.Array, Source.Offset + offset, Source.Count - offset));
        public SourceLineMap Substring(int offset, int count) => new SourceLineMap(new ArraySegment<char>(Source.Array, Source.Offset + offset, count));
        // TODO make Substring faster by reusing the existing mappings instead of recalculating them

        /// <summary>
        /// Counts lines up to the given length.
        /// </summary>
        public int CountLines(int? length = null)
        {
            var len = length ?? Source.Count;
            if (len >= Source.Count) return map.Length;
            var i = 0;
            while (i < map.Length)
            {
                if (len < map[i].Offset)
                    break;
                i++;
            }
            return i;
        }

        public ArraySegment<char> this[int index] => map[index];        
        public int Count => map.Length;
        IEnumerator IEnumerable.GetEnumerator() => map.GetEnumerator();
        IEnumerator<ArraySegment<char>> IEnumerable<ArraySegment<char>>.GetEnumerator() => ((IEnumerable<ArraySegment<char>>)map).GetEnumerator();

    }
}