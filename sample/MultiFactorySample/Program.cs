namespace DataFlow.Runner;

class Program
{
    static void Main()
    {
        var provider = CompositionRoot.Build();
        Console.WriteLine("MultiFactorySample ready — expect two factories from workspace commands.");
    }
}
