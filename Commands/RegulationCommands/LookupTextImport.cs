using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.Lex;
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

        // file
        TextTableResult table;
        await using (var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (var bufferedStream = new BufferedStream(fileStream))
        {
            table = TextTable.Read(bufferedStream, Context.Mapping);
        }
        // lines rejected by a filter are expected (e.g. header records), lines without key or value are not
        foreach (var line in table.IgnoredLines)
        {
            Context.Logger.Warning($"Ignoring source line: {line}");
        }
        Context.Logger.Debug($"Converted {table.Rows.Count} lines, filtered {table.Filtered} lines, ignored {table.Ignored} lines.");

        // lookup
        var lookup = new LookupSet
        {
            Name = lookupName,
            Values = table.Rows.Select(ToLookupValue).ToList()
        };

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

    /// <summary>
    /// Lookup value of a table row
    /// </summary>
    private static LookupValue ToLookupValue(TextTableRow row)
    {
        var value = new LookupValue
        {
            RangeValue = row.RangeValue,
            Key = row.Key,
            Value = row.Value
        };
        // the key values replace the key, the value object replaces the value
        if (row.KeyValues != null)
        {
            value.KeyValues = row.KeyValues.ToArray();
        }
        if (row.ValueObject != null)
        {
            value.ValueObject = row.ValueObject;
        }
        return value;
    }

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
            await FileWriter.WriteAsync(exchange, targetFile);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(regulationName);

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
}
