import { motion } from 'framer-motion';

export default function ThinkingIndicator() {
    return (
        <motion.div
            className="thinking-indicator"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
        >
            <div className="message-avatar message-avatar-assistant">✦</div>
            <div className="thinking-dots">
                <div className="thinking-dot" />
                <div className="thinking-dot" />
                <div className="thinking-dot" />
            </div>
            <span className="thinking-text">Düşünüyor...</span>
        </motion.div>
    );
}
