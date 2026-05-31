import { useState, useCallback } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Routes, Route } from 'react-router-dom';
import ChatPage from './components/ChatPage';
import AuthCallback from './components/AuthCallback';
import ChatHistorySidebar from './components/ChatHistorySidebar';
import { uploadDocument, getDocument } from './api';
import { useAuthStore, useChatStore } from './store';
import { useStreamingChat } from './hooks/useStreamingChat';

function App() {
  const [pdfFile, setPdfFile] = useState<File | null>(null);
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [processingStatus, setProcessingStatus] = useState('Belge analiz ediliyor...');

  const { user, isAuthenticated, logout } = useAuthStore();
  const { setActiveSession } = useChatStore();
  const { messages, isStreaming, isThinking, sendMessage, clearMessages } = useStreamingChat();

  const handleFileSelected = useCallback(async (file: File) => {
    if (!isAuthenticated) {
      alert("Belge yükleyebilmek ve sohbet edebilmek için lütfen önce giriş yapın.");
      return;
    }

    setIsProcessing(true);
    setPdfFile(file);
    setProcessingStatus('Dosya yükleniyor...');

    try {
      setProcessingStatus('Metinler analiz ediliyor ve çıkarılıyor...');
      const result = await uploadDocument(file);

      if (result.status === 'Failed') {
        setProcessingStatus(`Analiz başarısız: ${result.errorMessage}`);
        setTimeout(() => setIsProcessing(false), 3000);
        return;
      }

      setDocumentId(result.id);
      setProcessingStatus(`Analiz tamamlandı! ${result.pageCount} sayfa`);

      await new Promise(resolve => setTimeout(resolve, 600));
      setIsProcessing(false);
    } catch (error) {
      console.error('Upload failed:', error);
      setProcessingStatus('Sunucuya bağlanılamadı. Backend\'in çalıştığından emin olun.');
      setTimeout(() => setIsProcessing(false), 3000);
    }
  }, [isAuthenticated]);

  const handleSend = useCallback((message: string) => {
    if (!documentId) {
      // If no document uploaded yet, prompt user
      alert('Lütfen soru sormadan önce bir PDF dosyası yükleyin.');
      return;
    }
    sendMessage(message, documentId);
  }, [documentId, sendMessage]);

  const handleRemoveFile = useCallback(() => {
    setPdfFile(null);
    // Don't clear documentId if already uploaded — let messages continue
  }, []);

  const handleNewChat = useCallback(() => {
    clearMessages();
    setPdfFile(null);
    setDocumentId(null);
    setActiveSession(null);
  }, [clearMessages, setActiveSession]);

  const handleLogin = () => {
    window.location.href = 'http://localhost:5000/api/auth/login';
  };

  const handleSelectConversation = async (sessionId: string, docId: string) => {
    if (docId !== documentId) {
      setIsProcessing(true);
      try {
        setProcessingStatus('Belge bilgileri alınıyor...');
        const docInfo = await getDocument(docId);

        const dummyFile = new File([''], docInfo.fileName, { type: 'application/pdf' });
        setDocumentId(docInfo.id);
        setPdfFile(dummyFile);
      } catch (err) {
        console.error(err);
        setProcessingStatus('Sohbet için belge geri yüklenemedi.');
        setTimeout(() => setIsProcessing(false), 2000);
        return;
      }
      setIsProcessing(false);
    }
  };

  return (
    <Routes>
      <Route path="/auth/callback" element={<AuthCallback />} />
      <Route path="*" element={
        <div className="app-layout">
          {/* Main content area */}
          <div className={`main-content ${isSidebarOpen ? 'with-sidebar' : ''}`}>
            {/* Header */}
            <header className="app-header">
              <div className="header-left">
                <div className="header-brand" onClick={handleNewChat}>
                  <span className="header-brand-accent">SATCHAT</span>
                </div>
              </div>
              <div className="header-right">
                <div className="header-auth">
                  {isAuthenticated && user ? (
                    <div className="user-profile">
                      <span className="user-name">Merhaba, {user.name}</span>
                      <button className="auth-button logout-button" onClick={logout}>
                        Çıkış Yap
                      </button>
                    </div>
                  ) : (
                    <button className="auth-button login-button" onClick={handleLogin}>
                      Giriş Yap
                    </button>
                  )}
                </div>
                <button
                  className="header-menu-btn"
                  onClick={() => setIsSidebarOpen(!isSidebarOpen)}
                  title="Kenar Menüsü"
                >
                  ☰
                </button>
              </div>
            </header>

            {/* Chat page */}
            <ChatPage
              messages={messages}
              isThinking={isThinking}
              isStreaming={isStreaming}
              onSend={handleSend}
              onFileSelected={handleFileSelected}
              attachedFile={pdfFile}
              onRemoveFile={handleRemoveFile}
            />
          </div>

          {/* Sidebar */}
          <ChatHistorySidebar
            isOpen={isSidebarOpen}
            onClose={() => setIsSidebarOpen(false)}
            onToggle={() => setIsSidebarOpen(!isSidebarOpen)}
            onSelectConversation={handleSelectConversation}
            onNewChat={handleNewChat}
          />

          {/* Processing Overlay */}
          <AnimatePresence>
            {isProcessing && (
              <motion.div
                className="processing-overlay"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.3 }}
              >
                <div className="processing-spinner" />
                <div className="processing-text">{processingStatus}</div>
                <div className="processing-subtext">{pdfFile?.name}</div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      } />
    </Routes>
  );
}

export default App;
