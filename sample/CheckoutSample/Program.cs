namespace CheckoutSample.Runner;

class Program
{
    static void Main()
    {
        var provider = CompositionRoot.Build();
        Console.WriteLine("CheckoutSample ready — run DI plugin workspace commands.");
    }
}
