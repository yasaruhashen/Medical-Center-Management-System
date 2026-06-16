import { useApp } from '../../context/AppContext';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { Users, Calendar, FileText, Clock, CheckCircle } from 'lucide-react';

export function DoctorDashboard() {
  const { currentUser, appointments, prescriptions, patients } = useApp();
  const today = new Date().toISOString().split('T')[0];

  const myAppts = appointments.filter(a => a.doctorId === currentUser?.id);
  const todayAppts = myAppts.filter(a => a.date === today);
  const scheduledToday = todayAppts.filter(a => a.status === 'Scheduled');
  const myRx = prescriptions.filter(p => p.doctorId === currentUser?.id);

  const weekDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  const apptWeekData = weekDays.map((day, i) => {
    const d = new Date();
    const dayOfWeek = d.getDay();
    const diff = i - (dayOfWeek === 0 ? 6 : dayOfWeek - 1);
    const targetDate = new Date(d.setDate(d.getDate() + diff)).toISOString().split('T')[0];
    return { day, count: myAppts.filter(a => a.date === targetDate).length };
  });

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-gray-800" style={{ fontWeight: 700 }}>Welcome, {currentUser?.name}</h2>
        <p className="text-gray-500 text-sm">{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
      </div>

      <div className="grid grid-cols-4 gap-4">
        {[
          { label: "Today's Patients", value: todayAppts.length, icon: Calendar, color: '#8A0007', bg: '#fff0f0' },
          { label: 'Pending Consultations', value: scheduledToday.length, icon: Clock, color: '#d97706', bg: '#fffbeb' },
          { label: 'Prescriptions Issued', value: myRx.length, icon: FileText, color: '#4E0205', bg: '#fdf0f0' },
          { label: 'Total Appointments', value: myAppts.length, icon: Users, color: '#15803d', bg: '#f0fdf4' },
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
        {/* Today's schedule */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>Today's Schedule</h3>
          {scheduledToday.length === 0 ? (
            <div className="text-center py-8">
              <CheckCircle size={36} className="mx-auto mb-2 text-green-400" />
              <p className="text-gray-400 text-sm">No more appointments today!</p>
            </div>
          ) : (
            <div className="space-y-2">
              {scheduledToday.sort((a, b) => a.time.localeCompare(b.time)).map(a => {
                const patient = patients.find(p => p.id === a.patientId);
                return (
                  <div key={a.id} className="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:border-red-200 transition-colors">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-xs" style={{ background: '#8A0007', fontWeight: 700 }}>{a.patientName.charAt(0)}</div>
                      <div>
                        <p className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{a.patientName}</p>
                        <p className="text-gray-400 text-xs">{patient?.studentId || ''} {a.reason ? `• ${a.reason}` : ''}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Clock size={13} className="text-gray-400" />
                      <span className="text-gray-600 text-sm" style={{ fontWeight: 600 }}>{a.time}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Weekly appointments */}
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="text-gray-700 mb-4" style={{ fontWeight: 600 }}>This Week's Appointments</h3>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={apptWeekData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0e6e6" />
              <XAxis dataKey="day" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="count" fill="#8A0007" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Recent prescriptions */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-gray-100" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)' }}>
          <h3 className="text-white" style={{ fontWeight: 600 }}>Recent Prescriptions</h3>
        </div>
        {myRx.length === 0 ? (
          <div className="text-center py-8 text-gray-400 text-sm">No prescriptions issued yet.</div>
        ) : (
          <table className="w-full">
            <thead><tr className="bg-gray-50">{['Patient', 'Date', 'Diagnosis', 'Items', 'Status'].map(h => <th key={h} className="px-4 py-2 text-left text-xs text-gray-500">{h}</th>)}</tr></thead>
            <tbody>
              {myRx.slice(0, 5).map((rx, i) => (
                <tr key={rx.id} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                  <td className="px-4 py-3 text-sm text-gray-800" style={{ fontWeight: 500 }}>{rx.patientName}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{rx.date}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{rx.diagnosis || '—'}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{rx.items.length}</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 rounded-full text-xs" style={{ background: rx.status === 'Pending' ? '#fffbeb' : '#f0fdf4', color: rx.status === 'Pending' ? '#92400e' : '#065f46', fontWeight: 600 }}>{rx.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
