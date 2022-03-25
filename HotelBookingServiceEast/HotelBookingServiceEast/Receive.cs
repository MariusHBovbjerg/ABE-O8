using System;
using System.ComponentModel.Design;
using System.Text;
using Consumer.Models;
using Consumer.Models.BookingCommands;
using Consumer.Models.CancelCommands;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer
{
    public static class Receive
    {
        private const string ExchangeName = "CommandExchangeTopic";
        private const string RoutingKey = "*.East";
        
        public static void Main()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(ExchangeName, "topic");
            var QueueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(QueueName, ExchangeName, RoutingKey);

            channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(channel);

            channel.BasicConsume(QueueName, false, consumer);

            consumer.Received += (_, ea) =>
            {
                string response = null;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);

                    var cmd = JsonConvert.DeserializeObject<Command>(message);
                    
                    Console.WriteLine(Environment.MachineName + " - " + DateTime.Now.Millisecond +" - Received Command Type: {0} with message {1}", cmd.Type, cmd.Name);

                    response = cmd.Type switch
                    {
                        HotelBookingCmd.Type => cmd.Location == Location.EAST ? "HotelEastBooked" : "HotelEastFailed",
                        
                        HotelCancelCmd.Type => "HotelEastCancelled",
                        _ => "False"
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(" [.] " + e.Message);
                    response = "";
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish("", props.ReplyTo, replyProps, responseBytes);
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            for (;;) ;
        }
    }
}