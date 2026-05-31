namespace DataFlow.Core;

public interface IImportSource
{
    IReadOnlyList<string> ReadRows();
}

public interface IImportValidator
{
    bool IsValid(string row);
}

public interface IImportStorage
{
    void Save(string row);
}

public interface IExportSource
{
    IReadOnlyList<string> LoadRecords();
}

public interface IExportFormatter
{
    string Format(string record);
}

public interface IExportDelivery
{
    void Deliver(string payload);
}

public interface IJobClock
{
    DateTimeOffset UtcNow { get; }
}
