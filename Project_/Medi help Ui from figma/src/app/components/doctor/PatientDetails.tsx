import { useState } from 'react';
import { useApp, Patient } from '../../context/AppContext';
import { Search, User, Calendar, FileText, Pill, ChevronRight, X } from 'lucide-react';

export function PatientDetails() {
  const { patients, appointments, prescriptions, currentUser } = useApp();
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<Patient | null>(null);

  const filtered = patients.filter(p =>
    p.name.toLowerCase().includes(search.toLowerCase()) ||
    (p.studentId || '').toLowerCase().includes(search.toLowerCase()) ||
    p.phone.includes(search)
  );

  const patientAppts = selected ? appointments.filter(a => a.patientId === selected.id).sort((a, b) => b.date.localeCompare(a.date)) : [];
  const patientRx = selected ? prescriptions.filter(p => p.patientId === selected.id).sort((a, b) => b.date.localeCompare(a.date)) : [];

  const bloodColors: Record<string, string> = { 'A+': '#8A0007', 'A-': '#4E0205', 'B+': '#b45309', 'O+': '#15803d', 'O-': '#166534', 'AB+': '#1d4ed8', 'AB-': '#1e40af' };

  return (
    <div className="p-6 space-y-5">
      <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Patient Details</h2><p className="text-gray-500 text-sm">View patient records, appointment history, and prescriptions.</p></div>

      <div className="relative"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search patients by name, student ID, or phone..." className="w-full pl-9 pr-4 py-2.5 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>

      <div className="grid grid-cols-3 gap-4">
        {/* Patient list */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-100" style={{ background: '#4E0205' }}>
            <p className="text-white text-sm" style={{ fontWeight: 600 }}>Patients ({filtered.length})</p>
          </div>
          <div className="overflow-y-auto" style={{ maxHeight: 'calc(100vh - 280px)' }}>
            {filtered.map(p => (
              <button key={p.id} onClick={() => setSelected(p)} className="w-full flex items-center justify-between px-4 py-3 border-b border-gray-50 hover:bg-red-50 transition-colors text-left" style={{ background: selected?.id === p.id ? '#fff0f0' : 'transparent' }}>
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-xs flex-shrink-0" style={{ background: '#8A0007', fontWeight: 700 }}>{p.name.charAt(0)}</div>
                  <div>
                    <p className="text-gray-800 text-sm" style={{ fontWeight: selected?.id === p.id ? 700 : 500 }}>{p.name}</p>
                    <p className="text-gray-400 text-xs">{p.studentId || p.phone}</p>
                  </div>
                </div>
                {selected?.id === p.id && <ChevronRight size={14} style={{ color: '#8A0007' }} />}
              </button>
            ))}
            {filtered.length === 0 && <div className="text-center py-8 text-gray-400 text-sm">No patients found.</div>}
          </div>
        </div>

        {/* Patient detail */}
        {selected ? (
          <div className="col-span-2 space-y-4">
            {/* Info card */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
              <div className="flex items-center gap-4 px-5 py-4" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)' }}>
                <div className="w-14 h-14 rounded-full flex items-center justify-center text-white text-2xl" style={{ background: 'rgba(255,255,255,0.2)', fontWeight: 700 }}>{selected.name.charAt(0)}</div>
                <div>
                  <p className="text-white" style={{ fontWeight: 700, fontSize: '1.1rem' }}>{selected.name}</p>
                  <p className="text-red-200 text-sm">{selected.studentId || 'No Student ID'} • {selected.gender}</p>
                  <div className="flex items-center gap-2 mt-1">
                    {selected.bloodGroup && <span className="px-2 py-0.5 rounded text-xs text-white" style={{ background: bloodColors[selected.bloodGroup] || '#6b7280', fontWeight: 700 }}>{selected.bloodGroup}</span>}
                    {selected.allergies && selected.allergies !== 'None' && <span className="px-2 py-0.5 rounded text-xs" style={{ background: '#fef3c7', color: '#92400e', fontWeight: 600 }}>⚠ {selected.allergies}</span>}
                  </div>
                </div>
              </div>
              <div className="grid grid-cols-3 gap-0 divide-x divide-gray-100">
                {[['DOB', selected.dob], ['Phone', selected.phone], ['Registered', selected.registeredDate]].map(([k, v]) => (
                  <div key={k} className="px-4 py-3 text-center"><p className="text-gray-400 text-xs">{k}</p><p className="text-gray-800 text-sm" style={{ fontWeight: 600 }}>{v}</p></div>
                ))}
              </div>
              {selected.address && <div className="px-4 py-2 border-t border-gray-100 text-xs text-gray-500">📍 {selected.address}</div>}
            </div>

            {/* Appointment history */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-100 flex items-center gap-2" style={{ background: '#f8f0f0' }}>
                <Calendar size={15} style={{ color: '#8A0007' }} />
                <p className="text-gray-700 text-sm" style={{ fontWeight: 600 }}>Appointment History ({patientAppts.length})</p>
              </div>
              {patientAppts.length === 0 ? (
                <div className="text-center py-5 text-gray-400 text-sm">No appointments found.</div>
              ) : (
                <div className="divide-y divide-gray-50">
                  {patientAppts.map(a => (
                    <div key={a.id} className="flex items-center justify-between px-5 py-3">
                      <div><p className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{a.reason || 'General Consultation'}</p><p className="text-gray-400 text-xs">{a.doctorName} • {a.date} {a.time}</p></div>
                      <span className="px-2 py-0.5 rounded-full text-xs" style={{ background: a.status === 'Completed' ? '#f0fdf4' : a.status === 'Scheduled' ? '#eff6ff' : '#fef2f2', color: a.status === 'Completed' ? '#16a34a' : a.status === 'Scheduled' ? '#1d4ed8' : '#991b1b', fontWeight: 600 }}>{a.status}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Prescription history */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-100 flex items-center gap-2" style={{ background: '#f8f0f0' }}>
                <Pill size={15} style={{ color: '#8A0007' }} />
                <p className="text-gray-700 text-sm" style={{ fontWeight: 600 }}>Prescription History ({patientRx.length})</p>
              </div>
              {patientRx.length === 0 ? (
                <div className="text-center py-5 text-gray-400 text-sm">No prescriptions found.</div>
              ) : (
                <div className="divide-y divide-gray-50">
                  {patientRx.map(rx => (
                    <div key={rx.id} className="px-5 py-3">
                      <div className="flex items-center justify-between mb-1">
                        <p className="text-gray-800 text-sm" style={{ fontWeight: 600 }}>{rx.diagnosis || 'General'} — {rx.date}</p>
                        <span className="px-2 py-0.5 rounded-full text-xs" style={{ background: rx.status === 'Processed' ? '#f0fdf4' : '#fffbeb', color: rx.status === 'Processed' ? '#16a34a' : '#92400e', fontWeight: 600 }}>{rx.status}</span>
                      </div>
                      <div className="space-y-0.5">
                        {rx.items.map((item, i) => (
                          <p key={i} className="text-gray-500 text-xs">• {item.medicationName} {item.dosage} × {item.quantity} — {item.instructions}</p>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="col-span-2 flex items-center justify-center" style={{ minHeight: 300 }}>
            <div className="text-center text-gray-300">
              <User size={60} className="mx-auto mb-3" />
              <p>Select a patient to view their details</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
