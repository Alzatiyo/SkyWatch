import { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import { Stethoscope, Bell } from 'lucide-react';
import * as signalR from '@microsoft/signalr';

export interface ToastMessage {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
}

function App() {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  // Configuración de SignalR para escuchar eventos de RabbitMQ procesados por EHRLogger
  useEffect(() => {
    // Usaremos el puerto 5000 que será nuestro API Gateway (YARP)
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/notifications")
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => console.log('SignalR Connected!'))
      .catch(err => console.error('SignalR Connection Error: ', err));

    connection.on("ReceiveEhrNotification", (message: string) => {
      addToast(message, 'success');
    });

    return () => {
      connection.stop();
    };
  }, []);

  const addToast = (message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info') => {
    const id = Date.now();
    setToasts(prev => [...prev, { id, message, type }]);
  };

  const removeToast = (id: number) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  };

  return (
    <Router>
      <div className="app-container">
        <header className="navbar">
          <div className="logo">
            <Stethoscope size={28} />
            Medipass Portal
          </div>
          <div style={{ color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Bell size={20} />
            <span style={{ fontSize: '0.875rem' }}>Recepción Principal</span>
          </div>
        </header>

        <main>
          <Routes>
            <Route path="/" element={<Dashboard addToast={addToast} />} />
          </Routes>
        </main>

        <div className="toast-container">
          {toasts.map(toast => (
            <div key={toast.id} className={`toast ${toast.type}`} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem' }}>
              <div>
                {toast.type === 'success' ? '✅ ' : toast.type === 'error' ? '❌ ' : '⚠️ '}
                {toast.message}
              </div>
              <button 
                onClick={() => removeToast(toast.id)} 
                style={{ background: 'transparent', border: 'none', color: 'inherit', cursor: 'pointer', fontSize: '1.2rem', padding: '0' }}
                aria-label="Cerrar"
              >
                &times;
              </button>
            </div>
          ))}
        </div>
      </div>
    </Router>
  );
}

export default App;
