import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../store';

export default function AuthCallback() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const { setAuth } = useAuthStore();

    useEffect(() => {
        const token = searchParams.get('token');
        if (token) {
            // Decode JWT slightly to get basic user info
            try {
                const payloadBase64 = token.split('.')[1];
                const decodedJson = atob(payloadBase64);
                const decoded = JSON.parse(decodedJson);

                setAuth(token, {
                    id: decoded.sub || '',
                    email: decoded.email || '',
                    name: decoded.name || 'User',
                });
            } catch (e) {
                console.error('Failed to parse token payload', e);
            }
        }

        // Redirect back to home
        navigate('/', { replace: true });
    }, [searchParams, navigate, setAuth]);

    return (
        <div style={{ display: 'flex', height: '100vh', width: '100%', justifyContent: 'center', alignItems: 'center', background: 'var(--bg-primary)' }}>
            <div className="processing-spinner" />
            <span style={{ marginLeft: 16, color: 'var(--text-primary)' }}>Giriş yapılıyor...</span>
        </div>
    );
}
