import { useState, useRef } from 'react';
import { useApp } from '../../context/AppContext';
import { Download, Upload, CheckCircle, AlertCircle, Database, Shield, RefreshCw } from 'lucide-react';

export function BackupRestore() {
  const { exportBackup, importBackup, users, patients, appointments, inventory, prescriptions } = useApp();
  const [importing, setImporting] = useState(false);
  const [msg, setMsg] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const handleExport = () => {
    exportBackup();
    setMsg({ text: 'Backup exported successfully! Check your downloads folder.', type: 'success' });
    setTimeout(() => setMsg(null), 4000);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setImporting(true);
    const reader = new FileReader();
    reader.onload = ev => {
      const text = ev.target?.result as string;
      const ok = importBackup(text);
      setMsg(ok ? { text: 'Backup restored successfully! All data has been loaded.', type: 'success' } : { text: 'Failed to restore backup. Please ensure the file is a valid backup.', type: 'error' });
      setImporting(false);
      setTimeout(() => setMsg(null), 4000);
    };
    reader.readAsText(file);
    e.target.value = '';
  };

  const dataSummary = [
    { label: 'Users', count: users.length, icon: '👥' },
    { label: 'Patients', count: patients.length, icon: '🏥' },
    { label: 'Appointments', count: appointments.length, icon: '📅' },
    { label: 'Inventory Items', count: inventory.length, icon: '💊' },
    { label: 'Prescriptions', count: prescriptions.length, icon: '📋' },
  ];

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>Backup & Restore</h2>
        <p className="text-gray-500 text-sm">Export a full system backup or restore from a previous backup file.</p>
      </div>

      {msg && (
        <div className={`flex items-center gap-3 px-4 py-3 rounded-xl border ${msg.type === 'success' ? 'bg-green-50 border-green-200 text-green-800' : 'bg-red-50 border-red-200 text-red-800'}`}>
          {msg.type === 'success' ? <CheckCircle size={18} /> : <AlertCircle size={18} />}
          <span className="text-sm">{msg.text}</span>
        </div>
      )}

      {/* Current data summary */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
        <div className="flex items-center gap-2 mb-4">
          <Database size={18} style={{ color: '#8A0007' }} />
          <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Current Data Summary</h3>
        </div>
        <div className="grid grid-cols-5 gap-3">
          {dataSummary.map(d => (
            <div key={d.label} className="text-center p-3 rounded-lg" style={{ background: '#fff5f5' }}>
              <div className="text-2xl mb-1">{d.icon}</div>
              <p style={{ fontSize: '1.4rem', fontWeight: 700, color: '#8A0007' }}>{d.count}</p>
              <p className="text-gray-500 text-xs">{d.label}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Export */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl flex items-center justify-center" style={{ background: '#f0fdf4' }}>
              <Download size={20} style={{ color: '#16a34a' }} />
            </div>
            <div>
              <h3 className="text-gray-800" style={{ fontWeight: 600 }}>Export Backup</h3>
              <p className="text-gray-500 text-xs">Download all system data as JSON</p>
            </div>
          </div>
          <p className="text-gray-500 text-sm">Creates a complete snapshot of all users, patients, appointments, inventory, and prescriptions in a JSON file that can be safely stored.</p>
          <ul className="space-y-1 text-sm text-gray-600">
            <li className="flex items-center gap-2"><CheckCircle size={14} className="text-green-500" /> All user accounts</li>
            <li className="flex items-center gap-2"><CheckCircle size={14} className="text-green-500" /> Patient records</li>
            <li className="flex items-center gap-2"><CheckCircle size={14} className="text-green-500" /> Appointments & prescriptions</li>
            <li className="flex items-center gap-2"><CheckCircle size={14} className="text-green-500" /> Inventory data</li>
          </ul>
          <button onClick={handleExport} className="w-full py-3 rounded-xl text-white flex items-center justify-center gap-2" style={{ background: '#16a34a', fontWeight: 600 }}>
            <Download size={18} /> Export Backup
          </button>
        </div>

        {/* Import */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl flex items-center justify-center" style={{ background: '#fff0f0' }}>
              <Upload size={20} style={{ color: '#8A0007' }} />
            </div>
            <div>
              <h3 className="text-gray-800" style={{ fontWeight: 600 }}>Restore Backup</h3>
              <p className="text-gray-500 text-xs">Upload a backup file to restore data</p>
            </div>
          </div>
          <p className="text-gray-500 text-sm">Select a previously exported backup file to restore all system data. Current data will be replaced with the backup.</p>
          <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-amber-600 flex-shrink-0 mt-0.5" />
            <p className="text-amber-700 text-xs">Warning: Restoring a backup will replace all current data. This cannot be undone. Export a current backup first.</p>
          </div>
          <input type="file" accept=".json" ref={fileRef} onChange={handleFileChange} className="hidden" />
          <button onClick={() => fileRef.current?.click()} disabled={importing}
            className="w-full py-3 rounded-xl text-white flex items-center justify-center gap-2"
            style={{ background: importing ? '#c97b80' : '#8A0007', fontWeight: 600, cursor: importing ? 'not-allowed' : 'pointer' }}>
            {importing ? <><RefreshCw size={18} className="animate-spin" /> Restoring...</> : <><Upload size={18} /> Choose Backup File</>}
          </button>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
        <div className="flex items-center gap-2 mb-3">
          <Shield size={18} style={{ color: '#8A0007' }} />
          <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Backup Recommendations</h3>
        </div>
        <div className="grid grid-cols-3 gap-4 text-sm text-gray-600">
          <div className="flex items-start gap-2"><span className="text-blue-500 mt-0.5">•</span> Export a backup at least once per week</div>
          <div className="flex items-start gap-2"><span className="text-blue-500 mt-0.5">•</span> Store backups in a secure off-site location</div>
          <div className="flex items-start gap-2"><span className="text-blue-500 mt-0.5">•</span> Test restore procedures periodically</div>
        </div>
      </div>
    </div>
  );
}
