using AnalyzeChat.Application.DTOs;
using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Domain.Entities;
using AnalyzeChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzeChat.Application.Services;

/// <summary>
/// Orchestrates the RAG chat pipeline: 
/// keyword search context → build prompt → stream from ChatGPT.
/// </summary>
public class ChatService
{
    private readonly IVectorStore _vectorStore;
    private readonly IChatAIService _chatAIService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ChatService> _logger;

    private const int MaxContextChunks = 5;
    private const int MaxHistoryMessages = 5;

    public ChatService(
        IVectorStore vectorStore,
        IChatAIService chatAIService,
        IApplicationDbContext dbContext,
        ILogger<ChatService> logger)
    {
        _vectorStore = vectorStore;
        _chatAIService = chatAIService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        ChatRequest request,
        Guid userId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] 
        CancellationToken cancellationToken = default)
    {
        var documentId = Guid.Parse(request.DocumentId);

        // 0. Get the authenticated user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        // 1. Get or Create Conversation
        Conversation? conversation = null;
        if (request.ConversationId.HasValue)
        {
            conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken);
        }

        if (conversation == null)
        {
            var title = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message;
            conversation = new Conversation
            {
                Title = title,
                UserId = user.Id,
                DocumentId = documentId
            };
            _dbContext.Conversations.Add(conversation);
        }

        // 2. Save User Message
        var userMessage = new Message
        {
            Role = MessageRole.User,
            Content = request.Message,
            Conversation = conversation
        };
        _dbContext.Messages.Add(userMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Keyword-based search
        _logger.LogInformation("Searching chunks for query: {Question}", request.Message);
        var relevantChunks = await _vectorStore.SearchByKeywordAsync(
            request.Message, documentId, MaxContextChunks);

        var contextText = string.Join("\n\n---\n\n", relevantChunks.Select(c =>
            $"[Sayfa {c.PageNumber}]: {c.Content}"));

        var historyText = string.Join("\n", request.History
            .TakeLast(MaxHistoryMessages * 2)
            .Select(h => $"{(h.Role == "user" ? "Kullanıcı" : "Asistan")}: {h.Content}"));

        var prompt = $"""
            Sen belge analizi konusunda uzmanlaşmış akıllı ve düzenli bir asistansın. Kullanıcının sorusunu sunulan içeriğe dayanarak yanıtla.
            Eğer içerikte cevabı bulamazsan, bunu açıkça belirt. Mümkün olduğunda sayfa numaralarına referans ver.
            
            Yanıtlarını her zaman Markdown kullanarak biçimlendir, böylece açık ve okunması kolay olsun:
            - Konuları ve ana fikirleri bölümlere ayırmak için başlıklar (### veya ##) kullan.
            - Adımlar için numaralı listeler (1., 2.), birden fazla madde için madde işaretli listeler (- veya *) kullan. Bağımsız fikirleri tek bir paragrafta birleştirme.
            - Her paragraf, başlık ve uzun liste öğeleri arasında boş satır bırak.
            - Anahtar kelimeleri ve önemli terimleri vurgulamak için **kalın yazı** kullan.
            - Uzun ve yoğun metin bloklarından kaçın. Bilgileri küçük ve mantıklı parçalar halinde sun.
            
            === İlgili Belge İçeriği ===
            {contextText}
            
            === Sohbet Geçmişi ===
            {historyText}
            
            === Kullanıcının Sorusu ===
            {request.Message}
            
            Türkçe olarak açık ve ayrıntılı yanıt verin:
            """;

        // 4. Stream response and accumulate
        _logger.LogInformation("Streaming response from ChatGPT...");
        
        // Let the frontend know the conversation ID so it can maintain the session
        yield return $"[CONVERSATION_ID:{conversation.Id}]";
        
        var aiContentBuilder = new System.Text.StringBuilder();

        await foreach (var chunk in _chatAIService.StreamResponseAsync(prompt, request.SelectedModel, cancellationToken))
        {
            aiContentBuilder.Append(chunk);
            yield return chunk;
        }

        // 5. Save AI Message
        var aiMessage = new Message
        {
            Role = MessageRole.Assistant,
            Content = aiContentBuilder.ToString(),
            ConversationId = conversation.Id
        };
        _dbContext.Messages.Add(aiMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
