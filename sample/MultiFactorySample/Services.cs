namespace DataFlow.Services;

using DataFlow.Core;

/// <summary>ETL import — 3 interface deps → factory candidate.</summary>
public class ImportPipeline
{
    private readonly IImportSource _source;
    private readonly IImportValidator _validator;
    private readonly IImportStorage _storage;

    public ImportPipeline(
        IImportSource source,
        IImportValidator validator,
        IImportStorage storage)
    {
        _source = source;
        _validator = validator;
        _storage = storage;
    }

    public int Run()
    {
        var count = 0;
        foreach (var row in _source.ReadRows())
        {
            if (!_validator.IsValid(row)) continue;
            _storage.Save(row);
            count++;
        }
        return count;
    }
}

/// <summary>ETL export — 3 interface deps → second factory candidate.</summary>
public class ExportPipeline
{
    private readonly IExportSource _source;
    private readonly IExportFormatter _formatter;
    private readonly IExportDelivery _delivery;

    public ExportPipeline(
        IExportSource source,
        IExportFormatter formatter,
        IExportDelivery delivery)
    {
        _source = source;
        _formatter = formatter;
        _delivery = delivery;
    }

    public int Run()
    {
        var count = 0;
        foreach (var record in _source.LoadRecords())
        {
            _delivery.Deliver(_formatter.Format(record));
            count++;
        }
        return count;
    }
}

/// <summary>Thin scheduler — 2 deps → no factory.</summary>
public class JobScheduler
{
    private readonly IJobClock _clock;
    private readonly IImportStorage _storage;

    public JobScheduler(IJobClock clock, IImportStorage storage)
    {
        _clock = clock;
        _storage = storage;
    }

    public void LogNextRun() => Console.WriteLine($"Next run after {_clock.UtcNow}");
}
