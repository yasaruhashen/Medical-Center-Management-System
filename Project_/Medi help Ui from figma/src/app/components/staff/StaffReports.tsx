import { useApp } from '../../context/AppContext';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Download, Users, Package, Calendar, Pill } from 'lucide-react';

export function StaffReports() {
  const { patients, appointments, inventory, prescriptions } = useApp();
  const COLORS = ['#8A0007', '#F7AA37', '#4E0205', '#c97b80', '#f5c842', '#6b7280'];

  const genderData = [
    { name: 'Male', value: patients.filter(p => p.gender === 'Male').length },
    { name: 'Female', value: patients.filter(p => p.gender === 'Female').length },
  ].filter(d => d.value > 0);

  const apptData = [
    { name: 'Scheduled', value: appointments.filter(a => a.status === 'Scheduled').length },
    { name: 'Completed', value: appointments.filter(a => a.status === 'Completed').length },
    { name: 'Cancelled', value: appointments.filter(a => a.status === 'Cancelled').length },
  ];

  const invCatData = inventory.reduce((acc: Record<string, number>, i) => {
    acc[i.category] = (acc[i.category] || 0) + i.quantity;
    return acc;
  }, {});
  const inventoryData = Object.entries(invCatData).map(([name, value]) => ({ name, value }));

  const rxData = [
    { name: 'Pending', value: prescriptions.filter(p => p.status === 'Pending').length },
    { name: 'Processed', value: prescriptions.filter(p => p.status === 'Processed').length },
  ];

  const downloadCSV = (title: string, rows: string[][], headers: string[]) => {
    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${title}_${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
  };

  return (
    <div className="p-6 space-y-6">
      <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Staff Reports</h2><p className="text-gray-500 text-sm">View analytics and download reports for patients, inventory, appointments, and prescriptions.</p></div>

      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Patient Report', icon: Users, action: () => downloadCSV('patients', patients.map(p => [p.name, p.studentId || '', p.gender, p.phone, p.bloodGroup || '', p.registeredDate]), ['Name', 'Student ID', 'Gender', 'Phone', 'Blood Group', 'Registered']) },
          { label: 'Appointment Report', icon: Calendar, action: () => downloadCSV('appointments', appointments.map(a => [a.patientName, a.doctorName, a.date, a.time, a.status, a.reason || '']), ['Patient', 'Doctor', 'Date', 'Time', 'Status', 'Reason']) },
          { label: 'Inventory Report', icon: Package, action: () => downloadCSV('inventory', inventory.map(i => [i.name, i.category, String(i.quantity), i.unit, i.expiryDate]), ['Name', 'Category', 'Quantity', 'Unit', 'Expiry']) },
          { label: 'Prescription Report', icon: Pill, action: () => downloadCSV('prescriptions', prescriptions.map(p => [p.patientName, p.doctorName, p.date, p.diagnosis || '', p.status]), ['Patient', 'Doctor', 'Date', 'Diagnosis', 'Status']) },
        ].map(btn => {
          const Icon = btn.icon;
          return (
            <button key={btn.label} onClick={btn.action} className="flex items-center gap-3 p-4 bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-all">
              <div className="w-9 h-9 rounded-lg flex items-center justify-center" style={{ background: '#fff0f0' }}><Icon size={18} style={{ color: '#8A0007' }} /></div>
              <div className="text-left flex-1"><p className="text-gray-700 text-sm" style={{ fontWeight: 600 }}>{btn.label}</p><p className="text-gray-400 text-xs">Download CSV</p></div>
              <Download size={14} className="text-gray-400" />
            </button>
          );
        })}
      </div>

      <div className="grid grid-cols-2 gap-6">
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Patients by Gender</h3>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="55%" height={160}>
              <PieChart><Pie data={genderData} cx="50%" cy="50%" outerRadius={65} dataKey="value">{genderData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}</Pie><Tooltip /></PieChart>
            </ResponsiveContainer>
            <div className="space-y-2">{genderData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><strong>{d.value}</strong></div>)}</div>
          </div>
        </div>

        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Prescriptions by Status</h3>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="55%" height={160}>
              <PieChart><Pie data={rxData} cx="50%" cy="50%" outerRadius={65} dataKey="value">{rxData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}</Pie><Tooltip /></PieChart>
            </ResponsiveContainer>
            <div className="space-y-2">{rxData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><strong>{d.value}</strong></div>)}</div>
          </div>
        </div>

        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Appointments by Status</h3>
          <ResponsiveContainer width="100%" height={160}>
            <BarChart data={apptData}><CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" /><XAxis dataKey="name" tick={{ fontSize: 12 }} /><YAxis tick={{ fontSize: 12 }} /><Tooltip /><Bar dataKey="value" radius={[4, 4, 0, 0]}>{apptData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}</Bar></BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Inventory by Category</h3>
          <ResponsiveContainer width="100%" height={160}>
            <BarChart data={inventoryData}><CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" /><XAxis dataKey="name" tick={{ fontSize: 11 }} /><YAxis tick={{ fontSize: 12 }} /><Tooltip /><Bar dataKey="value" fill="#8A0007" radius={[4, 4, 0, 0]} /></BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Low stock table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="px-5 py-4 border-b" style={{ background: '#4E0205' }}><h3 className="text-white" style={{ fontWeight: 600 }}>Low Stock Alert</h3></div>
        <table className="w-full">
          <thead><tr className="bg-gray-50">{['Item', 'Category', 'Current Qty', 'Min Stock', 'Status'].map(h => <th key={h} className="px-4 py-2 text-left text-xs text-gray-500">{h}</th>)}</tr></thead>
          <tbody>
            {inventory.filter(i => i.quantity <= i.minStock).map((i, idx) => (
              <tr key={i.id} className={idx % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                <td className="px-4 py-2 text-sm text-gray-800">{i.name}</td>
                <td className="px-4 py-2 text-sm text-gray-600">{i.category}</td>
                <td className="px-4 py-2 text-sm" style={{ color: '#8A0007', fontWeight: 600 }}>{i.quantity} {i.unit}</td>
                <td className="px-4 py-2 text-sm text-gray-600">{i.minStock}</td>
                <td className="px-4 py-2"><span className="px-2 py-0.5 rounded-full text-xs" style={{ background: i.quantity === 0 ? '#fef2f2' : '#fffbeb', color: i.quantity === 0 ? '#991b1b' : '#92400e', fontWeight: 600 }}>{i.quantity === 0 ? 'Out of Stock' : 'Low Stock'}</span></td>
              </tr>
            ))}
            {inventory.filter(i => i.quantity <= i.minStock).length === 0 && <tr><td colSpan={5} className="text-center py-6 text-gray-400 text-sm">All items are adequately stocked.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}
