using AnalyzeChat.Application.DTOs;
using AnalyzeChat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzeChat.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Stream a chat response using Server-Sent Events.
    /// The response is streamed token by token from the AI model.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Message is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.DocumentId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("DocumentId is required");
            return;
        }

        _logger.LogInformation("Streaming chat for document {DocId}: {Message}",
            request.DocumentId, request.Message);

        // Extract User ID from JWT Claims
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            Response.StatusCode = 401;
            await Response.WriteAsync("Unauthorized: User ID not found");
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var chunk in _chatService.StreamAnswerAsync(
                request, userId, HttpContext.RequestAborted))
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stream cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming");
            await Response.WriteAsync($"data: Hata: {ex.Message}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
