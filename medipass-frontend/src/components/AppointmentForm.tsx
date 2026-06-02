import { useState } from 'react';
import axios from 'axios';
import { Calendar, User, Stethoscope, AlertCircle } from 'lucide-react';

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

interface AppointmentFormProps {
  doctors: Doctor[];
  patients: Patient[];
  addToast: (msg: string, type: 'success' | 'error' | 'warning' | 'info') => void;
}

export default function AppointmentForm({ doctors, patients, addToast }: AppointmentFormProps) {
  const [patientId, setPatientId] = useState('');
  const [doctorId, setDoctorId] = useState('');
  const [procedureCode, setProcedureCode] = useState('');
  const [date, setDate] = useState('');
  const [time, setTime] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg('');

    if (!patientId || !doctorId || !procedureCode || !date || !time) {
      setErrorMsg('Por favor complete todos los campos.');
      return;
    }

    const doc = doctors.find(d => d.id === doctorId);
    if (!doc) return;

    // Combine date and time
    const appointmentDate = new Date(`${date}T${time}:00Z`).toISOString();

    setIsLoading(true);
    try {
      // Usamos el API Gateway en puerto 5000 (ruta /api/agendahub/appointment)
      await axios.post('http://localhost:5000/api/agendahub/Appointment', {
        patientId,
        doctorId,
        specialty: doc.specialty,
        appointmentDate,
        procedureCode
      });
      
      addToast('Cita agendada con éxito. Esperando actualización del historial médico...', 'success');
      // Reset form
      setPatientId('');
      setDoctorId('');
      setProcedureCode('');
      setDate('');
      setTime('');
    } catch (error: any) {
      if (error.response && error.response.data) {
        // Asumiendo que la API devuelve un string con el error (DomainException)
        let extractedError = "Error al conectar con el servidor.";
        if (typeof error.response.data === 'string') {
          // Extrae la primera línea antes del 'at' para limpiar el stack trace feo de ASP.NET
          const firstLine = error.response.data.split('\\n')[0];
          extractedError = firstLine.split(' at ')[0].replace('Domain.Exceptions.DomainException: ', '').trim();
        } else if (error.response.data?.message) {
          extractedError = error.response.data.message;
        }
        setErrorMsg(extractedError);
        addToast('No se pudo agendar la cita. Revisa los mensajes de error.', 'error');
      } else {
        setErrorMsg('Error de conexión con el servidor.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="glass-panel">
      <h2>Agendar Nueva Cita</h2>
      
      {errorMsg && (
        <div className="alert alert-error">
          <AlertCircle size={20} />
          <span>{errorMsg}</span>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label"><User size={14} style={{display:'inline', marginRight: '4px'}}/> Paciente</label>
          <select 
            className="form-select" 
            value={patientId} 
            onChange={(e) => setPatientId(e.target.value)}
          >
            <option value="">Seleccione un paciente...</option>
            {patients.map(p => (
              <option key={p.id} value={p.id}>{p.fullName} ({p.insuranceNumber})</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label className="form-label"><Stethoscope size={14} style={{display:'inline', marginRight: '4px'}}/> Médico Tratante</label>
          <select 
            className="form-select" 
            value={doctorId} 
            onChange={(e) => setDoctorId(e.target.value)}
          >
            <option value="">Seleccione un médico...</option>
            {doctors.map(d => (
              <option key={d.id} value={d.id}>{d.fullName} - {d.specialty}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label className="form-label">Procedimiento</label>
          <select 
            className="form-select" 
            value={procedureCode} 
            onChange={(e) => setProcedureCode(e.target.value)}
          >
            <option value="">Seleccione procedimiento...</option>
            <option value="PROC-BASIC">Consulta General Básica (PROC-BASIC)</option>
            <option value="PROC-CONSULTATION">Consulta Especializada (PROC-CONSULTATION)</option>
            <option value="PROC-CARDIOLOGY">Evaluación Cardiológica (PROC-CARDIOLOGY)</option>
            <option value="PROC-MRI">Resonancia Magnética (PROC-MRI)</option>
            <option value="PROC-ONCOLOGY">Evaluación Oncológica (PROC-ONCOLOGY)</option>
            <option value="PROC-CHEMOTHERAPY">Quimioterapia (PROC-CHEMOTHERAPY)</option>
            <option value="PROC-001">Procedimiento Prueba 1 (PROC-001)</option>
            <option value="PROC-002">Procedimiento Prueba 2 (PROC-002)</option>
            <option value="PROC-003">Procedimiento Prueba 3 (PROC-003)</option>
            <option value="PROC-INVALID">Procedimiento Inválido (Para forzar rechazo)</option>
          </select>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <div className="form-group">
            <label className="form-label"><Calendar size={14} style={{display:'inline', marginRight: '4px'}}/> Fecha</label>
            <input 
              type="date" 
              className="form-input" 
              value={date} 
              onChange={(e) => setDate(e.target.value)}
            />
          </div>
          <div className="form-group">
            <label className="form-label">Hora (UTC)</label>
            <input 
              type="time" 
              className="form-input" 
              value={time} 
              onChange={(e) => setTime(e.target.value)}
            />
          </div>
        </div>

        <button type="submit" className="btn-primary" disabled={isLoading}>
          {isLoading ? 'Procesando...' : 'Confirmar Cita Médica'}
        </button>
      </form>
    </div>
  );
}
