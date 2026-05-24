namespace DataFlow.Infrastructure;

using DataFlow.Core;

public class CsvImportSource : IImportSource
{
    public IReadOnlyList<string> ReadRows() => new[] { "row-1", "row-2" };
}

public class StrictImportValidator : IImportValidator
{
    public bool IsValid(string row) => !string.IsNullOrWhiteSpace(row);
}

public class SqlImportStorage : IImportStorage
{
    public void Save(string row) { }
}

public class DatabaseExportSource : IExportSource
{
    public IReadOnlyList<string> LoadRecords() => new[] { "a", "b" };
}

public class JsonExportFormatter : IExportFormatter
{
    public string Format(string record) => $"{{\"value\":\"{record}\"}}";
}

public class SftpExportDelivery : IExportDelivery
{
    public void Deliver(string payload) { }
}

public class SystemJobClock : IJobClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
