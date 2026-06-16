import { useState } from 'react';
import { useApp, PrescriptionItem } from '../../context/AppContext';
import { Plus, Trash2, Check, Search, FileText, AlertTriangle } from 'lucide-react';

export function WritePrescription() {
  const { patients, inventory, prescriptions, addPrescription, currentUser } = useApp();
  const [patientId, setPatientId] = useState('');
  const [diagnosis, setDiagnosis] = useState('');
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState<PrescriptionItem[]>([]);
  const [msg, setMsg] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
  const [search, setSearch] = useState('');
  const [tab, setTab] = useState<'write' | 'history'>('write');

  const myRx = prescriptions.filter(p => p.doctorId === currentUser?.id).sort((a, b) => b.date.localeCompare(a.date));
  const selectedPatient = patients.find(p => p.id === patientId);

  const addItem = () => {
    setItems(prev => [...prev, { inventoryItemId: inventory[0]?.id || '', medicationName: inventory[0]?.name || '', dosage: '', quantity: 1, instructions: '' }]);
  };

  const updateItem = (i: number, field: keyof PrescriptionItem, value: string | number) => {
    setItems(prev => {
      const next = [...prev];
      if (field === 'inventoryItemId') {
        const inv = inventory.find(item => item.id === value);
        next[i] = { ...next[i], inventoryItemId: value as string, medicationName: inv?.name || '' };
      } else {
        next[i] = { ...next[i], [field]: value };
      }
      return next;
    });
  };

  const removeItem = (i: number) => setItems(prev => prev.filter((_, idx) => idx !== i));

  const handleSubmit = () => {
    if (!patientId) { setMsg({ text: 'Please select a patient.', type: 'error' }); return; }
    if (items.length === 0) { setMsg({ text: 'Please add at least one medication.', type: 'error' }); return; }
    if (items.some(i => !i.dosage || !i.instructions)) { setMsg({ text: 'Please fill in dosage and instructions for all medications.', type: 'error' }); return; }

    const patient = patients.find(p => p.id === patientId)!;
    addPrescription({
      patientId,
      patientName: patient.name,
      doctorId: currentUser!.id,
      doctorName: currentUser!.name,
      date: new Date().toISOString().split('T')[0],
      items,
      status: 'Pending',
      diagnosis,
      notes,
    });

    setMsg({ text: 'Prescription submitted successfully. Staff can now process it.', type: 'success' });
    setPatientId('');
    setDiagnosis('');
    setNotes('');
    setItems([]);
    setTimeout(() => setMsg(null), 5000);
  };

  const filteredRx = myRx.filter(rx =>
    rx.patientName.toLowerCase().includes(search.toLowerCase()) ||
    (rx.diagnosis || '').toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Prescriptions</h2><p className="text-gray-500 text-sm">Write new prescriptions or view your prescription history.</p></div>
        <div className="flex rounded-lg overflow-hidden border border-gray-200">
          {[{ id: 'write', label: 'Write Prescription' }, { id: 'history', label: 'History' }].map(t => (
            <button key={t.id} onClick={() => setTab(t.id as 'write' | 'history')}
              className="px-4 py-2 text-sm transition-colors"
              style={{ background: tab === t.id ? '#8A0007' : 'white', color: tab === t.id ? 'white' : '#374151', fontWeight: tab === t.id ? 600 : 400 }}>
              {t.label}
            </button>
          ))}
        </div>
      </div>

      {tab === 'write' ? (
        <div className="space-y-5">
          {msg && (
            <div className={`flex items-center gap-2 px-4 py-3 rounded-xl border text-sm ${msg.type === 'success' ? 'bg-green-50 border-green-200 text-green-800' : 'bg-red-50 border-red-200 text-red-800'}`}>
              {msg.type === 'success' ? <Check size={16} /> : <AlertTriangle size={16} />}{msg.text}
            </div>
          )}

          <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 space-y-4">
            <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Patient & Diagnosis</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Patient *</label>
                <select value={patientId} onChange={e => setPatientId(e.target.value)} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white">
                  <option value="">Select patient...</option>
                  {patients.map(p => <option key={p.id} value={p.id}>{p.name} {p.studentId ? `(${p.studentId})` : ''}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Diagnosis</label>
                <input type="text" value={diagnosis} onChange={e => setDiagnosis(e.target.value)} placeholder="e.g. Common Cold, Pharyngitis" className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white" />
              </div>
            </div>

            {selectedPatient && selectedPatient.allergies && selectedPatient.allergies !== 'None' && (
              <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-50 border border-amber-200">
                <AlertTriangle size={15} className="text-amber-600" />
                <p className="text-amber-700 text-sm">Patient has known allergies: <strong>{selectedPatient.allergies}</strong></p>
              </div>
            )}

            <div>
              <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Notes / Instructions</label>
              <textarea value={notes} onChange={e => setNotes(e.target.value)} placeholder="Additional instructions for the patient..." rows={2} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white resize-none" />
            </div>
          </div>

          {/* Medications */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Medications</h3>
              <button onClick={addItem} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}><Plus size={15} /> Add Medication</button>
            </div>

            {items.length === 0 ? (
              <div className="text-center py-8 border-2 border-dashed border-gray-200 rounded-xl text-gray-400 text-sm">
                <FileText size={30} className="mx-auto mb-2 opacity-40" />
                No medications added yet. Click "Add Medication" to start.
              </div>
            ) : (
              <div className="space-y-3">
                {items.map((item, idx) => {
                  const inv = inventory.find(i => i.id === item.inventoryItemId);
                  return (
                    <div key={idx} className="p-4 border border-gray-100 rounded-xl bg-gray-50 space-y-3">
                      <div className="flex items-center justify-between">
                        <p className="text-gray-600 text-sm" style={{ fontWeight: 600 }}>Medication #{idx + 1}</p>
                        <button onClick={() => removeItem(idx)} className="p-1 rounded hover:bg-red-50" style={{ color: '#8A0007' }}><Trash2 size={14} /></button>
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <div className="col-span-2">
                          <label className="block text-xs text-gray-600 mb-1">Select Medicine</label>
                          <select value={item.inventoryItemId} onChange={e => updateItem(idx, 'inventoryItemId', e.target.value)} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white">
                            {inventory.map(i => <option key={i.id} value={i.id}>{i.name} (Stock: {i.quantity} {i.unit})</option>)}
                          </select>
                          {inv && inv.quantity === 0 && <p className="text-red-600 text-xs mt-1">⚠ This item is out of stock</p>}
                        </div>
                        <div>
                          <label className="block text-xs text-gray-600 mb-1">Dosage *</label>
                          <input type="text" value={item.dosage} onChange={e => updateItem(idx, 'dosage', e.target.value)} placeholder="e.g. 500mg" className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-600 mb-1">Quantity *</label>
                          <input type="number" value={item.quantity} onChange={e => updateItem(idx, 'quantity', Number(e.target.value))} min={1} max={inv?.quantity || 999} className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" />
                        </div>
                        <div className="col-span-2">
                          <label className="block text-xs text-gray-600 mb-1">Instructions *</label>
                          <input type="text" value={item.instructions} onChange={e => updateItem(idx, 'instructions', e.target.value)} placeholder="e.g. Take 1 tablet 3 times daily after meals" className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none bg-white" />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <div className="flex gap-3">
            <button onClick={() => { setPatientId(''); setDiagnosis(''); setNotes(''); setItems([]); }} className="px-6 py-2.5 rounded-xl border border-gray-200 text-gray-600 text-sm">Clear</button>
            <button onClick={handleSubmit} className="flex-1 py-2.5 rounded-xl text-white flex items-center justify-center gap-2" style={{ background: '#8A0007', fontWeight: 600 }}>
              <Check size={18} /> Submit Prescription
            </button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="relative"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by patient or diagnosis..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>

          {filteredRx.map(rx => (
            <div key={rx.id} className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
              <div className="flex items-center justify-between px-5 py-3" style={{ background: rx.status === 'Processed' ? '#f0fdf4' : '#fffbeb', borderBottom: '1px solid', borderColor: rx.status === 'Processed' ? '#d1fae5' : '#fde68a' }}>
                <div>
                  <p className="text-gray-800" style={{ fontWeight: 700 }}>{rx.patientName}</p>
                  <p className="text-gray-500 text-xs">{rx.diagnosis || 'General'} • {rx.date}</p>
                </div>
                <span className="px-2 py-1 rounded-full text-xs" style={{ background: rx.status === 'Processed' ? '#d1fae5' : '#fde68a', color: rx.status === 'Processed' ? '#065f46' : '#92400e', fontWeight: 600 }}>{rx.status}</span>
              </div>
              <div className="p-4 space-y-1.5">
                {rx.items.map((item, i) => (
                  <div key={i} className="flex justify-between text-sm"><span className="text-gray-700">{item.medicationName} {item.dosage}</span><span className="text-gray-500">×{item.quantity} — {item.instructions}</span></div>
                ))}
                {rx.notes && <p className="text-gray-400 text-xs pt-1 border-t border-gray-100 mt-2">📝 {rx.notes}</p>}
              </div>
            </div>
          ))}
          {filteredRx.length === 0 && <div className="text-center py-12 text-gray-400 text-sm">No prescriptions found.</div>}
        </div>
      )}
    </div>
  );
}
