import { useEffect, useState } from 'react';
import axios from 'axios';
import AppointmentForm from '../components/AppointmentForm';
import { Activity } from 'lucide-react';

interface Doctor {
  id: string;
  fullName: string;
  specialty: string;
}

interface Patient {
  id: string;
  fullName: string;
  insuranceNumber: string;
}

export default function Dashboard({ addToast }: { addToast: (msg: string, type: 'success' | 'error' | 'warning' | 'info') => void }) {
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchCatalogs = async () => {
      try {
        // Obtenemos los datos desde el API Gateway
        const docRes = await axios.get('http://localhost:5000/api/agendahub/Catalog/doctors');
        const patRes = await axios.get('http://localhost:5000/api/agendahub/Catalog/patients');

        setDoctors(docRes.data || []);
        setPatients(patRes.data || []);
      } catch (err) {
        console.error("Error fetching catalogs", err);
        addToast("Error al cargar los catálogos desde el servidor", "error");
      } finally {
        setLoading(false);
      }
    };

    fetchCatalogs();
  }, []);

  return (
    <div className="dashboard-grid">
      <div className="main-content">
        {loading ? (
          <div className="glass-panel" style={{ textAlign: 'center', padding: '4rem' }}>
            <Activity size={40} color="var(--primary-color)" style={{ animation: 'pulse 2s infinite' }} />
            <p style={{ marginTop: '1rem' }}>Cargando datos del sistema...</p>
          </div>
        ) : (
          <AppointmentForm doctors={doctors} patients={patients} addToast={addToast} />
        )}
      </div>

      <div className="sidebar">
        <div className="glass-panel" style={{ marginBottom: '1.5rem' }}>
          <h3>Estadísticas del Día</h3>
          <div style={{ marginTop: '1rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Pacientes Activos</span>
              <strong>{patients.length}</strong>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Médicos Disponibles</span>
              <strong>{doctors.length}</strong>
            </div>
          </div>
        </div>

        <div className="glass-panel">
          <h3>Información del Sistema</h3>
          <p style={{ fontSize: '0.875rem', marginTop: '1rem' }}>
            Este terminal está conectado al núcleo de <strong>Medipass</strong> mediante Arquitectura Hexagonal y Microservicios.
          </p>
          <ul style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '1rem', listStyle: 'none' }}>
            <li style={{ marginBottom: '0.5rem' }}> Validación de Seguros en Tiempo Real</li>
            <li style={{ marginBottom: '0.5rem' }}> Prevención de Double-Booking</li>
            <li> Sincronización Asíncrona EHR (RabbitMQ)</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
