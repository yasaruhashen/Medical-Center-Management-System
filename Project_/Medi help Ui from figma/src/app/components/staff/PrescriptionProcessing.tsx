import { useState } from 'react';
import { useApp } from '../../context/AppContext';
import { Search, CheckCircle, Clock, Package, User, Calendar, Pill } from 'lucide-react';

export function PrescriptionProcessing() {
  const { prescriptions, processPrescription, inventory } = useApp();
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState('All');
  const [processing, setProcessing] = useState<string | null>(null);
  const [msg, setMsg] = useState('');

  const filtered = prescriptions.filter(p =>
    (filter === 'All' || p.status === filter) &&
    (p.patientName.toLowerCase().includes(search.toLowerCase()) ||
      p.doctorName.toLowerCase().includes(search.toLowerCase()))
  ).sort((a, b) => b.date.localeCompare(a.date));

  const handleProcess = async (id: string) => {
    const rx = prescriptions.find(p => p.id === id);
    if (!rx) return;
    // Check stock
    for (const item of rx.items) {
      const inv = inventory.find(i => i.id === item.inventoryItemId);
      if (!inv || inv.quantity < item.quantity) {
        setMsg(`Insufficient stock for ${item.medicationName}. Available: ${inv?.quantity || 0} ${inv?.unit || ''}`);
        setTimeout(() => setMsg(''), 4000);
        return;
      }
    }
    setProcessing(id);
    await new Promise(r => setTimeout(r, 600));
    processPrescription(id);
    setProcessing(null);
    setMsg('Prescription processed successfully. Inventory updated.');
    setTimeout(() => setMsg(''), 4000);
  };

  return (
    <div className="p-6 space-y-5">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>Prescription Processing</h2>
        <p className="text-gray-500 text-sm">View and process doctor prescriptions — inventory is automatically deducted upon processing.</p>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Total', value: prescriptions.length, color: '#8A0007', bg: '#fff0f0' },
          { label: 'Pending', value: prescriptions.filter(p => p.status === 'Pending').length, color: '#d97706', bg: '#fffbeb' },
          { label: 'Processed', value: prescriptions.filter(p => p.status === 'Processed').length, color: '#16a34a', bg: '#f0fdf4' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 text-center">
            <p style={{ fontSize: '1.6rem', fontWeight: 700, color: s.color }}>{s.value}</p>
            <p className="text-gray-500 text-xs">{s.label} Prescriptions</p>
          </div>
        ))}
      </div>

      {msg && <div className={`px-4 py-2.5 rounded-lg text-sm border ${msg.startsWith('Insufficient') ? 'bg-red-50 text-red-800 border-red-200' : 'bg-green-50 text-green-800 border-green-200'}`}>{msg}</div>}

      <div className="flex gap-3">
        <div className="relative flex-1"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by patient or doctor..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>
        <select value={filter} onChange={e => setFilter(e.target.value)} className="px-3 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none">
          <option>All</option><option>Pending</option><option>Processed</option>
        </select>
      </div>

      <div className="space-y-4">
        {filtered.map(rx => {
          const isPending = rx.status === 'Pending';
          return (
            <div key={rx.id} className="bg-white rounded-xl shadow-sm border overflow-hidden" style={{ borderColor: isPending ? '#fcd34d' : '#d1fae5' }}>
              <div className="flex items-center justify-between px-5 py-4 border-b" style={{ background: isPending ? '#fffbeb' : '#f0fdf4', borderColor: isPending ? '#fcd34d' : '#d1fae5' }}>
                <div className="flex items-center gap-4">
                  <div>
                    <div className="flex items-center gap-2">
                      <User size={15} style={{ color: '#8A0007' }} />
                      <p className="text-gray-800" style={{ fontWeight: 700 }}>{rx.patientName}</p>
                    </div>
                    <div className="flex items-center gap-4 mt-0.5">
                      <span className="flex items-center gap-1 text-gray-500 text-xs"><User size={11} /> {rx.doctorName}</span>
                      <span className="flex items-center gap-1 text-gray-500 text-xs"><Calendar size={11} /> {rx.date}</span>
                      {rx.diagnosis && <span className="text-xs px-2 py-0.5 rounded" style={{ background: '#fff0f0', color: '#8A0007' }}>{rx.diagnosis}</span>}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-full" style={{ background: isPending ? '#fef3c7' : '#d1fae5' }}>
                    {isPending ? <Clock size={14} style={{ color: '#d97706' }} /> : <CheckCircle size={14} style={{ color: '#16a34a' }} />}
                    <span className="text-xs" style={{ fontWeight: 600, color: isPending ? '#92400e' : '#065f46' }}>{rx.status}</span>
                  </div>
                  {isPending && (
                    <button onClick={() => handleProcess(rx.id)} disabled={processing === rx.id}
                      className="flex items-center gap-2 px-4 py-1.5 rounded-lg text-white text-sm transition-all"
                      style={{ background: processing === rx.id ? '#c97b80' : '#8A0007', fontWeight: 600, cursor: processing === rx.id ? 'not-allowed' : 'pointer' }}>
                      {processing === rx.id ? <><Package size={14} className="animate-spin" /> Processing...</> : <><Package size={14} /> Process</>}
                    </button>
                  )}
                </div>
              </div>

              <div className="p-5">
                <div className="space-y-2">
                  {rx.items.map((item, i) => {
                    const inv = inventory.find(inv => inv.id === item.inventoryItemId);
                    const insufficient = inv ? inv.quantity < item.quantity : true;
                    return (
                      <div key={i} className="flex items-start justify-between p-3 rounded-lg border border-gray-100 bg-gray-50">
                        <div className="flex items-start gap-3">
                          <Pill size={15} style={{ color: '#8A0007', marginTop: 2 }} />
                          <div>
                            <p className="text-gray-800 text-sm" style={{ fontWeight: 600 }}>{item.medicationName}</p>
                            <p className="text-gray-500 text-xs">{item.dosage} — {item.instructions}</p>
                          </div>
                        </div>
                        <div className="text-right flex-shrink-0 ml-4">
                          <p className="text-gray-800 text-sm" style={{ fontWeight: 600 }}>Qty: {item.quantity}</p>
                          {inv && (
                            <p className="text-xs" style={{ color: insufficient ? '#991b1b' : '#6b7280' }}>
                              Stock: {inv.quantity} {inv.unit} {insufficient && isPending ? '⚠ Low' : ''}
                            </p>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
                {rx.notes && <p className="mt-3 text-gray-500 text-xs border-t border-gray-100 pt-3">📝 {rx.notes}</p>}
              </div>
            </div>
          );
        })}
        {filtered.length === 0 && (
          <div className="text-center py-12 text-gray-400">
            <Pill size={40} className="mx-auto mb-3 opacity-30" />
            <p className="text-sm">No prescriptions found.</p>
          </div>
        )}
      </div>
    </div>
  );
}
