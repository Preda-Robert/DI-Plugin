using ShopApp;

namespace ShopApp.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            // Keep Program minimal; workspace commands should still infer DI graph
            // and suggest builder/factory output from other files.
            var provider = CompositionRoot.Build();
        }
    }
}

