import { useRef, useEffect } from 'react';
import { AnimatePresence } from 'framer-motion';
import type { ChatMessage } from '../types';
import StreamingMessage from './StreamingMessage';
import ThinkingIndicator from './ThinkingIndicator';
import IntelligentInput from './IntelligentInput';
import { useChatStore } from '../store';

interface ChatPageProps {
    messages: ChatMessage[];
    isThinking: boolean;
    isStreaming: boolean;
    onSend: (message: string) => void;
    onFileSelected: (file: File) => void;
    attachedFile: File | null;
    onRemoveFile: () => void;
}

const AVAILABLE_MODELS = [
    { id: 'gemini-2.5-flash', label: 'Gemini 2.5 Flash' },
    { id: 'gemini-2.5-pro', label: 'Gemini 2.5 Pro' },
    { id: 'gemini-2.0-flash', label: 'Gemini 2.0 Flash' },
    { id: 'gemini-2.0-flash-lite', label: 'Gemini 2.0 Flash Lite' },
];

const WELCOME_CHIPS = [
    { icon: '📄', text: 'PDF dosyasını analiz et' },
    { icon: '💡', text: 'Bir belgeyi özetle' },
    { icon: '🔍', text: 'Dosyalarında ara' },
    { icon: '✍️', text: 'Bilgileri çıkar' },
];

export default function ChatPage({
    messages,
    isThinking,
    isStreaming,
    onSend,
    onFileSelected,
    attachedFile,
    onRemoveFile,
}: ChatPageProps) {
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const { selectedModel, setSelectedModel } = useChatStore();

    // Auto-scroll to bottom on new messages
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, isThinking]);

    const showWelcome = messages.length === 0 && !isThinking;

    return (
        <div className="chat-area">
            {/* Model selector in the header area */}
            <div style={{ display: 'flex', justifyContent: 'center', padding: '8px 0 0' }}>
                <select
                    className="model-selector"
                    value={selectedModel}
                    onChange={(e) => setSelectedModel(e.target.value)}
                >
                    {AVAILABLE_MODELS.map((m) => (
                        <option key={m.id} value={m.id}>
                            {m.label}
                        </option>
                    ))}
                </select>
            </div>

            {/* Messages or Welcome */}
            {showWelcome ? (
                <div className="welcome-screen">
                    <div className="welcome-icon">✦</div>
                    <h1 className="welcome-title">
                        <span className="welcome-title-accent">SATCHAT</span>'e Hoş Geldiniz
                    </h1>
                    <p className="welcome-subtitle">
                        Bir PDF dosyası yükleyin ve herhangi bir soru sorun — analiz, özetleme ve bilgi çıkarma konusunda size yardımcı olacağım.
                    </p>
                    <div className="welcome-chips">
                        {WELCOME_CHIPS.map((chip, i) => (
                            <button key={i} className="welcome-chip" onClick={() => onSend(chip.text)}>
                                <span className="welcome-chip-icon">{chip.icon}</span>
                                {chip.text}
                            </button>
                        ))}
                    </div>
                </div>
            ) : (
                <div className="chat-messages">
                    <AnimatePresence mode="popLayout">
                        {messages.map((msg) => (
                            <StreamingMessage key={msg.id} message={msg} />
                        ))}
                    </AnimatePresence>

                    <AnimatePresence>
                        {isThinking && <ThinkingIndicator />}
                    </AnimatePresence>
                    <div ref={messagesEndRef} />
                </div>
            )}

            {/* Input */}
            <IntelligentInput
                onSend={onSend}
                onFileSelected={onFileSelected}
                isStreaming={isStreaming}
                disabled={isThinking}
                attachedFile={attachedFile}
                onRemoveFile={onRemoveFile}
            />
        </div>
    );
}
