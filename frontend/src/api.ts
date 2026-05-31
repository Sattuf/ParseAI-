import { useAuthStore } from './store';
import type { ConversationDto, MessageDto } from './types';

export const API_BASE = 'http://localhost:5000/api';

export interface UploadResponse {
    id: string;
    fileName: string;
    fileSizeBytes: number;
    pageCount: number;
    status: 'Uploading' | 'Processing' | 'Ready' | 'Failed';
    uploadedAt: string;
    errorMessage?: string;
}

/**
 * Uploads a PDF file to the backend for processing (extraction, chunking, embedding).
 */
export async function uploadDocument(file: File): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const token = useAuthStore.getState().token;

    const response = await fetch(`${API_BASE}/documents/upload`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        body: formData,
    });

    if (!response.ok) {
        if (response.status === 401) {
            useAuthStore.getState().logout();
            throw new Error('Yetkiniz yok. Lütfen tekrar giriş yapın.');
        }
        const error = await response.text();
        throw new Error(`Upload failed: ${error}`);
    }

    return response.json();
}

/**
 * Gets document info by ID.
 */
export async function getDocument(id: string): Promise<UploadResponse> {
    const token = useAuthStore.getState().token;

    const response = await fetch(`${API_BASE}/documents/${id}`, {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    if (!response.ok) {
        if (response.status === 401) useAuthStore.getState().logout();
        throw new Error('Document not found');
    }
    return response.json();
}

/**
 * Gets user's past conversations.
 */
export async function getConversations(): Promise<ConversationDto[]> {
    const token = useAuthStore.getState().token;

    const response = await fetch(`${API_BASE}/conversations`, {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    if (!response.ok) {
        if (response.status === 401) useAuthStore.getState().logout();
        throw new Error('Failed to fetch conversations');
    }
    return response.json();
}

/**
 * Gets messages for a specific conversation.
 */
export async function getConversationMessages(id: string): Promise<MessageDto[]> {
    const token = useAuthStore.getState().token;

    const response = await fetch(`${API_BASE}/conversations/${id}/messages`, {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    if (!response.ok) {
        if (response.status === 401) useAuthStore.getState().logout();
        throw new Error('Failed to fetch messages');
    }
    return response.json();
}
