using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace CsvShuffle.Pages;

public partial class Shuffle : ComponentBase
{
    [Inject] ISnackbar Snackbar { get; set; } = null!;

    string _fileName = string.Empty;
    string _search = string.Empty;
    string _progressLabel = "Preparing export…";
    string? _obfuscatedCsv;
    bool _busy;
    double _progress;
    CancellationTokenSource? _cancellation;
    List<string> _headers = [];
    List<string[]> _rows = [];
    List<CsvRow> _gridRows = [];
    List<CsvRow> _obfuscatedGridRows = [];
    List<ObfuscationMode> _modes = [];
    bool _showObfuscated;

    IEnumerable<CsvRow> VisibleGridRows => _showObfuscated ? _obfuscatedGridRows : _gridRows;

    bool QuickFilter(CsvRow row) =>
        string.IsNullOrWhiteSpace(_search)
        || row.Cells.Any(cell => cell.Contains(_search, StringComparison.OrdinalIgnoreCase));

    async Task LoadFile(InputFileChangeEventArgs args)
    {
        _fileName = args.File.Name;
        try
        {
            await using var stream = args.File.OpenReadStream(500_000_000L);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            List<string[]> parsed = ParseCsv(await reader.ReadToEndAsync());

            if (parsed.Count == 0)
                throw new InvalidDataException("The selected file is empty.");

            _headers = [.. parsed[0]];
            _rows = [.. parsed.Skip(1).Select(row => NormalizeRow(row, _headers.Count))];
            _gridRows = [.. _rows.Select(row => new CsvRow(row))];
            _obfuscatedGridRows.Clear();
            _modes = [.. Enumerable.Repeat(ObfuscationMode.Clear, _headers.Count)];
            _obfuscatedCsv = null;
            _showObfuscated = false;
        }
        catch (Exception exception)
        {
            _headers.Clear();
            _rows.Clear();
            _gridRows.Clear();
            _obfuscatedGridRows.Clear();
            _showObfuscated = false;
            string message = $"Could not read this CSV: {exception.Message}";
            Snackbar.Add(message, Severity.Error, options => options.RequireInteraction = true);
        }
    }

    async Task Obfuscate()
    {
        _busy = true;
        _progress = 0;
        _progressLabel = "Preparing obfuscation…";
        _obfuscatedCsv = null;
        _obfuscatedGridRows.Clear();
        _showObfuscated = false;
        _cancellation = new CancellationTokenSource();

        try
        {
            Dictionary<string, string> consistentValues = [];
            StringBuilder output = new();
            List<CsvRow> obfuscatedRows = [];
            output.AppendLine(string.Join(',', _headers.Select(EncodeCsv)));

            for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
            {
                _cancellation.Token.ThrowIfCancellationRequested();

                string[] values = [.. _rows[rowIndex].Select((value, column) =>
                    Transform(value, _modes[column], consistentValues))];

                obfuscatedRows.Add(new CsvRow(values));
                output.AppendLine(string.Join(',', values.Select(EncodeCsv)));

                if (rowIndex % 500 != 0)
                    continue;

                _progress = 100d * rowIndex / Math.Max(1, _rows.Count);
                _progressLabel = $"Obfuscating row {rowIndex:N0} of {_rows.Count:N0}";
                await InvokeAsync(StateHasChanged);
                await Task.Yield();
            }

            _obfuscatedCsv = output.ToString();
            _obfuscatedGridRows = obfuscatedRows;
            _showObfuscated = true;
            _progress = 100;
            _progressLabel = "Obfuscation complete. Your download is ready.";
            Snackbar.Add("Obfuscation complete. Your download is ready.", Severity.Success);
        }
        catch (OperationCanceledException)
        {
            _progressLabel = "Obfuscation cancelled.";
        }
        finally
        {
            _busy = false;
        }
    }

    async Task Download()
    {
        if (_obfuscatedCsv is not null)
            await Js.InvokeVoidAsync("csvShuffle.download", ObfuscatedFileName(), _obfuscatedCsv);
    }

    void Cancel() => _cancellation?.Cancel();

    string ObfuscatedFileName() => $"{Path.GetFileNameWithoutExtension(_fileName)}_obfuscated.csv";

    static string[] NormalizeRow(string[] row, int columnCount) =>
        [.. row.Concat(Enumerable.Repeat(string.Empty, Math.Max(0, columnCount - row.Length))).Take(columnCount)];

    static string EncodeCsv(string value) =>
        value.Contains(',') ||
        value.Contains('"') ||
        value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    static string Transform(
        string value,
        ObfuscationMode mode,
        Dictionary<string, string> consistentValues
    )
    {
        if (mode == ObfuscationMode.Clear || string.IsNullOrEmpty(value))
            return value;

        string key = $"{mode}|{value}";

        if (consistentValues.TryGetValue(key, out string? prior))
            return prior;

        string transformed = mode == ObfuscationMode.Date
            ? TransformDate(
                value: value
            )
            : TransformCharacters(
                value: value,
                preserveVowelClass: mode is ObfuscationMode.Name or ObfuscationMode.Address
            );

        consistentValues[key] = transformed;
        return transformed;
    }

    static string TransformCharacters(string value, bool preserveVowelClass) =>
        new(value.Select(character =>
            char.IsDigit(character)
                ? (char)('0' + Random.Shared.Next(10))
                : char.IsLetter(character)
                    ? RandomLetter(character, preserveVowelClass)
                    : character).ToArray());

    static char RandomLetter(
        char source,
        bool preserveVowelClass
    )
    {
        const string vowels = "aeiouy";
        const string consonants = "bcdfghjklmnpqrstvwxz";

        string pool = preserveVowelClass && vowels.Contains(char.ToLowerInvariant(source))
            ? vowels
            : preserveVowelClass
                ? consonants
                : "abcdefghijklmnopqrstuvwxyz";

        char result = pool[Random.Shared.Next(pool.Length)];

        return char.IsUpper(source)
            ? char.ToUpperInvariant(result)
            : result;
    }

    static string TransformDate(string value)
    {
        if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            && !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
            return TransformCharacters(
                value: value,
                preserveVowelClass: false
            );

        return date
            .AddYears(Random.Shared.Next(-5, 6))
            .AddMonths(Random.Shared.Next(-2, 3))
            .AddDays(Random.Shared.Next(-10, 11))
            .ToString("M/d/yyyy", CultureInfo.InvariantCulture);
    }

    static List<string[]> ParseCsv(string input)
    {
        List<string[]> rows = [];
        List<string> row = [];
        var cell = new StringBuilder();
        bool quoted = false;

        char delimiter = input.Count(character => character == '\t') > input.Count(character => character == ',')
            ? '\t'
            : ',';

        for (int i = 0; i < input.Length; i++)
        {
            char character = input[i];
            if (character == '"' && (quoted || cell.Length == 0))
            {
                if (quoted && i + 1 < input.Length && input[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (character == delimiter && !quoted)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (character is '\r' or '\n' && !quoted)
            {
                if (character == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                    i++;

                row.Add(cell.ToString());
                cell.Clear();
                if (row.Any(value => value.Length > 0))
                    rows.Add([.. row]);
                row = [];
            }
            else cell.Append(character);
        }

        if (cell.Length <= 0 && row.Count <= 0)
            return rows;

        row.Add(cell.ToString());
        rows.Add([.. row]);

        return rows;
    }

    sealed record CsvRow(string[] Cells);
}
