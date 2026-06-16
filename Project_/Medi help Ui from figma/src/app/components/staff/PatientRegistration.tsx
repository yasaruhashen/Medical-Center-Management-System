import { useState } from 'react';
import { useApp, Patient } from '../../context/AppContext';
import { Plus, Search, Pencil, Trash2, X, Check, User } from 'lucide-react';

type FormData = Omit<Patient, 'id' | 'registeredDate'>;
const emptyForm: FormData = { name: '', dob: '', gender: 'Male', phone: '', address: '', studentId: '', bloodGroup: '', allergies: '' };

export function PatientRegistration() {
  const { patients, addPatient, updatePatient, removePatient } = useApp();
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [msg, setMsg] = useState('');
  const [view, setView] = useState<Patient | null>(null);

  const filtered = patients.filter(p =>
    p.name.toLowerCase().includes(search.toLowerCase()) ||
    (p.studentId || '').toLowerCase().includes(search.toLowerCase()) ||
    p.phone.includes(search)
  );

  const openAdd = () => { setForm(emptyForm); setEditId(null); setShowForm(true); };
  const openEdit = (p: Patient) => { setForm({ name: p.name, dob: p.dob, gender: p.gender, phone: p.phone, address: p.address, studentId: p.studentId || '', bloodGroup: p.bloodGroup || '', allergies: p.allergies || '' }); setEditId(p.id); setShowForm(true); };

  const handleSave = () => {
    if (!form.name || !form.dob || !form.phone) { setMsg('Name, date of birth, and phone are required.'); return; }
    if (editId) { updatePatient(editId, form); flash('Patient updated successfully.'); }
    else { addPatient(form); flash('Patient registered successfully.'); }
    setShowForm(false);
  };

  const flash = (text: string) => { setMsg(text); setTimeout(() => setMsg(''), 3000); };

  const bloodColors: Record<string, string> = { 'A+': '#8A0007', 'A-': '#4E0205', 'B+': '#b45309', 'B-': '#92400e', 'O+': '#15803d', 'O-': '#166534', 'AB+': '#1d4ed8', 'AB-': '#1e40af' };

  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Patient Registration</h2><p className="text-gray-500 text-sm">Register new patients and manage existing records</p></div>
        <button onClick={openAdd} className="flex items-center gap-2 px-4 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}><Plus size={16} /> Register Patient</button>
      </div>

      {msg && <div className="px-4 py-2.5 rounded-lg text-sm bg-green-50 text-green-800 border border-green-200">{msg}</div>}

      <div className="relative"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name, student ID, or phone..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table className="w-full">
          <thead><tr style={{ background: '#4E0205' }}>{['Patient', 'Student ID', 'Gender', 'Blood Group', 'Phone', 'Registered', 'Actions'].map(h => <th key={h} className="px-4 py-3 text-left text-white text-xs" style={{ fontWeight: 600 }}>{h}</th>)}</tr></thead>
          <tbody>
            {filtered.map((p, i) => (
              <tr key={p.id} className={`${i % 2 === 0 ? 'bg-white' : 'bg-gray-50'} hover:bg-red-50 cursor-pointer transition-colors`} onClick={() => setView(p)}>
                <td className="px-4 py-3"><div className="flex items-center gap-2"><div className="w-7 h-7 rounded-full flex items-center justify-center text-white text-xs flex-shrink-0" style={{ background: '#8A0007', fontWeight: 700 }}>{p.name.charAt(0)}</div><span className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{p.name}</span></div></td>
                <td className="px-4 py-3 text-gray-600 text-sm">{p.studentId || '—'}</td>
                <td className="px-4 py-3 text-gray-600 text-sm">{p.gender}</td>
                <td className="px-4 py-3"><span className="px-2 py-0.5 rounded text-xs text-white" style={{ background: bloodColors[p.bloodGroup || ''] || '#6b7280', fontWeight: 700 }}>{p.bloodGroup || '—'}</span></td>
                <td className="px-4 py-3 text-gray-600 text-sm">{p.phone}</td>
                <td className="px-4 py-3 text-gray-500 text-xs">{p.registeredDate}</td>
                <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                  <div className="flex gap-2">
                    <button onClick={() => openEdit(p)} className="p-1.5 rounded-lg hover:bg-blue-50 text-blue-600"><Pencil size={14} /></button>
                    <button onClick={() => setDeleteId(p.id)} className="p-1.5 rounded-lg hover:bg-red-50" style={{ color: '#8A0007' }}><Trash2 size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && <tr><td colSpan={7} className="text-center py-8 text-gray-400 text-sm">No patients found.</td></tr>}
          </tbody>
        </table>
      </div>

      {/* Patient detail view modal */}
      {view && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)', borderRadius: '1rem 1rem 0 0' }}>
              <h3 className="text-white" style={{ fontWeight: 700 }}>Patient Details</h3>
              <button onClick={() => setView(null)} className="text-red-200 hover:text-white"><X size={20} /></button>
            </div>
            <div className="p-6 space-y-3">
              <div className="flex items-center gap-4 mb-4">
                <div className="w-14 h-14 rounded-full flex items-center justify-center text-white text-xl" style={{ background: '#8A0007', fontWeight: 700 }}>{view.name.charAt(0)}</div>
                <div><p className="text-gray-800" style={{ fontWeight: 700, fontSize: '1.1rem' }}>{view.name}</p><p className="text-gray-500 text-sm">{view.studentId || 'No Student ID'}</p></div>
              </div>
              {[['Date of Birth', view.dob], ['Gender', view.gender], ['Phone', view.phone], ['Blood Group', view.bloodGroup || '—'], ['Allergies', view.allergies || 'None'], ['Address', view.address], ['Registered', view.registeredDate]].map(([k, v]) => (
                <div key={k} className="flex justify-between py-1.5 border-b border-gray-50"><span className="text-gray-500 text-sm">{k}</span><span className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{v}</span></div>
              ))}
              <div className="flex gap-3 pt-2">
                <button onClick={() => { setView(null); openEdit(view); }} className="flex-1 py-2 rounded-lg text-sm border border-gray-200 text-gray-600 hover:bg-gray-50 flex items-center justify-center gap-1"><Pencil size={14} /> Edit</button>
                <button onClick={() => setView(null)} className="flex-1 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>Close</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Add/Edit modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between px-6 py-4 sticky top-0" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)', borderRadius: '1rem 1rem 0 0' }}>
              <h3 className="text-white" style={{ fontWeight: 700 }}>{editId ? 'Edit Patient' : 'Register New Patient'}</h3>
              <button onClick={() => setShowForm(false)} className="text-red-200 hover:text-white"><X size={20} /></button>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                {[
                  { label: 'Full Name *', field: 'name', type: 'text', col: 'col-span-2' },
                  { label: 'Date of Birth *', field: 'dob', type: 'date', col: '' },
                  { label: 'Phone *', field: 'phone', type: 'tel', col: '' },
                  { label: 'Student ID', field: 'studentId', type: 'text', col: '' },
                  { label: 'Blood Group', field: 'bloodGroup', type: 'text', col: '' },
                  { label: 'Allergies', field: 'allergies', type: 'text', col: 'col-span-2' },
                  { label: 'Address', field: 'address', type: 'text', col: 'col-span-2' },
                ].map(({ label, field, type, col }) => (
                  <div key={field} className={col || ''}>
                    <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>{label}</label>
                    <input type={type} value={(form as Record<string, string>)[field]} onChange={e => setForm(f => ({ ...f, [field]: e.target.value }))}
                      className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white" />
                  </div>
                ))}
                <div>
                  <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Gender *</label>
                  <select value={form.gender} onChange={e => setForm(f => ({ ...f, gender: e.target.value as 'Male' | 'Female' | 'Other' }))}
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white">
                    <option>Male</option><option>Female</option><option>Other</option>
                  </select>
                </div>
              </div>
              {msg && <p className="text-red-600 text-sm">{msg}</p>}
              <div className="flex gap-3 pt-2">
                <button onClick={() => setShowForm(false)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
                <button onClick={handleSave} className="flex-1 py-2 rounded-lg text-white text-sm flex items-center justify-center gap-2" style={{ background: '#8A0007', fontWeight: 600 }}><Check size={16} />{editId ? 'Update' : 'Register'}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 p-6 text-center">
            <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-4" style={{ background: '#fff0f0' }}><Trash2 size={22} style={{ color: '#8A0007' }} /></div>
            <h3 className="text-gray-800 mb-2" style={{ fontWeight: 700 }}>Remove Patient?</h3>
            <p className="text-gray-500 text-sm mb-5">This will permanently delete the patient record.</p>
            <div className="flex gap-3">
              <button onClick={() => setDeleteId(null)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
              <button onClick={() => { removePatient(deleteId); setDeleteId(null); flash('Patient removed.'); }} className="flex-1 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>Remove</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
