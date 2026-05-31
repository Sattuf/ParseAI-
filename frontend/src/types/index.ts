export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
}

export interface Document {
  id: string;
  fileName: string;
  pageCount: number;
  status: 'uploading' | 'processing' | 'ready' | 'failed';
  uploadedAt: Date;
}

export interface ChatSession {
  id: string;
  documentId: string;
  messages: ChatMessage[];
  createdAt: Date;
}



export interface ConversationDto {
  id: string;
  title: string;
  createdAt: string;
  documentId: string;
}

export interface MessageDto {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}
