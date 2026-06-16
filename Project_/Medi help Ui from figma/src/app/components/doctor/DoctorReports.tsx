import { useApp } from '../../context/AppContext';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Download, FileText, Users, Calendar } from 'lucide-react';

export function DoctorReports() {
  const { currentUser, appointments, prescriptions, patients } = useApp();
  const COLORS = ['#8A0007', '#F7AA37', '#4E0205', '#c97b80'];

  const myAppts = appointments.filter(a => a.doctorId === currentUser?.id);
  const myRx = prescriptions.filter(p => p.doctorId === currentUser?.id);

  const apptStatusData = [
    { name: 'Scheduled', value: myAppts.filter(a => a.status === 'Scheduled').length },
    { name: 'Completed', value: myAppts.filter(a => a.status === 'Completed').length },
    { name: 'Cancelled', value: myAppts.filter(a => a.status === 'Cancelled').length },
  ].filter(d => d.value > 0);

  const rxStatusData = [
    { name: 'Pending', value: myRx.filter(p => p.status === 'Pending').length },
    { name: 'Processed', value: myRx.filter(p => p.status === 'Processed').length },
  ].filter(d => d.value > 0);

  const patientFreq = myAppts.reduce((acc: Record<string, number>, a) => {
    acc[a.patientName] = (acc[a.patientName] || 0) + 1;
    return acc;
  }, {});
  const topPatients = Object.entries(patientFreq).sort((a, b) => b[1] - a[1]).slice(0, 6).map(([name, count]) => ({ name, count }));

  const downloadCSV = (title: string, rows: string[][], headers: string[]) => {
    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
    a.download = `${title}_${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
  };

  return (
    <div className="p-6 space-y-6">
      <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>My Reports</h2><p className="text-gray-500 text-sm">View your appointment and prescription analytics.</p></div>

      {/* Summary cards */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'My Total Appointments', value: myAppts.length, color: '#8A0007' },
          { label: 'Completed Consultations', value: myAppts.filter(a => a.status === 'Completed').length, color: '#16a34a' },
          { label: 'Prescriptions Written', value: myRx.length, color: '#4E0205' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 text-center">
            <p style={{ fontSize: '1.8rem', fontWeight: 700, color: s.color }}>{s.value}</p>
            <p className="text-gray-500 text-sm">{s.label}</p>
          </div>
        ))}
      </div>

      {/* Download buttons */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Appointment Report', icon: Calendar, action: () => downloadCSV('my_appointments', myAppts.map(a => [a.patientName, a.date, a.time, a.status, a.reason || '']), ['Patient', 'Date', 'Time', 'Status', 'Reason']) },
          { label: 'Prescription Report', icon: FileText, action: () => downloadCSV('my_prescriptions', myRx.map(p => [p.patientName, p.date, p.diagnosis || '', String(p.items.length), p.status]), ['Patient', 'Date', 'Diagnosis', 'Items', 'Status']) },
          { label: 'Patient List', icon: Users, action: () => { const myPatientIds = [...new Set(myAppts.map(a => a.patientId))]; const myPatients = patients.filter(p => myPatientIds.includes(p.id)); downloadCSV('my_patients', myPatients.map(p => [p.name, p.studentId || '', p.gender, p.phone, p.bloodGroup || '']), ['Name', 'Student ID', 'Gender', 'Phone', 'Blood Group']); } },
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
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Appointments by Status</h3>
          {apptStatusData.length === 0 ? <div className="text-center text-gray-400 text-sm py-8">No appointment data yet.</div> : (
            <div className="flex items-center gap-4">
              <ResponsiveContainer width="55%" height={160}>
                <PieChart><Pie data={apptStatusData} cx="50%" cy="50%" outerRadius={65} dataKey="value">{apptStatusData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}</Pie><Tooltip /></PieChart>
              </ResponsiveContainer>
              <div className="space-y-2">{apptStatusData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><strong>{d.value}</strong></div>)}</div>
            </div>
          )}
        </div>

        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Prescriptions by Status</h3>
          {rxStatusData.length === 0 ? <div className="text-center text-gray-400 text-sm py-8">No prescription data yet.</div> : (
            <div className="flex items-center gap-4">
              <ResponsiveContainer width="55%" height={160}>
                <PieChart><Pie data={rxStatusData} cx="50%" cy="50%" outerRadius={65} dataKey="value">{rxStatusData.map((_, i) => <Cell key={i} fill={COLORS[i]} />)}</Pie><Tooltip /></PieChart>
              </ResponsiveContainer>
              <div className="space-y-2">{rxStatusData.map((d, i) => <div key={d.name} className="flex items-center justify-between text-sm"><div className="flex items-center gap-2"><div className="w-3 h-3 rounded-full" style={{ background: COLORS[i] }} /><span className="text-gray-600">{d.name}</span></div><strong>{d.value}</strong></div>)}</div>
            </div>
          )}
        </div>

        {topPatients.length > 0 && (
          <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100 col-span-2">
            <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Most Frequent Patients</h3>
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={topPatients}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="count" fill="#8A0007" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </div>
  );
}
