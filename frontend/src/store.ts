import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { ChatMessage } from './types';

// --- Auth State ---
interface User {
    id: string;
    email: string;
    name: string;
}

interface AuthState {
    token: string | null;
    user: User | null;
    isAuthenticated: boolean;
    setAuth: (token: string, user: User) => void;
    logout: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            token: null,
            user: null,
            isAuthenticated: false,
            setAuth: (token, user) => set({ token, user, isAuthenticated: true }),
            logout: () => set({ token: null, user: null, isAuthenticated: false }),
        }),
        {
            name: 'auth-storage',
        }
    )
);

// --- Chat State ---
interface ChatState {
    messages: Record<string, ChatMessage[]>; // Keyed by DocumentId or SessionId
    activeSessionId: string | null;
    selectedModel: string;
    addMessage: (sessionId: string, message: ChatMessage) => void;
    setMessages: (sessionId: string, messages: ChatMessage[]) => void;
    setActiveSession: (sessionId: string | null) => void;
    setSelectedModel: (model: string) => void;
    clearSession: (sessionId: string) => void;
}

export const useChatStore = create<ChatState>()((set) => ({
    messages: {},
    activeSessionId: null,
    selectedModel: "gemini-2.5-flash",
    addMessage: (sessionId, message) =>
        set((state) => ({
            messages: {
                ...state.messages,
                [sessionId]: [...(state.messages[sessionId] || []), message],
            },
        })),
    setMessages: (sessionId, messages) =>
        set((state) => ({
            messages: {
                ...state.messages,
                [sessionId]: messages,
            },
        })),
    setActiveSession: (sessionId) => set({ activeSessionId: sessionId }),
    setSelectedModel: (model) => set({ selectedModel: model }),
    clearSession: (sessionId) =>
        set((state) => {
            const newMessages = { ...state.messages };
            delete newMessages[sessionId];
            return { messages: newMessages };
        }),
}));
