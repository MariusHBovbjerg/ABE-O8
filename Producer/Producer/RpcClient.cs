using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Producer.Models;

namespace Producer
{
    public class RpcClient
    {
        private const string ExchangeName = "CommandExchange";
        private const string ReplyQueueName = "SagaReturnQueue";

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

        public RpcClient()
        {
            var factory = new ConnectionFactory {HostName = "localhost"};

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            // declare a server-named queue
            _channel.ExchangeDeclare(ExchangeName, "direct");
            _replyQueueName = _channel.QueueDeclare(queue: ReplyQueueName);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(
                consumer: consumer,
                queue: _replyQueueName,
                autoAck: false);
        }
        
        public Task<string> CallAsync(Command message, string routingKey, CancellationToken cancellationToken = default)
        {
            var props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;
            var obj = JsonConvert.SerializeObject(message);
            var messageBytes = Encoding.UTF8.GetBytes(obj);
            var tcs = new TaskCompletionSource<string>();
            _callbackMapper.TryAdd(correlationId, tcs);

            _channel.BasicPublish(ExchangeName, routingKey, props, messageBytes);

            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public void Close()
        {
            _connection.Close();
        }
    }
}
