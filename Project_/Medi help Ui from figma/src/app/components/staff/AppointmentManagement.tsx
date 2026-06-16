import { useState } from 'react';
import { useApp, Appointment } from '../../context/AppContext';
import { Plus, Search, Pencil, Trash2, X, Check, Calendar } from 'lucide-react';

type FormData = Omit<Appointment, 'id'>;

export function AppointmentManagement() {
  const { appointments, patients, users, addAppointment, updateAppointment, removeAppointment } = useApp();
  const doctors = users.filter(u => u.role === 'doctor');
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('All');
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [msg, setMsg] = useState('');

  const emptyForm: FormData = {
    patientId: patients[0]?.id || '',
    patientName: patients[0]?.name || '',
    doctorId: doctors[0]?.id || '',
    doctorName: doctors[0]?.name || '',
    date: new Date().toISOString().split('T')[0],
    time: '09:00',
    status: 'Scheduled',
    reason: '',
  };
  const [form, setForm] = useState<FormData>(emptyForm);

  const filtered = appointments.filter(a =>
    (filterStatus === 'All' || a.status === filterStatus) &&
    (a.patientName.toLowerCase().includes(search.toLowerCase()) ||
      a.doctorName.toLowerCase().includes(search.toLowerCase()) ||
      a.date.includes(search))
  ).sort((a, b) => a.date.localeCompare(b.date));

  const openAdd = () => { setForm(emptyForm); setEditId(null); setShowForm(true); };
  const openEdit = (a: Appointment) => { setForm({ patientId: a.patientId, patientName: a.patientName, doctorId: a.doctorId, doctorName: a.doctorName, date: a.date, time: a.time, status: a.status, reason: a.reason || '' }); setEditId(a.id); setShowForm(true); };

  const handlePatientChange = (id: string) => {
    const p = patients.find(p => p.id === id);
    setForm(f => ({ ...f, patientId: id, patientName: p?.name || '' }));
  };
  const handleDoctorChange = (id: string) => {
    const d = doctors.find(d => d.id === id);
    setForm(f => ({ ...f, doctorId: id, doctorName: d?.name || '' }));
  };

  const handleSave = () => {
    if (!form.patientId || !form.doctorId || !form.date || !form.time) { setMsg('Please fill all required fields.'); return; }
    if (editId) { updateAppointment(editId, form); flash('Appointment updated.'); }
    else { addAppointment(form); flash('Appointment scheduled.'); }
    setShowForm(false);
  };

  const flash = (text: string) => { setMsg(text); setTimeout(() => setMsg(''), 3000); };

  const statusColor: Record<string, { bg: string; color: string }> = {
    Scheduled: { bg: '#eff6ff', color: '#1d4ed8' },
    Completed: { bg: '#f0fdf4', color: '#16a34a' },
    Cancelled: { bg: '#fef2f2', color: '#991b1b' },
  };

  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Appointment Management</h2><p className="text-gray-500 text-sm">Schedule and manage patient appointments</p></div>
        <button onClick={openAdd} className="flex items-center gap-2 px-4 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}><Plus size={16} /> New Appointment</button>
      </div>

      {msg && <div className="px-4 py-2.5 rounded-lg text-sm bg-green-50 text-green-800 border border-green-200">{msg}</div>}

      <div className="flex gap-3">
        <div className="relative flex-1"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by patient, doctor, or date..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)} className="px-3 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none">
          <option>All</option><option>Scheduled</option><option>Completed</option><option>Cancelled</option>
        </select>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table className="w-full">
          <thead><tr style={{ background: '#4E0205' }}>{['Patient', 'Doctor', 'Date', 'Time', 'Reason', 'Status', 'Actions'].map(h => <th key={h} className="px-4 py-3 text-left text-white text-xs" style={{ fontWeight: 600 }}>{h}</th>)}</tr></thead>
          <tbody>
            {filtered.map((a, i) => (
              <tr key={a.id} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                <td className="px-4 py-3 text-gray-800 text-sm" style={{ fontWeight: 500 }}>{a.patientName}</td>
                <td className="px-4 py-3 text-gray-600 text-sm">{a.doctorName}</td>
                <td className="px-4 py-3 text-gray-600 text-sm">{a.date}</td>
                <td className="px-4 py-3 text-gray-600 text-sm">{a.time}</td>
                <td className="px-4 py-3 text-gray-500 text-sm">{a.reason || '—'}</td>
                <td className="px-4 py-3">
                  <select value={a.status} onChange={e => updateAppointment(a.id, { status: e.target.value as Appointment['status'] })}
                    className="px-2 py-0.5 rounded-full text-xs border-0 outline-none cursor-pointer"
                    style={{ background: statusColor[a.status]?.bg, color: statusColor[a.status]?.color, fontWeight: 600 }}>
                    <option value="Scheduled">Scheduled</option>
                    <option value="Completed">Completed</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-2">
                    <button onClick={() => openEdit(a)} className="p-1.5 rounded-lg hover:bg-blue-50 text-blue-600"><Pencil size={14} /></button>
                    <button onClick={() => setDeleteId(a.id)} className="p-1.5 rounded-lg hover:bg-red-50" style={{ color: '#8A0007' }}><Trash2 size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && <tr><td colSpan={7} className="text-center py-8 text-gray-400 text-sm">No appointments found.</td></tr>}
          </tbody>
        </table>
      </div>

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)', borderRadius: '1rem 1rem 0 0' }}>
              <h3 className="text-white" style={{ fontWeight: 700 }}>{editId ? 'Edit Appointment' : 'Schedule Appointment'}</h3>
              <button onClick={() => setShowForm(false)} className="text-red-200 hover:text-white"><X size={20} /></button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Patient *</label>
                <select value={form.patientId} onChange={e => handlePatientChange(e.target.value)} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white">
                  {patients.map(p => <option key={p.id} value={p.id}>{p.name} {p.studentId ? `(${p.studentId})` : ''}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Doctor *</label>
                <select value={form.doctorId} onChange={e => handleDoctorChange(e.target.value)} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white">
                  {doctors.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Date *</label><input type="date" value={form.date} onChange={e => setForm(f => ({ ...f, date: e.target.value }))} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" /></div>
                <div><label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Time *</label><input type="time" value={form.time} onChange={e => setForm(f => ({ ...f, time: e.target.value }))} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" /></div>
              </div>
              <div><label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Reason</label><input type="text" value={form.reason || ''} onChange={e => setForm(f => ({ ...f, reason: e.target.value }))} placeholder="Chief complaint / reason for visit" className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" /></div>
              {editId && (
                <div><label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Status</label>
                  <select value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as Appointment['status'] }))} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white">
                    <option>Scheduled</option><option>Completed</option><option>Cancelled</option>
                  </select>
                </div>
              )}
              {msg && <p className="text-red-600 text-sm">{msg}</p>}
              <div className="flex gap-3 pt-2">
                <button onClick={() => setShowForm(false)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
                <button onClick={handleSave} className="flex-1 py-2 rounded-lg text-white text-sm flex items-center justify-center gap-2" style={{ background: '#8A0007', fontWeight: 600 }}><Check size={16} />{editId ? 'Update' : 'Schedule'}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 p-6 text-center">
            <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-4" style={{ background: '#fff0f0' }}><Trash2 size={22} style={{ color: '#8A0007' }} /></div>
            <h3 className="text-gray-800 mb-2" style={{ fontWeight: 700 }}>Cancel Appointment?</h3>
            <p className="text-gray-500 text-sm mb-5">This will permanently remove the appointment record.</p>
            <div className="flex gap-3">
              <button onClick={() => setDeleteId(null)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Keep</button>
              <button onClick={() => { removeAppointment(deleteId); setDeleteId(null); flash('Appointment removed.'); }} className="flex-1 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>Remove</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
