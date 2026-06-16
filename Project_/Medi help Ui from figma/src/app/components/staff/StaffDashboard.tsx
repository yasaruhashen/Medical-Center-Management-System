import { useApp } from '../../context/AppContext';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Users, Calendar, Package, FileText, AlertTriangle, Clock } from 'lucide-react';

export function StaffDashboard() {
  const { patients, appointments, inventory, prescriptions } = useApp();
  const today = new Date().toISOString().split('T')[0];

  const todayAppts = appointments.filter(a => a.date === today);
  const scheduledToday = todayAppts.filter(a => a.status === 'Scheduled').length;
  const pendingRx = prescriptions.filter(p => p.status === 'Pending').length;
  const lowStock = inventory.filter(i => i.quantity <= i.minStock).length;

  const COLORS = ['#8A0007', '#F7AA37', '#4E0205', '#c97b80'];

  const invByCategory = inventory.reduce((acc: Record<string, number>, i) => {
    acc[i.category] = (acc[i.category] || 0) + i.quantity;
    return acc;
  }, {});
  const invData = Object.entries(invByCategory).map(([name, value]) => ({ name, value }));

  const apptByDoctor = appointments.reduce((acc: Record<string, number>, a) => {
    acc[a.doctorName] = (acc[a.doctorName] || 0) + 1;
    return acc;
  }, {});
  const doctorData = Object.entries(apptByDoctor).map(([name, value]) => ({ name, value }));

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>Staff Dashboard</h2>
        <p className="text-gray-500 text-sm">Today: {new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
      </div>

      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total Patients', value: patients.length, icon: Users, color: '#8A0007', bg: '#fff0f0' },
          { label: "Today's Appointments", value: scheduledToday, icon: Calendar, color: '#4E0205', bg: '#fdf0f0' },
          { label: 'Pending Prescriptions', value: pendingRx, icon: FileText, color: '#b45309', bg: '#fffbeb' },
          { label: 'Low Stock Alerts', value: lowStock, icon: AlertTriangle, color: lowStock > 0 ? '#d97706' : '#16a34a', bg: lowStock > 0 ? '#fffbeb' : '#f0fdf4' },
        ].map(s => {
          const Icon = s.icon;
          return (
            <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 flex items-center gap-3">
              <div className="rounded-xl p-3 flex-shrink-0" style={{ background: s.bg }}><Icon size={20} style={{ color: s.color }} /></div>
              <div><p className="text-gray-500 text-xs">{s.label}</p><p style={{ fontSize: '1.4rem', fontWeight: 700, color: s.color }}>{s.value}</p></div>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Today's appointments */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <div className="flex items-center gap-2 mb-4"><Clock size={18} style={{ color: '#8A0007' }} /><h3 className="text-gray-700" style={{ fontWeight: 600 }}>Today's Appointments</h3></div>
          {todayAppts.length === 0 ? (
            <p className="text-gray-400 text-sm text-center py-6">No appointments scheduled for today.</p>
          ) : (
            <div className="space-y-2">
              {todayAppts.map(a => (
                <div key={a.id} className="flex items-center justify-between p-3 rounded-lg border border-gray-100">
                  <div><p className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{a.patientName}</p><p className="text-gray-500 text-xs">{a.doctorName} • {a.time}</p></div>
                  <span className="px-2 py-0.5 rounded-full text-xs" style={{ background: a.status === 'Scheduled' ? '#eff6ff' : a.status === 'Completed' ? '#f0fdf4' : '#fef2f2', color: a.status === 'Scheduled' ? '#1d4ed8' : a.status === 'Completed' ? '#16a34a' : '#991b1b', fontWeight: 600 }}>{a.status}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Inventory by category */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <div className="flex items-center gap-2 mb-4"><Package size={18} style={{ color: '#8A0007' }} /><h3 className="text-gray-700" style={{ fontWeight: 600 }}>Inventory Overview</h3></div>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="55%" height={160}>
              <PieChart><Pie data={invData} cx="50%" cy="50%" outerRadius={65} dataKey="value">
                {invData.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
              </Pie><Tooltip /></PieChart>
            </ResponsiveContainer>
            <div className="space-y-1.5 flex-1">
              {invData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-xs"><div className="flex items-center gap-1.5"><div className="w-2.5 h-2.5 rounded-full" style={{ background: COLORS[i % COLORS.length] }} /><span className="text-gray-600">{d.name}</span></div><span style={{ fontWeight: 700 }}>{d.value}</span></div>)}
            </div>
          </div>
        </div>

        {/* Appointments by doctor */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100 col-span-2">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Appointments by Doctor</h3>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={doctorData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" fill="#8A0007" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Pending prescriptions */}
      {pendingRx > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-4">
          <div className="flex items-center gap-2 mb-2"><AlertTriangle size={18} className="text-amber-600" /><p className="text-amber-800" style={{ fontWeight: 600 }}>Pending Prescriptions ({pendingRx})</p></div>
          <p className="text-amber-700 text-sm">There are {pendingRx} prescription(s) waiting to be processed. Go to Prescription Processing to handle them.</p>
        </div>
      )}
    </div>
  );
}
