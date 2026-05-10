namespace DemoApp.Services
{
    public class HelloMessageService : IMessageService
    {
        public string GetMessage() => "Hello from HelloMessageService";
    }
}