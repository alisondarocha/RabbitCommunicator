using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitCommunicator.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string QueueName = "message_queue";

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private async Task InitializeRabbitMqAsync()
    {
        const int maxRetries = 5;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    AutomaticRecoveryEnabled = true
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("RabbitMQ connection initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Tentativa {retry} de {maxRetries} falhou ao conectar com RabbitMQ", retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Erro ao inicializar conexão RabbitMQ após {maxRetries} tentativas", maxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(5 * retryCount));
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            await InitializeRabbitMqAsync();

            if (_channel == null)
            {
                _logger.LogError("Channel not initialized");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Mensagem recebida: {message}", message);
                    Console.WriteLine($"Mensagem recebida: {message}");

                    await Task.Delay(100, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem");
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: true,
                consumer: consumer);

            _logger.LogInformation("Worker is listening for messages...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal no Worker");
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ resources disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer dispose dos recursos RabbitMQ");
        }
        finally
        {
            base.Dispose();
        }
    }
}