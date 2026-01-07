using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Scripting.Script;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Import text file to lookup
/// </summary>
internal sealed class LookupTextImport
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();
    private LookupTextImportContext Context { get; }
    private LookupTextImportParameters Parameters { get; }

    private enum LookupTextFormat
    {
        FixedColumns,
        TabDelimited
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="context">Import context</param>
    /// <param name="parameters">Import parameters</param>
    internal LookupTextImport(LookupTextImportContext context, LookupTextImportParameters parameters)
    {
        Context = context;
        Parameters = parameters;
    }

    /// <summary>
    /// Import regulation lookups from text files
    /// </summary>
    /// <returns>Count of imported files</returns>
    internal async Task<int> ImportAsync()
    {
        // single file
        var fileName = Parameters.SourceFileName;
        if (File.Exists(fileName))
        {
            return await ImportFileAsync(new(fileName));
        }

        // file mask
        var files = new DirectoryInfo(Directory.GetCurrentDirectory())
            .GetFiles(Parameters.SourceFileName)
            .ToList();
        var count = 0;
        foreach (var file in files)
        {
            count += await ImportFileAsync(file);
        }
        return count;
    }

    /// <summary>
    /// Import regulation lookup from text file
    /// </summary>
    /// <param name="file">File to convert</param>
    /// <returns>Count of imported files</returns>
    private async Task<int> ImportFileAsync(FileInfo file)
    {
        // new regulation
        var lookupName = file.Name.Replace(file.Extension, string.Empty);

        Context.Logger.Debug($"Parsing text file {file.FullName}.");

        // lookup
        var lookup = new LookupSet
        {
            Name = lookupName,
            Values = []
        };

        // file
        await using (FileStream fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (BufferedStream bufferedStream = new BufferedStream(fileStream))
        using (StreamReader streamReader = new StreamReader(bufferedStream))
        {
            // lookup values
            var count = 0;
            var ignored = 0;
            var format = LookupTextFormat.FixedColumns;
            while (await streamReader.ReadLineAsync() is { } line)
            {
                if (count == 0)
                {
                    format = GetLineFormat(line);
                }
                var result = ConvertLine(line, Context.Mapping, format);
                if (result != null)
                {
                    count++;
                    lookup.Values.Add(result);
                }
                else
                {
                    ignored++;
                    Context.Logger.Warning($"Ignoring source line: {line}");
                }
            }
            Context.Logger.Debug(ignored > 0 ?
                $"Converted {count} lines, ignored {ignored} lines." :
                $"Converted {count} lines.");
        }

        // empty result
        if (!lookup.Values.Any())
        {
            return 0;
        }

        // single lookup file
        if (Parameters.SliceSize <= 0 || lookup.Values.Count < Parameters.SliceSize)
        {
            // ensure reset on single lookup file
            await ImportLookupFileAsync(lookup, lookupName, null);
            return 1;
        }

        // sliced lookup files
        var slice = 0;
        var offset = 0;
        var sliceLookup = new LookupSet
        {
            Name = lookup.Name
        };
        var sliceSize = Math.Min(Parameters.SliceSize, lookup.Values.Count);
        while (sliceSize > 0)
        {
            // update mode
            sliceLookup.UpdateMode = slice == 0 ? UpdateMode.Update : UpdateMode.NoUpdate;

            // slice values
            sliceLookup.Values = lookup.Values.GetRange(offset, sliceSize);

            // slice file
            await ImportLookupFileAsync(sliceLookup, lookupName, slice + 1);

            // next slice
            offset += Parameters.SliceSize;
            if (offset > lookup.Values.Count)
            {
                break;
            }
            slice++;
            sliceSize = Math.Min(Parameters.SliceSize, lookup.Values.Count - offset);
        }
        return slice;
    }

    private static LookupTextFormat GetLineFormat(string line) =>
        line.Contains('\t') ? LookupTextFormat.TabDelimited : LookupTextFormat.FixedColumns;

    /// <summary>
    /// Import lookup file
    /// </summary>
    /// <param name="lookup">The lookup</param>
    /// <param name="lookupFileName">The file name</param>
    /// <param name="counter">The file counter</param>
    private async Task ImportLookupFileAsync(LookupSet lookup, string lookupFileName, int? counter)
    {
        // exchange
        var exchange = CreateRegulation(Parameters.Tenant, Parameters.Regulation);
        var regulation = exchange.Tenants.First().Regulations.First();
        regulation.Lookups = [lookup];

        var importFile = Parameters.ImportTarget is ImportMode.File or ImportMode.All;
        var importBackend = Parameters.ImportTarget is ImportMode.Backend or ImportMode.All;
        if (!importFile && !importBackend)
        {
            return;
        }

        // info
        string target;
        if (importFile && importBackend)
        {
            target = "backend and file";
        }
        else if (importFile)
        {
            target = "file";
        }
        else // import backend
        {
            target = "backend";
        }
        var message = $"Importing lookup '{lookup.Name}' to {target}...";
        Context.Logger.Debug(message);
        Context.Console.DisplayInfo(message);

        // write lookup json import file
        if (importFile)
        {
            // write file
            var fileName = lookupFileName;
            if (counter != null)
            {
                var count = counter > 100 ? $"_{counter.Value:000}" : $"_{counter.Value:00}";
                fileName += count;
            }
            fileName += ".json";
            var targetFile = Path.Combine(Context.TargetFolder, fileName);
            await ExchangeWriter.WriteAsync(exchange, targetFile);
        }

        // write lookup to backend
        if (importBackend)
        {
            // import lookup
            var import = new ExchangeImport(Context.HttpClient, exchange, ScriptParser, importMode: Parameters.ImportMode);
            await import.ImportAsync();
        }

        // info
        Context.Console.DisplayNewLine();
        message = $"Lookup '{lookup.Name}' with {lookup.Values.Count} values imported to {target}.";
        Context.Logger.Debug(message);
        Context.Console.DisplaySuccessLine(message);
    }

    private static Exchange CreateRegulation(string tenantIdentifier, string regulationName)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new ArgumentException(nameof(tenantIdentifier));
        }
        if (string.IsNullOrWhiteSpace(regulationName))
        {
            throw new ArgumentException(nameof(regulationName));
        }

        // exchange
        var exchange = new Exchange
        {
            Tenants = []
        };

        // tenant
        var exchangeTenant = new ExchangeTenant
        {
            Identifier = tenantIdentifier,
            UpdateMode = UpdateMode.NoUpdate
        };
        exchange.Tenants.Add(exchangeTenant);

        // regulation
        var regulation = new RegulationSet
        {
            Name = regulationName,
            UpdateMode = UpdateMode.NoUpdate
        };
        exchangeTenant.Regulations = [regulation];

        return exchange;
    }

    #region Convertion

    private static LookupValue ConvertLine(string line, LookupTextMap mapping, LookupTextFormat format)
    {
        // key
        var key = ConvertValue(line, mapping.Key, format);

        // key values
        var keyValues = new List<object>();
        if (mapping.Keys != null)
        {
            foreach (var keyMap in mapping.Keys)
            {
                var keyValue = ConvertValue(line, keyMap, format);
                if (keyValue != null)
                {
                    keyValues.Add(keyValue);
                }
            }
        }
        if (key == null && !keyValues.Any())
        {
            return null;
        }

        // range value
        var rangeValue = ConvertDecimalValue(line, mapping.RangeValue, format);

        // value
        var value = ConvertValue(line, mapping.Value, format);

        // values
        var valueObject = new Dictionary<string, object>();
        foreach (var valueMap in mapping.Values)
        {
            if (string.IsNullOrWhiteSpace(valueMap.Name))
            {
                throw new PayrollException($"Text value map without name {valueMap}.");
            }

            var objectValue = ConvertValue(line, valueMap, format);
            if (objectValue != null)
            {
                valueObject[valueMap.Name] = objectValue;
            }
        }
        if (value == null && !valueObject.Any())
        {
            return null;
        }

        var lookup = new LookupValue
        {
            RangeValue = rangeValue,
            Key = key?.ToString(),
            KeyValues = keyValues.Any() ? keyValues.ToArray() : null,
            Value = value?.ToString(),
            ValueObject = valueObject.Any() ? valueObject : null
        };
        return lookup;
    }

    private static decimal? ConvertDecimalValue(string line, LookupTextValueMap mapping, LookupTextFormat format)
    {
        var value = ConvertValue(line, mapping, format);
        if (value != null)
        {
            return Convert.ToDecimal(value);
        }
        return null;
    }

    private static object ConvertValue(string line, LookupTextValueMap mapping, LookupTextFormat format)
    {
        if (mapping == null)
        {
            return null;
        }
        var textValue = ExtractValue(line, mapping, format);
        if (string.IsNullOrWhiteSpace(textValue))
        {
            return null;
        }
        switch (mapping.ValueType)
        {
            case LookupValueType.Text:
                return textValue;
            case LookupValueType.Decimal:
                return ConvertDecimal(textValue, mapping.DecimalPlaces);
            case LookupValueType.Integer:
                return ConvertInteger(textValue);
            case LookupValueType.Boolean:
                return ConvertBoolean(textValue);
        }
        return null;
    }

    private static bool? ConvertBoolean(string value)
    {
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    private static int? ConvertInteger(string value)
    {
        if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return null;
    }

    private static decimal? ConvertDecimal(string value, int decimalPlaces)
    {
        if (decimal.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            if (decimalPlaces == 0)
            {
                return result;
            }
            return result / (decimal)Math.Pow(10, decimalPlaces);
        }
        return null;
    }

    private static string ExtractValue(string line, LookupTextValueMap mapping, LookupTextFormat format)
    {
        if (string.IsNullOrWhiteSpace(line) || mapping.Start >= line.Length)
        {
            return null;
        }

        switch (format)
        {
            case LookupTextFormat.FixedColumns:
                if (mapping.Length == 0)
                {
                    throw new PayrollException($"Missing length in fixed column mapping {mapping.Name}.");
                }
                var length = Math.Min(mapping.Length, line.Length - mapping.Start);
                return line.Substring(mapping.Start, length).Trim();
            case LookupTextFormat.TabDelimited:
                var tokens = line.Split('\t');
                return mapping.Start < tokens.Length ? tokens[mapping.Start].Trim() : null;
        }
        return null;
    }

    #endregion
}