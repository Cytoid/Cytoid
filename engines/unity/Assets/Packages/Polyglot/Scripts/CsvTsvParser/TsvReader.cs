using System.Collections.Generic;

namespace Polyglot
{
    public static class TsvReader
    {
        /// <summary>
        /// Parses the TSV string.
        /// </summary>
        /// <returns>a two-dimensional array. first index indicates the row.  second index indicates the column.</returns>
        /// <param name="src">raw TSV contents as string</param>
        public static List<List<string>> Parse(string src)
        {
            var rows = new List<List<string>>();
            var lines = src.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var cols = new List<string>(line.Split('\t'));
                rows.Add(cols);
            }

            return rows;
        }
    }
}