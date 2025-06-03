using System.Text;
using RabbitMQ.Client;

namespace RabbitCommunicator.Api.Services;

public class RabbitMqService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ConnectionFactory _factory;
    private const string QueueName = "message_queue";

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost", // Mudança aqui
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            AutomaticRecoveryEnabled = true
        };
    }

    public async Task PublishMessage(string message)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QueueName,
                    body: body,
                    mandatory: false);

                _logger.LogInformation("Mensagem publicada: {message}", message);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Tentativa {retry} de {maxRetries} falhou ao publicar mensagem", retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Erro ao publicar mensagem no RabbitMQ após {maxRetries} tentativas", maxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(2 * retryCount)); // Backoff exponencial
            }
        }
    }
}