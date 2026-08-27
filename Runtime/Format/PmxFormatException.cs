using System;
using System.IO;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxFormatException : IOException
    {
        public PmxFormatException(string section, long byteOffset, string message)
            : this(section, byteOffset, message, null)
        {
        }

        public PmxFormatException(string section, long byteOffset, string message, Exception innerException)
            : base(CreateMessage(section, byteOffset, message), innerException)
        {
            Section = section;
            ByteOffset = byteOffset;
        }

        public string Section { get; }

        public long ByteOffset { get; }

        private static string CreateMessage(string section, long byteOffset, string message)
        {
            if (string.IsNullOrWhiteSpace(section))
            {
                throw new ArgumentException("A PMX section name is required.", nameof(section));
            }

            if (byteOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteOffset));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("An error description is required.", nameof(message));
            }

            return $"Invalid PMX data in section '{section}' at byte offset {byteOffset} (0x{byteOffset:X}): {message}";
        }
    }
}
