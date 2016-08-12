using System;

namespace NSourceMap
{
    public static class Preconditions
    {
        public static void checkState(bool expression)
        {
            if (!expression)
                throw new InvalidOperationException();
        }

        public static void checkState(bool expression, string message)
        {
            if (!expression)
                throw new InvalidOperationException(message);
        }

        public static void checkState(bool expression, string format, params object[] formatArgs)
        {
            if (!expression)
                throw new InvalidOperationException(string.Format(format, formatArgs));
        }
    }
}