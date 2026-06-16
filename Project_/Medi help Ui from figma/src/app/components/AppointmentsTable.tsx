import { MoreVertical, CheckCircle2, Clock, AlertCircle } from 'lucide-react';

interface Appointment {
  id: string;
  patientName: string;
  doctor: string;
  department: string;
  time: string;
  status: 'confirmed' | 'pending' | 'cancelled';
  type: string;
}

const appointments: Appointment[] = [
  { id: 'APT-001', patientName: 'Priya Sharma', doctor: 'Dr. Anil Patel', department: 'Cardiology', time: '09:00 AM', status: 'confirmed', type: 'Follow-up' },
  { id: 'APT-002', patientName: 'Rahul Verma', doctor: 'Dr. Meera Singh', department: 'Orthopedics', time: '10:30 AM', status: 'pending', type: 'Consultation' },
  { id: 'APT-003', patientName: 'Anjali Reddy', doctor: 'Dr. Suresh Kumar', department: 'Pediatrics', time: '11:15 AM', status: 'confirmed', type: 'Check-up' },
  { id: 'APT-004', patientName: 'Vikram Rao', doctor: 'Dr. Lakshmi Devi', department: 'Neurology', time: '02:00 PM', status: 'pending', type: 'New Patient' },
  { id: 'APT-005', patientName: 'Kavita Nair', doctor: 'Dr. Rajesh Iyer', department: 'Dermatology', time: '03:30 PM', status: 'cancelled', type: 'Treatment' },
];

export function AppointmentsTable() {
  const getStatusBadge = (status: Appointment['status']) => {
    switch (status) {
      case 'confirmed':
        return (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-green-50 text-green-700 rounded-full text-xs font-medium">
            <CheckCircle2 className="w-3 h-3" />
            Confirmed
          </span>
        );
      case 'pending':
        return (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-accent/20 text-accent-foreground rounded-full text-xs font-medium">
            <Clock className="w-3 h-3" />
            Pending
          </span>
        );
      case 'cancelled':
        return (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-red-50 text-red-700 rounded-full text-xs font-medium">
            <AlertCircle className="w-3 h-3" />
            Cancelled
          </span>
        );
    }
  };

  return (
    <div className="bg-card border border-border rounded-xl overflow-hidden">
      <div className="p-6 border-b border-border">
        <h2 className="font-semibold text-foreground">Today's Appointments</h2>
        <p className="text-sm text-muted-foreground mt-1">Manage and track patient appointments</p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-muted/50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Patient ID</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Patient Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Doctor</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Department</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Time</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {appointments.map((appointment) => (
              <tr key={appointment.id} className="hover:bg-muted/20 transition-colors">
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                  {appointment.id}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-foreground">
                  {appointment.patientName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-foreground">
                  {appointment.doctor}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-muted-foreground">
                  {appointment.department}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-foreground font-medium">
                  {appointment.time}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-muted-foreground">
                  {appointment.type}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {getStatusBadge(appointment.status)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <button className="p-2 hover:bg-muted rounded-lg transition-colors">
                    <MoreVertical className="w-4 h-4 text-muted-foreground" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="p-4 border-t border-border flex items-center justify-between">
        <p className="text-sm text-muted-foreground">Showing 5 of 28 appointments</p>
        <button className="px-4 py-2 text-sm font-medium text-primary hover:bg-primary/5 rounded-lg transition-colors">
          View All Appointments
        </button>
      </div>
    </div>
  );
}
