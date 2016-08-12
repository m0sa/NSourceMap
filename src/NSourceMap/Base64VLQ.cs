using System.Collections.Generic;
using System.Text;

namespace NSourceMap
{
    // c# port of:
    // https://github.com/google/closure-compiler/blob/a369bab89a214feafd2a59c623a19b89ee91dc53/src/com/google/debugging/sourcemap/Base64VLQ.java
    internal static class Base64VLQ
    {
        private const int VLQ_BASE_SHIFT = 5;
        private const int VLQ_BASE = 1 << VLQ_BASE_SHIFT;
        private const int VLQ_BASE_MASK = VLQ_BASE - 1;
        private const int VLQ_CONTINUATION_BIT = VLQ_BASE;

        private const string BASE64_MAP =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789+/";
        
        /* generated via
        private static int[] CreateBase64ReverseMap() 
        {
            var max = BASE64_MAP.Max(x => x); // 122, 'z'
            var array = Enumerable.Repeat(-1, max + 1).ToArray();
            for (var i = 0; i < BASE64_MAP.Length; i++)
                array[(char)BASE64_MAP[i]] = i;
            return array;
        } */
        private static readonly int[] BASE64_REVERSE_MAP =
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
            -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, -1, -1, -1, -1, -1, -1, 26,
            27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
            39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51
        };

        /**
        * Converts from a two-complement value to a value where the sign bit is
        * is placed in the least significant bit.  For example, as decimals:
        *   1 becomes 2 (10 binary), -1 becomes 3 (11 binary)
        *   2 becomes 4 (100 binary), -2 becomes 5 (101 binary)
        */

        private static int ToVLQSigned(int value) =>
            value < 0
                ? (-value << 1) + 1
                : (value << 1) + 0;

        /**
        * Converts to a two-complement value from a value where the sign bit is
        * is placed in the least significant bit.  For example, as decimals:
        *   2 (10 binary) becomes 1, 3 (11 binary) becomes -1
        *   4 (100 binary) becomes 2, 5 (101 binary) becomes -2
        */

        private static int FromVLQSigned(int value) =>
            (value & 1) == 1
                ? -(value >> 1)
                : value >> 1;

        /**
        * Writes a VLQ encoded value
        */

        public static void VLQEncode(int value, StringBuilder writer)
        {
            value = ToVLQSigned(value);
            do
            {
                var digit = value & VLQ_BASE_MASK;
                value = value >> VLQ_BASE_SHIFT;
                if (value > 0)
                {
                    digit |= VLQ_CONTINUATION_BIT;
                }
                writer.Append(ToBase64(digit));
            } while (value > 0);
        }

        public static void VLQEncode(StringBuilder writer, params int[] values)
        {
            for (var i = 0; i < values.Length; i++)
                VLQEncode(values[i], writer);
        }

        public static string VLQEncode(params int[] values)
        {
            var writer = new StringBuilder();
            VLQEncode(writer, values);
            return writer.ToString();
        }

        /**
        * Decodes the next VLQValue from str
        */

        public static int VLQDecode(string source, int position, ICollection<int> target)
        {
            var result = 0;
            var shift = 0;
            for (; position < source.Length; position++)
            {
                var digit = FromBase64(source[position]);
                var continuation = (digit & VLQ_CONTINUATION_BIT) != 0;
                digit &= VLQ_BASE_MASK;
                result += digit << shift;
                if (!continuation) break;
                shift += VLQ_BASE_SHIFT;
            }

            target.Add(FromVLQSigned(result));
            return position;
        }


        private static char ToBase64(int digit) =>
            BASE64_MAP[digit];

        private static int FromBase64(char c) =>
            c > 'z' ? -1 : BASE64_REVERSE_MAP[c];
    }
}