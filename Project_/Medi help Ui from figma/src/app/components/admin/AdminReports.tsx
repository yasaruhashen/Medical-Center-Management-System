import { useApp } from '../../context/AppContext';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Download, Users, Package, Calendar, FileText } from 'lucide-react';

export function AdminReports() {
  const { users, patients, appointments, inventory, prescriptions } = useApp();

  const COLORS = ['#8A0007', '#F7AA37', '#4E0205', '#c97b80', '#f5c842', '#6b7280'];

  const roleData = [
    { name: 'Admin', value: users.filter(u => u.role === 'admin').length },
    { name: 'Doctor', value: users.filter(u => u.role === 'doctor').length },
    { name: 'Staff', value: users.filter(u => u.role === 'staff').length },
  ];

  const genderData = [
    { name: 'Male', value: patients.filter(p => p.gender === 'Male').length },
    { name: 'Female', value: patients.filter(p => p.gender === 'Female').length },
    { name: 'Other', value: patients.filter(p => p.gender === 'Other').length },
  ].filter(d => d.value > 0);

  const apptStatusData = [
    { name: 'Scheduled', value: appointments.filter(a => a.status === 'Scheduled').length },
    { name: 'Completed', value: appointments.filter(a => a.status === 'Completed').length },
    { name: 'Cancelled', value: appointments.filter(a => a.status === 'Cancelled').length },
  ];

  const invCategoryData = inventory.reduce((acc: Record<string, number>, i) => {
    acc[i.category] = (acc[i.category] || 0) + i.quantity;
    return acc;
  }, {});
  const inventoryData = Object.entries(invCategoryData).map(([name, value]) => ({ name, value }));

  const lowStock = inventory.filter(i => i.quantity <= i.minStock);

  const downloadReport = (title: string, rows: string[][], headers: string[]) => {
    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${title}_${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>System Reports</h2>
        <p className="text-gray-500 text-sm">Comprehensive analytics and downloadable reports for all system data.</p>
      </div>

      {/* Quick download buttons */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Staff Report', icon: Users, color: '#8A0007', action: () => downloadReport('staff_report', users.map(u => [u.name, u.username, u.role, u.email || '']), ['Name', 'Username', 'Role', 'Email']) },
          { label: 'Patient Report', icon: Users, color: '#4E0205', action: () => downloadReport('patient_report', patients.map(p => [p.name, p.studentId || '', p.gender, p.phone, p.bloodGroup || '', p.registeredDate]), ['Name', 'Student ID', 'Gender', 'Phone', 'Blood Group', 'Registered']) },
          { label: 'Inventory Report', icon: Package, color: '#b45309', action: () => downloadReport('inventory_report', inventory.map(i => [i.name, i.category, String(i.quantity), i.unit, i.expiryDate, String(i.minStock)]), ['Name', 'Category', 'Quantity', 'Unit', 'Expiry', 'Min Stock']) },
          { label: 'Appointment Report', icon: Calendar, color: '#15803d', action: () => downloadReport('appointment_report', appointments.map(a => [a.patientName, a.doctorName, a.date, a.time, a.status, a.reason || '']), ['Patient', 'Doctor', 'Date', 'Time', 'Status', 'Reason']) },
        ].map(btn => {
          const Icon = btn.icon;
          return (
            <button key={btn.label} onClick={btn.action} className="flex items-center gap-3 p-4 bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-all">
              <div className="w-9 h-9 rounded-lg flex items-center justify-center" style={{ background: btn.color + '20' }}>
                <Icon size={18} style={{ color: btn.color }} />
              </div>
              <div className="text-left flex-1">
                <p className="text-gray-700 text-sm" style={{ fontWeight: 600 }}>{btn.label}</p>
                <p className="text-gray-400 text-xs">Download CSV</p>
              </div>
              <Download size={14} className="text-gray-400" />
            </button>
          );
        })}
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* User by role */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Users by Role</h3>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="55%" height={180}>
              <PieChart><Pie data={roleData} cx="50%" cy="50%" outerRadius={70} dataKey="value">
                {roleData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}
              </Pie><Tooltip /></PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 flex-1">
              {roleData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><span style={{ fontWeight: 700, color: COLORS[i] }}>{d.value}</span></div>)}
            </div>
          </div>
        </div>

        {/* Patients by gender */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Patients by Gender</h3>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="55%" height={180}>
              <PieChart><Pie data={genderData} cx="50%" cy="50%" outerRadius={70} dataKey="value">
                {genderData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}
              </Pie><Tooltip /></PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 flex-1">
              {genderData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><span style={{ fontWeight: 700, color: COLORS[i] }}>{d.value}</span></div>)}
            </div>
          </div>
        </div>

        {/* Appointments by status */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Appointments by Status</h3>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={apptStatusData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                {apptStatusData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Inventory by category */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Inventory by Category</h3>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={inventoryData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" fill="#8A0007" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Low stock table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between" style={{ background: '#4E0205' }}>
          <h3 className="text-white" style={{ fontWeight: 600 }}>Low Stock Items Report</h3>
          <span className="text-red-200 text-sm">{lowStock.length} items need restocking</span>
        </div>
        {lowStock.length === 0 ? (
          <div className="text-center py-8 text-gray-400 text-sm">All items are adequately stocked.</div>
        ) : (
          <table className="w-full">
            <thead><tr className="bg-gray-50"><th className="px-4 py-2 text-left text-xs text-gray-500">Item</th><th className="px-4 py-2 text-left text-xs text-gray-500">Category</th><th className="px-4 py-2 text-left text-xs text-gray-500">Current Qty</th><th className="px-4 py-2 text-left text-xs text-gray-500">Min Stock</th><th className="px-4 py-2 text-left text-xs text-gray-500">Status</th></tr></thead>
            <tbody>
              {lowStock.map((i, idx) => (
                <tr key={i.id} className={idx % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                  <td className="px-4 py-2 text-sm text-gray-800">{i.name}</td>
                  <td className="px-4 py-2 text-sm text-gray-600">{i.category}</td>
                  <td className="px-4 py-2 text-sm" style={{ color: '#8A0007', fontWeight: 600 }}>{i.quantity} {i.unit}</td>
                  <td className="px-4 py-2 text-sm text-gray-600">{i.minStock} {i.unit}</td>
                  <td className="px-4 py-2"><span className="px-2 py-0.5 rounded-full text-xs" style={{ background: i.quantity === 0 ? '#fef2f2' : '#fffbeb', color: i.quantity === 0 ? '#991b1b' : '#92400e', fontWeight: 600 }}>{i.quantity === 0 ? 'Out of Stock' : 'Low Stock'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
