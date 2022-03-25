using System;
using System.Threading.Tasks;

namespace Producer
{
    public static class Producer
    {
        public static async Task Main(string[] args)
        {
            var saga = SagaHandler.CreateSaga("hotel", "west");
            
            await SagaHandler.ExecuteSaga(saga);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
