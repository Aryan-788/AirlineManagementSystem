using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;

namespace Shared.RabbitMQ;

public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent;
}

public interface IEventConsumer
{
    void Initialize(string serviceName);
    Task SubscribeAsync<T>(Func<T, Task> handler) where T : IntegrationEvent;
    Task StartAsync();
}

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private const string ExchangeName = "airline-events";

    public RabbitMqEventPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<T>(T integrationEvent)
        where T : IntegrationEvent
    {
        await using var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var routingKey = typeof(T).Name;
        var message = JsonSerializer.Serialize(integrationEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}

public class RabbitMqEventConsumer : IEventConsumer
{
    private readonly IConnection _connection;
    private readonly Dictionary<Type, Delegate> _handlers = new();
    private string _serviceName = "DefaultService";
    private const string ExchangeName = "airline-events";

    public RabbitMqEventConsumer(IConnection connection)
    {
        _connection = connection;
    }

    public void Initialize(string serviceName)
    {
        _serviceName = serviceName;
    }

    public Task SubscribeAsync<T>(Func<T, Task> handler)
        where T : IntegrationEvent
    {
        _handlers[typeof(T)] = handler;
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        foreach (var eventType in _handlers.Keys)
        {
            var queueName = $"{_serviceName}-{eventType.Name}-queue";
            var routingKey = eventType.Name;

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: ExchangeName,
                routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var integrationEvent =
                        JsonSerializer.Deserialize(message, eventType);

                    if (integrationEvent != null)
                    {
                        var handler = _handlers[eventType];
                        await (Task)handler.DynamicInvoke(integrationEvent)!;
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);
        }
    }
}
public static class RabbitMqExtensions
{
    public static async Task<IConnection> CreateRabbitMqConnectionAsync(
        string hostName,
        string userName,
        string password,
        int port)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            Port = port,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        return await factory.CreateConnectionAsync();
    }
}