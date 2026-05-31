import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { getConversations, getConversationMessages } from '../api';
import { useChatStore, useAuthStore } from '../store';
import type { ConversationDto } from '../types';

interface ChatHistorySidebarProps {
    isOpen: boolean;
    onClose: () => void;
    onToggle: () => void;
    onSelectConversation: (id: string, documentId: string) => void;
    onNewChat: () => void;
}

export default function ChatHistorySidebar({ isOpen, onClose, onToggle, onSelectConversation, onNewChat }: ChatHistorySidebarProps) {
    const [conversations, setConversations] = useState<ConversationDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');

    const { setMessages, setActiveSession, activeSessionId } = useChatStore();
    const { isAuthenticated, logout } = useAuthStore();

    useEffect(() => {
        if (isOpen && isAuthenticated) {
            loadConversations();
        }
    }, [isOpen]);

    const loadConversations = async () => {
        setIsLoading(true);
        setError('');
        try {
            const data = await getConversations();
            setConversations(data);
        } catch (err: any) {
            if (err?.message?.includes('401') || err?.message?.includes('Unauthorized')) {
                logout();
            }
            setError('Önceki sohbetler yüklenemedi');
        } finally {
            setIsLoading(false);
        }
    };

    const handleSelect = async (conversation: ConversationDto) => {
        try {
            const history = await getConversationMessages(conversation.id);

            const formattedMessages = history.map(msg => ({
                id: msg.id,
                role: msg.role as 'user' | 'assistant',
                content: msg.content,
                timestamp: new Date(msg.createdAt),
            }));

            // Update Zustand state
            setMessages(conversation.id, formattedMessages);
            setActiveSession(conversation.id);

            // Tell parent about the selection
            onSelectConversation(conversation.id, conversation.documentId);
        } catch (err) {
            console.error(err);
            alert('Mesajlar yüklenirken bir hata oluştu');
        }
    };

    return (
        <>
            {/* Mobile overlay */}
            <AnimatePresence>
                {isOpen && (
                    <motion.div
                        className="sidebar-overlay"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        onClick={onClose}
                    />
                )}
            </AnimatePresence>

            {/* Sidebar */}
            <div className={`sidebar ${isOpen ? '' : 'collapsed'}`}>
                <div className="sidebar-header">
                    <div className="sidebar-brand">
                        <div className="sidebar-brand-icon">✦</div>
                        <h2>SATCHAT</h2>
                    </div>
                    <button className="sidebar-toggle" onClick={onToggle} title="Menüyü kapat">
                        ✕
                    </button>
                </div>

                <button className="sidebar-new-chat" onClick={onNewChat}>
                    <span className="sidebar-new-chat-icon">＋</span>
                    Yeni sohbet
                </button>

                <div className="sidebar-content">
                    <div className="sidebar-section-title">Önceki sohbetler</div>

                    {isLoading ? (
                        <div className="sidebar-loading">Yükleniyor...</div>
                    ) : error ? (
                        <div className="sidebar-error">{error}</div>
                    ) : conversations.length === 0 ? (
                        <div className="sidebar-empty">Önceki sohbet bulunamadı</div>
                    ) : (
                        <div className="conversation-list">
                            {conversations.map((conv) => (
                                <div
                                    key={conv.id}
                                    className={`conversation-item ${activeSessionId === conv.id ? 'active' : ''}`}
                                    onClick={() => handleSelect(conv)}
                                >
                                    <div className="conv-icon">💬</div>
                                    <div className="conv-details">
                                        <div className="conv-title">{conv.title}</div>
                                        <div className="conv-date">
                                            {new Date(conv.createdAt).toLocaleDateString('tr-TR')}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}
