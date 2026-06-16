import { useApp } from '../../context/AppContext';
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Users, UserCheck, Package, FileText, TrendingUp, AlertTriangle } from 'lucide-react';

export function AdminDashboard() {
  const { users, patients, appointments, inventory, prescriptions } = useApp();

  const totalPatients = patients.length;
  const totalStaff = users.filter(u => u.role === 'staff').length;
  const totalDoctors = users.filter(u => u.role === 'doctor').length;
  const lowStockItems = inventory.filter(i => i.quantity <= i.minStock).length;
  const pendingPrescriptions = prescriptions.filter(p => p.status === 'Pending').length;
  const todayAppts = appointments.filter(a => a.date === new Date().toISOString().split('T')[0]).length;

  const apptByStatus = [
    { name: 'Scheduled', value: appointments.filter(a => a.status === 'Scheduled').length },
    { name: 'Completed', value: appointments.filter(a => a.status === 'Completed').length },
    { name: 'Cancelled', value: appointments.filter(a => a.status === 'Cancelled').length },
  ];

  const inventoryByCategory = inventory.reduce((acc: Record<string, number>, item) => {
    acc[item.category] = (acc[item.category] || 0) + item.quantity;
    return acc;
  }, {});
  const inventoryData = Object.entries(inventoryByCategory).map(([name, value]) => ({ name, value }));

  const monthlyPatients = [
    { month: 'Jan', count: 38 }, { month: 'Feb', count: 45 }, { month: 'Mar', count: 52 },
    { month: 'Apr', count: 41 }, { month: 'May', count: 60 }, { month: 'Jun', count: totalPatients },
  ];

  const COLORS = ['#8A0007', '#F7AA37', '#4E0205', '#c97b80', '#f5c842'];

  const stats = [
    { label: 'Total Patients', value: totalPatients, icon: Users, color: '#8A0007', bg: '#fff0f0' },
    { label: 'Doctors', value: totalDoctors, icon: UserCheck, color: '#4E0205', bg: '#fdf0f0' },
    { label: 'Staff Members', value: totalStaff, icon: Users, color: '#F7AA37', bg: '#fffbf0' },
    { label: "Today's Appointments", value: todayAppts, icon: FileText, color: '#8A0007', bg: '#fff0f0' },
    { label: 'Pending Prescriptions', value: pendingPrescriptions, icon: FileText, color: '#4E0205', bg: '#fdf0f0' },
    { label: 'Low Stock Items', value: lowStockItems, icon: AlertTriangle, color: lowStockItems > 0 ? '#d97706' : '#16a34a', bg: lowStockItems > 0 ? '#fffbeb' : '#f0fdf4' },
  ];

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>Admin Dashboard</h2>
        <p className="text-gray-500 text-sm">MEDI HELP J'PURA — System Overview</p>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-3 gap-4">
        {stats.map(s => {
          const Icon = s.icon;
          return (
            <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 flex items-center gap-4">
              <div className="rounded-xl p-3 flex-shrink-0" style={{ background: s.bg }}>
                <Icon size={22} style={{ color: s.color }} />
              </div>
              <div>
                <p className="text-gray-500 text-xs">{s.label}</p>
                <p style={{ fontSize: '1.5rem', fontWeight: 700, color: s.color }}>{s.value}</p>
              </div>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Monthly patients trend */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <div className="flex items-center gap-2 mb-4">
            <TrendingUp size={18} style={{ color: '#8A0007' }} />
            <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Monthly Patients</h3>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={monthlyPatients}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="month" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Line type="monotone" dataKey="count" stroke="#8A0007" strokeWidth={2} dot={{ fill: '#8A0007', r: 4 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Appointment status */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Appointment Status</h3>
          <div className="flex items-center gap-6">
            <ResponsiveContainer width="60%" height={200}>
              <PieChart>
                <Pie data={apptByStatus} cx="50%" cy="50%" innerRadius={50} outerRadius={80} dataKey="value">
                  {apptByStatus.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2">
              {apptByStatus.map((s, i) => (
                <div key={s.name} className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} />
                  <span className="text-sm text-gray-600">{s.name}: <strong>{s.value}</strong></span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Inventory by category */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100 col-span-2">
          <div className="flex items-center gap-2 mb-4">
            <Package size={18} style={{ color: '#8A0007' }} />
            <h3 className="text-gray-700" style={{ fontWeight: 600 }}>Inventory by Category</h3>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={inventoryData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" fill="#8A0007" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Low stock warning */}
      {lowStockItems > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 flex items-start gap-3">
          <AlertTriangle size={20} className="text-amber-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-amber-800" style={{ fontWeight: 600 }}>Low Stock Alert</p>
            <p className="text-amber-700 text-sm">{lowStockItems} item(s) are at or below minimum stock level. Please restock soon.</p>
            <div className="mt-2 space-y-1">
              {inventory.filter(i => i.quantity <= i.minStock).map(i => (
                <p key={i.id} className="text-xs text-amber-700">• {i.name}: {i.quantity} {i.unit} remaining (min: {i.minStock})</p>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
