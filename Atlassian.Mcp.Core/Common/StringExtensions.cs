using System.Text;

namespace Atlassian.Mcp.Core.Common;

public static class StringExtensions
{
    extension(string value)
    {
        public int Utf8Length => Encoding.UTF8.GetByteCount(value);

        /// <summary>
        /// Усекает строку так, чтобы её UTF-8 представление не превышало
        /// <paramref name="maxBytes"/>, не разрывая символ.
        /// </summary>
        public string TruncateToBytes(int maxBytes)
        {
            var length = 0;
            var index = 0;

            while (index < value.Length)
            {
                var rune = Rune.GetRuneAt(value, index);
                length += rune.Utf8SequenceLength;

                if (length > maxBytes)
                {
                    return value[..index];
                }

                index += rune.Utf16SequenceLength;
            }

            return value;
        }
    }
}
