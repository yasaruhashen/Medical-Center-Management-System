/* MARKER-MAKE-KIT-INVOKED */
import { useState } from 'react';
import { AppProvider, useApp } from './context/AppContext';
import { Login } from './components/Login';
import { Sidebar } from './components/Sidebar';

// Admin
import { AdminDashboard } from './components/admin/AdminDashboard';
import { UserManagement } from './components/admin/UserManagement';
import { BackupRestore } from './components/admin/BackupRestore';
import { AdminReports } from './components/admin/AdminReports';

// Staff
import { StaffDashboard } from './components/staff/StaffDashboard';
import { PatientRegistration } from './components/staff/PatientRegistration';
import { AppointmentManagement } from './components/staff/AppointmentManagement';
import { InventoryManagement } from './components/staff/InventoryManagement';
import { PrescriptionProcessing } from './components/staff/PrescriptionProcessing';
import { StaffReports } from './components/staff/StaffReports';

// Doctor
import { DoctorDashboard } from './components/doctor/DoctorDashboard';
import { PatientDetails } from './components/doctor/PatientDetails';
import { WritePrescription } from './components/doctor/WritePrescription';
import { DoctorReports } from './components/doctor/DoctorReports';

import {
  LayoutDashboard, Users, Settings, Database, BarChart3,
  Calendar, UserPlus, Package, Pill, FileText,
  Stethoscope, ClipboardList, TrendingUp,
} from 'lucide-react';

const adminNav = [
  { id: 'dashboard', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
  { id: 'users', label: 'User Management', icon: <Users size={18} /> },
  { id: 'backup', label: 'Backup & Restore', icon: <Database size={18} /> },
  { id: 'reports', label: 'Reports', icon: <BarChart3 size={18} /> },
];

const staffNav = [
  { id: 'dashboard', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
  { id: 'patients', label: 'Patient Registration', icon: <UserPlus size={18} /> },
  { id: 'appointments', label: 'Appointments', icon: <Calendar size={18} /> },
  { id: 'inventory', label: 'Inventory', icon: <Package size={18} /> },
  { id: 'prescriptions', label: 'Prescription Processing', icon: <Pill size={18} /> },
  { id: 'reports', label: 'Reports', icon: <BarChart3 size={18} /> },
];

const doctorNav = [
  { id: 'dashboard', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
  { id: 'patients', label: 'Patient Details', icon: <Stethoscope size={18} /> },
  { id: 'prescriptions', label: 'Prescriptions', icon: <ClipboardList size={18} /> },
  { id: 'reports', label: 'My Reports', icon: <TrendingUp size={18} /> },
];

function AppContent() {
  const { currentUser } = useApp();
  const [page, setPage] = useState('dashboard');

  if (!currentUser) return <Login />;

  const role = currentUser.role;

  const nav = role === 'admin' ? adminNav : role === 'staff' ? staffNav : doctorNav;
  const title = "MEDI HELP J'PURA";
  const subtitle = role === 'admin' ? 'Admin Portal' : role === 'staff' ? 'Staff Portal' : 'Doctor Portal';

  const renderPage = () => {
    if (role === 'admin') {
      if (page === 'dashboard') return <AdminDashboard />;
      if (page === 'users') return <UserManagement />;
      if (page === 'backup') return <BackupRestore />;
      if (page === 'reports') return <AdminReports />;
    }
    if (role === 'staff') {
      if (page === 'dashboard') return <StaffDashboard />;
      if (page === 'patients') return <PatientRegistration />;
      if (page === 'appointments') return <AppointmentManagement />;
      if (page === 'inventory') return <InventoryManagement />;
      if (page === 'prescriptions') return <PrescriptionProcessing />;
      if (page === 'reports') return <StaffReports />;
    }
    if (role === 'doctor') {
      if (page === 'dashboard') return <DoctorDashboard />;
      if (page === 'patients') return <PatientDetails />;
      if (page === 'prescriptions') return <WritePrescription />;
      if (page === 'reports') return <DoctorReports />;
    }
    return null;
  };

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      <Sidebar
        navItems={nav}
        currentPage={page}
        onNavigate={p => setPage(p)}
        title={title}
        subtitle={subtitle}
      />
      <main className="flex-1 overflow-y-auto">
        {renderPage()}
      </main>
    </div>
  );
}

export default function App() {
  return (
    <AppProvider>
      <AppContent />
    </AppProvider>
  );
}
