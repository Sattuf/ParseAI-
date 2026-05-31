import type { ChatMessage } from '../types';
import { motion } from 'framer-motion';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface StreamingMessageProps {
    message: ChatMessage;
}

export default function StreamingMessage({ message }: StreamingMessageProps) {
    const isUser = message.role === 'user';

    return (
        <motion.div
            className={`message ${isUser ? 'message-user' : 'message-assistant'}`}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.25, ease: 'easeOut' }}
        >
            <div className={`message-avatar ${isUser ? 'message-avatar-user' : 'message-avatar-assistant'}`}>
                {isUser ? '👤' : '✦'}
            </div>
            <div className="message-content-wrapper">
                <div className={`message-bubble ${isUser ? 'message-bubble-user' : 'message-bubble-assistant'}`} dir="auto">
                    {isUser ? (
                        message.content
                    ) : (
                        <div className="markdown-content">
                            <ReactMarkdown remarkPlugins={[remarkGfm]}>
                                {message.content + (message.isStreaming ? '▍' : '')}
                            </ReactMarkdown>
                        </div>
                    )}
                </div>
                <div className="message-time" dir="ltr">
                    {message.timestamp.toLocaleTimeString('tr-TR', {
                        hour: '2-digit',
                        minute: '2-digit',
                    })}
                </div>
            </div>
        </motion.div>
    );
}
