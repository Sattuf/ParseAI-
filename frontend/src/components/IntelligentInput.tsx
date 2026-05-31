import { useState, useCallback, useRef, useEffect, type KeyboardEvent, type ChangeEvent } from 'react';

interface IntelligentInputProps {
    onSend: (message: string) => void;
    onFileSelected: (file: File) => void;
    disabled?: boolean;
    isStreaming?: boolean;
    attachedFile?: File | null;
    onRemoveFile?: () => void;
}

export default function IntelligentInput({
    onSend,
    onFileSelected,
    disabled,
    isStreaming,
    attachedFile,
    onRemoveFile,
}: IntelligentInputProps) {
    const [value, setValue] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 150)}px`;
        }
    }, [value]);

    const handleSend = () => {
        const trimmed = value.trim();
        if (!trimmed || disabled || isStreaming) return;
        onSend(trimmed);
        setValue('');
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const handleChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
        setValue(e.target.value);
    };

    const handleFileChange = useCallback((e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            onFileSelected(file);
        }
        // Reset input so re-selecting the same file triggers change
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    }, [onFileSelected]);

    return (
        <div className="input-area">
            <div className="input-container">
                {/* Attached file chip */}
                {attachedFile && (
                    <div className="input-attachment">
                        <div className="attachment-chip">
                            <span className="attachment-chip-icon">📄</span>
                            <span>{attachedFile.name}</span>
                            <button
                                className="attachment-remove"
                                onClick={onRemoveFile}
                                title="Dosyayı kaldır"
                            >
                                ✕
                            </button>
                        </div>
                    </div>
                )}

                <div className="input-row">
                    {/* Paperclip / file upload button */}
                    <div className="input-actions">
                        <button
                            className="input-action-btn"
                            title="PDF dosyası ekle"
                            onClick={() => fileInputRef.current?.click()}
                        >
                            📎
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept=".pdf"
                                onChange={handleFileChange}
                                style={{ display: 'none' }}
                            />
                        </button>
                    </div>

                    {/* Text input */}
                    <textarea
                        ref={textareaRef}
                        className="input-field"
                        value={value}
                        onChange={handleChange}
                        onKeyDown={handleKeyDown}
                        placeholder={isStreaming ? '...Yanıtlanıyor' : 'SATCHAT\'e sorun...'}
                        disabled={disabled || isStreaming}
                        rows={1}
                        dir="auto"
                    />

                    {/* Send button */}
                    <button
                        className={`send-button ${value.trim().length > 0 ? 'send-button-active' : ''}`}
                        onClick={handleSend}
                        disabled={!value.trim() || disabled || isStreaming}
                        title="Gönder"
                    >
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                            <line x1="22" y1="2" x2="11" y2="13"></line>
                            <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
                        </svg>
                    </button>
                </div>
            </div>
            <div className="input-hint">
                SATCHAT, PDF dosyalarınızı analiz edebilir ve sorularınızı yanıtlayabilir
            </div>
        </div>
    );
}
