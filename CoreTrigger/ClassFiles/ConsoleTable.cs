using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using CoreTrigger.ClassFiles;

namespace CoreTrigger.ClassFiles
{
    public class ConsoleTable
    {
        public IList<object> Columns { get; set; }
        public IList<object[]> Rows { get; protected set; }
        public int startingPoint = 0;
        public ConsoleTableOptions Options { get; protected set; }

        public ConsoleTable(params string[] columns)
            : this(new ConsoleTableOptions { Columns = new List<string>(columns) })
        {
        }

        public ConsoleTable(ConsoleTableOptions options)
        {
            if (options != null)
            {
                Options = options;
            }
            else
            {
                throw new ArgumentNullException("options");
            }
            Rows = new List<object[]>();
            Columns = new List<object>(options.Columns);
        }

        public ConsoleTable AddColumn(IEnumerable<string> names)
        {
            foreach (var name in names)
                Columns.Add(name);
            return this;
        }

        public ConsoleTable AddRow(params object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (!Columns.Any())
            {
                throw new Exception("Please set the columns first");
            }
            if (Columns.Count != values.Length)
            {
                throw new Exception(
                    $"The number columns in the row ({Columns.Count}) does not match the values ({values.Length}");
            }
            Rows.Add(values);
            return this;
        }

        public static ConsoleTable From<T>(IEnumerable<T> values)
        {
            var table = new ConsoleTable();

            var columns = GetColumns<T>();

            table.AddColumn(columns);

            foreach (
                    var propertyValues in
                    values.Select(value => columns.Select(column => GetColumnValue<T>(value, column))))
            {
                table.AddRow(propertyValues.ToArray());
            }
            return table;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Enumerable.Range(0, Columns.Count)
                .Select(i => " | {" + i + ",-" + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " |";

            // find the longest formatted line
            var maxRowLength = Math.Max(0, Rows.Any() ? Rows.Max(row => string.Format(format, row).Length) : 0);
            var columnHeaders = string.Format(format, Columns.ToArray());

            // longest line is greater of formatted columnHeader and longest row
            var longestLine = Math.Max(maxRowLength, columnHeaders.Length);

            // add each row
            var results = Rows.Select(row => string.Format(format, row)).ToList();

            // create the divider
            var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

            builder.AppendLine(divider);
            builder.AppendLine(columnHeaders);

            foreach (var row in results)
            {
                builder.AppendLine(divider);
                builder.AppendLine(row);
            }

            builder.AppendLine(divider);

            if (Options.EnableCount)
            {
                //builder.AppendLine("");
                //builder.AppendFormat(" Count: {0}", Rows.Count);
            }

            return builder.ToString();
        }

        public string ToMarkDownString()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.ToArray());

            // add each row
            var results = Rows.Select(row => string.Format(format, row)).ToList();

            // create the divider
            var divider = Regex.Replace(columnHeaders, @"[^|]", "-");

            builder.AppendLine(columnHeaders);
            builder.AppendLine(divider);
            results.ForEach(row => builder.AppendLine(row));

            return builder.ToString();
        }

        public string ToStringAlternative()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.ToArray());

            // add each row
            var results = Rows.Select(row => string.Format(format, row)).ToList();

            // create the divider
            var divider = Regex.Replace(columnHeaders, @"[^|]", "-");
            var dividerPlus = divider.Replace("|", "+");

            builder.AppendLine(dividerPlus);
            builder.AppendLine(columnHeaders);

            foreach (var row in results)
            {
                builder.AppendLine(dividerPlus);
                builder.AppendLine(row);
            }
            builder.AppendLine(dividerPlus);

            return builder.ToString();
        }

        private string Format(List<int> columnLengths)
        {
            var format = (Enumerable.Range(0, Columns.Count)
                .Select(i => " | {" + i + ",-" + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " |").Trim();
            return format;
        }

        private List<int> ColumnLengths()
        {
            var columnLengths = Columns
                .Select((t, i) => Rows.Select(x => x[i])
                    .Union(Columns)
                    .Where(x => x != null)
                    .Select(x => x.ToString().Length).Max())
                .ToList();
            return columnLengths;
        }

        public void Write(Format format = ClassFiles.Format.Default)
        {
            this.startingPoint = Console.CursorTop;
            switch (format)
            {
                case ClassFiles.Format.Default:
                    Console.WriteLine(ToString());
                    break;
                case ClassFiles.Format.MarkDown:
                    Console.WriteLine(ToMarkDownString());
                    break;
                case ClassFiles.Format.Alternative:
                    Console.WriteLine(ToStringAlternative());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static IEnumerable<string> GetColumns<T>()
        {
            return typeof(T).GetProperties().Select(x => x.Name).ToArray();
        }

        private static object GetColumnValue<T>(object target, string column)
        {
            return typeof(T).GetProperty(column).GetValue(target, null);
        }

        public void Clear(int otherHeights)
        {
            int height = getHeight();
            string blank = getBlank();
            Console.SetCursorPosition(0, startingPoint - otherHeights);
            for (int i = 0; i < height + otherHeights; i++)
            {
                Console.WriteLine(blank);
            }
            Console.SetCursorPosition(0, startingPoint - otherHeights);
        }

        public int getHeight()
        {
            int height = ((Rows.Count * 2) + 3) + 1;
            return height;
        }

        private string getBlank()
        {
            int width = getWidth();
            string blank = "";
            for (int i = 0; i < width; i++)
            {
                blank += " ";
            }
            return blank;
        }

        public int getWidth()
        {
            var columnLengths = ColumnLengths();
            var format = Enumerable.Range(0, Columns.Count)
                    .Select(i => " | {" + i + ",-" + columnLengths[i] + "}")
                    .Aggregate((s, a) => s + a) + " |";

            int width = Math.Max(0, Rows.Any() ? Rows.Max(row => string.Format(format, row).Length) : 0);

            return width;
        }
    }
    public class ConsoleTableOptions
    {
        public IEnumerable<string> Columns { get; set; } = new List<string>();
        public bool EnableCount { get; set; } = true;
    }

    public enum Format
    {
        Default = 0,
        MarkDown = 1,
        Alternative = 2
    }
}