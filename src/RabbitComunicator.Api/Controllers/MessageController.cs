using Microsoft.AspNetCore.Mvc;
using RabbitCommunicator.Api.Services;

namespace RabbitCommunicator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly RabbitMqService _rabbitMqService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(RabbitMqService rabbitMqService, ILogger<MessageController> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PublishMessage([FromBody] MessageDto messageDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageDto?.Message))
            {
                return BadRequest(new { error = "Mensagem não pode estar vazia" });
            }

            _logger.LogInformation("Recebida requisição para publicar mensagem: {message}", messageDto.Message);
            await _rabbitMqService.PublishMessage(messageDto.Message);
            return Ok(new { message = "Mensagem publicada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }
}

public class MessageDto
{
    public string Message { get; set; } = string.Empty;
}