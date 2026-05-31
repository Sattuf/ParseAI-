import { useState, useCallback, useRef, useEffect } from 'react';
import type { ChatMessage } from '../types';
import { useChatStore, useAuthStore } from '../store';

import { API_BASE } from '../api';

interface UseStreamingChatReturn {
    messages: ChatMessage[];
    isStreaming: boolean;
    isThinking: boolean;
    sendMessage: (content: string, documentId: string) => Promise<void>;
    clearMessages: () => void;
}

export function useStreamingChat(): UseStreamingChatReturn {
    const { activeSessionId, setActiveSession, selectedModel, messages: allMessages, addMessage, setMessages: storeSetMessages } = useChatStore();
    
    // Local state for the current session's messages to ensure smooth rendering during streaming
    const [localMessages, setLocalMessages] = useState<ChatMessage[]>([]);
    const [isStreaming, setIsStreaming] = useState(false);
    const [isThinking, setIsThinking] = useState(false);
    const abortControllerRef = useRef<AbortController | null>(null);

    // Sync local state when the active session changes or when the global store updates
    useEffect(() => {
        if (activeSessionId) {
            setLocalMessages(allMessages[activeSessionId] || []);
        } else {
            setLocalMessages([]);
        }
    }, [activeSessionId, allMessages]);

    const sendMessage = useCallback(async (content: string, documentId: string) => {
        if (!activeSessionId) {
            // For a brand new chat, we might not have a session ID yet. 
            // We'll generate a temporary one until the backend gives us a real one.
        }

        // Add user message
        const userMessage: ChatMessage = {
            id: crypto.randomUUID(),
            role: 'user',
            content,
            timestamp: new Date(),
        };

        const currentSessionId = activeSessionId || 'temp-' + Date.now();
        addMessage(currentSessionId, userMessage);
        setIsThinking(true);
        setIsStreaming(false);

        // Create placeholder assistant message
        const assistantId = crypto.randomUUID();
        const assistantMessage: ChatMessage = {
            id: assistantId,
            role: 'assistant',
            content: '',
            timestamp: new Date(),
            isStreaming: true,
        };

        try {
            abortControllerRef.current = new AbortController();

            const token = useAuthStore.getState().token;

            const response = await fetch(`${API_BASE}/chat/stream`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    conversationId: activeSessionId?.startsWith('temp-') ? null : activeSessionId,
                    message: content,
                    documentId,
                    selectedModel,
                    history: localMessages.slice(-10).map(m => ({
                        role: m.role,
                        content: m.content,
                    })),
                }),
                signal: abortControllerRef.current.signal,
            });

            if (!response.ok) {
                throw new Error(`Server error: ${response.status}`);
            }

            const reader = response.body?.getReader();
            if (!reader) throw new Error('No readable stream');

            const decoder = new TextDecoder();
            setIsThinking(false);
            setIsStreaming(true);

            // Add the assistant message placeholder to the store
            addMessage(currentSessionId, assistantMessage);

            let fullContent = '';
            let realSessionId = currentSessionId;

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const chunk = decoder.decode(value, { stream: true });

                // Parse SSE events
                const lines = chunk.split('\n');
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        if (data === '[DONE]') break;

                        if (data.startsWith('[CONVERSATION_ID:')) {
                            const newSessionId = data.slice(17, -1);
                            realSessionId = newSessionId;
                            
                            // If we were using a temp ID, migrate messages to the real ID
                            if (activeSessionId !== newSessionId) {
                                const currentMsgs = useChatStore.getState().messages[currentSessionId] || [];
                                storeSetMessages(newSessionId, currentMsgs);
                                setActiveSession(newSessionId);
                            }
                            continue;
                        }

                        try {
                            const parsed = JSON.parse(data);
                            if (typeof parsed === 'string') {
                                fullContent += parsed;
                            } else {
                                fullContent += parsed.content || parsed.text || data;
                            }
                        } catch {
                            // Raw text fallback
                            let text = data;
                            if (text.startsWith('"') && text.endsWith('"')) {
                                text = text.slice(1, -1);
                            }
                            fullContent += text.replace(/\\n/g, '\n');
                        }

                        // Update the message in the store
                        const state = useChatStore.getState();
                        const sessionMsgs = state.messages[realSessionId] || [];
                        const updatedMsgs = sessionMsgs.map(m =>
                            m.id === assistantId ? { ...m, content: fullContent } : m
                        );
                        storeSetMessages(realSessionId, updatedMsgs);
                    }
                }
            }

            // Mark streaming as complete in the store
            const finalState = useChatStore.getState();
            const finalMsgs = finalState.messages[realSessionId] || [];
            storeSetMessages(realSessionId, finalMsgs.map(m =>
                m.id === assistantId ? { ...m, isStreaming: false } : m
            ));

        } catch (error) {
            if ((error as Error).name === 'AbortError') return;

            const errorContent = 'Üzgünüz, sunucuya bağlanırken bir hata oluştu.';
            const state = useChatStore.getState();
            const sessionMsgs = state.messages[currentSessionId] || [];
            storeSetMessages(currentSessionId, [
                ...sessionMsgs.filter(m => m.id !== assistantId),
                { ...assistantMessage, content: errorContent, isStreaming: false },
            ]);
        } finally {
            setIsStreaming(false);
            setIsThinking(false);
            abortControllerRef.current = null;
        }
    }, [activeSessionId, localMessages, selectedModel, addMessage, storeSetMessages, setActiveSession]);

    const clearMessages = useCallback(() => {
        abortControllerRef.current?.abort();
        if (activeSessionId) {
            storeSetMessages(activeSessionId, []);
        }
        setIsStreaming(false);
        setIsThinking(false);
    }, [activeSessionId, storeSetMessages]);

    return { messages: localMessages, isStreaming, isThinking, sendMessage, clearMessages };
}
