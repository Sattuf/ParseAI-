using AnalyzeChat.Application.DTOs;
using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzeChat.Application.Services;

public class ConversationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(IApplicationDbContext dbContext, ILogger<ConversationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ConversationDto>> GetUserConversationsAsync(Guid userId)
    {
        var conversations = await _dbContext.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                DocumentId = c.DocumentId
            })
            .ToListAsync();

        return conversations;
    }

    public async Task<List<MessageDto>> GetConversationMessagesAsync(Guid conversationId, Guid userId)
    {
        // Ensure the conversation belongs to the user
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null)
            return new List<MessageDto>();

        var messages = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString().ToLower(),
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return messages;
    }
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid DocumentId { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
