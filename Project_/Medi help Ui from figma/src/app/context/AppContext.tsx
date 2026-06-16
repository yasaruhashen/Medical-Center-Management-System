import { createContext, useContext, useState, useEffect, ReactNode } from 'react';

export type UserRole = 'admin' | 'staff' | 'doctor';

export interface AppUser {
  id: string;
  username: string;
  password: string;
  role: UserRole;
  name: string;
  email?: string;
}

export interface Patient {
  id: string;
  name: string;
  dob: string;
  gender: 'Male' | 'Female' | 'Other';
  phone: string;
  address: string;
  studentId?: string;
  registeredDate: string;
  bloodGroup?: string;
  allergies?: string;
}

export interface Appointment {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  date: string;
  time: string;
  status: 'Scheduled' | 'Completed' | 'Cancelled';
  reason?: string;
}

export interface InventoryItem {
  id: string;
  name: string;
  category: string;
  quantity: number;
  unit: string;
  expiryDate: string;
  minStock: number;
  supplier?: string;
}

export interface PrescriptionItem {
  inventoryItemId: string;
  medicationName: string;
  dosage: string;
  quantity: number;
  instructions: string;
}

export interface Prescription {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  date: string;
  items: PrescriptionItem[];
  status: 'Pending' | 'Processed';
  diagnosis?: string;
  notes?: string;
}

interface AppContextType {
  currentUser: AppUser | null;
  users: AppUser[];
  patients: Patient[];
  appointments: Appointment[];
  inventory: InventoryItem[];
  prescriptions: Prescription[];
  login: (username: string, password: string) => boolean;
  logout: () => void;
  addUser: (user: Omit<AppUser, 'id'>) => void;
  updateUser: (id: string, data: Partial<AppUser>) => void;
  removeUser: (id: string) => void;
  addPatient: (patient: Omit<Patient, 'id' | 'registeredDate'>) => void;
  updatePatient: (id: string, data: Partial<Patient>) => void;
  removePatient: (id: string) => void;
  addAppointment: (appt: Omit<Appointment, 'id'>) => void;
  updateAppointment: (id: string, data: Partial<Appointment>) => void;
  removeAppointment: (id: string) => void;
  addInventoryItem: (item: Omit<InventoryItem, 'id'>) => void;
  updateInventoryItem: (id: string, data: Partial<InventoryItem>) => void;
  removeInventoryItem: (id: string) => void;
  addPrescription: (rx: Omit<Prescription, 'id'>) => void;
  processPrescription: (id: string) => void;
  exportBackup: () => void;
  importBackup: (data: string) => boolean;
}

const defaultUsers: AppUser[] = [
  { id: 'u1', username: 'admin', password: 'admin123', role: 'admin', name: 'System Admin', email: 'admin@medihelp.lk' },
  { id: 'u2', username: 'staff1', password: 'staff123', role: 'staff', name: 'Nurse Kumari', email: 'kumari@medihelp.lk' },
  { id: 'u3', username: 'doctor1', password: 'doc123', role: 'doctor', name: 'Dr. Perera', email: 'perera@medihelp.lk' },
  { id: 'u4', username: 'doctor2', password: 'doc456', role: 'doctor', name: 'Dr. Jayasinghe', email: 'jayasinghe@medihelp.lk' },
];

const defaultPatients: Patient[] = [
  { id: 'p1', name: 'Kamal Silva', dob: '2000-05-12', gender: 'Male', phone: '0771234567', address: '45/B, Kandy Rd, Colombo', studentId: 'SC2021001', registeredDate: '2024-01-15', bloodGroup: 'A+', allergies: 'None' },
  { id: 'p2', name: 'Nimali Jayawardena', dob: '2001-08-22', gender: 'Female', phone: '0779876543', address: '12, Peradeniya, Kandy', studentId: 'SC2021045', registeredDate: '2024-02-10', bloodGroup: 'B+', allergies: 'Penicillin' },
  { id: 'p3', name: 'Ruwan Fernando', dob: '1999-11-30', gender: 'Male', phone: '0715556677', address: '78, Galle Rd, Matara', studentId: 'SC2020099', registeredDate: '2024-03-05', bloodGroup: 'O+', allergies: 'None' },
  { id: 'p4', name: 'Sanduni Wickramasinghe', dob: '2002-03-14', gender: 'Female', phone: '0762223344', address: '23, Nugegoda, Colombo', studentId: 'SC2022013', registeredDate: '2024-04-20', bloodGroup: 'AB+', allergies: 'Aspirin' },
  { id: 'p5', name: 'Chamara Bandara', dob: '2000-07-08', gender: 'Male', phone: '0718889900', address: '56, Kurunegala', studentId: 'SC2021078', registeredDate: '2024-05-11', bloodGroup: 'A-', allergies: 'None' },
];

const defaultInventory: InventoryItem[] = [
  { id: 'i1', name: 'Paracetamol 500mg', category: 'Analgesic', quantity: 500, unit: 'Tablets', expiryDate: '2026-12-31', minStock: 50, supplier: 'PharmaCo Ltd' },
  { id: 'i2', name: 'Amoxicillin 250mg', category: 'Antibiotic', quantity: 200, unit: 'Capsules', expiryDate: '2026-06-30', minStock: 30, supplier: 'MedSupply Lanka' },
  { id: 'i3', name: 'Ibuprofen 400mg', category: 'NSAID', quantity: 300, unit: 'Tablets', expiryDate: '2026-09-15', minStock: 40, supplier: 'PharmaCo Ltd' },
  { id: 'i4', name: 'Vitamin C 500mg', category: 'Supplement', quantity: 400, unit: 'Tablets', expiryDate: '2027-01-01', minStock: 50, supplier: 'VitaPlus' },
  { id: 'i5', name: 'Chlorphenamine 4mg', category: 'Antihistamine', quantity: 150, unit: 'Tablets', expiryDate: '2026-08-30', minStock: 20, supplier: 'MedSupply Lanka' },
  { id: 'i6', name: 'ORS Sachets', category: 'Rehydration', quantity: 80, unit: 'Sachets', expiryDate: '2027-03-01', minStock: 20, supplier: 'PharmaCo Ltd' },
  { id: 'i7', name: 'Antacid Tablets', category: 'Gastro', quantity: 25, unit: 'Tablets', expiryDate: '2026-11-20', minStock: 30, supplier: 'VitaPlus' },
  { id: 'i8', name: 'Bandages (10cm)', category: 'First Aid', quantity: 60, unit: 'Rolls', expiryDate: '2028-01-01', minStock: 15, supplier: 'MediFirst' },
];

const defaultAppointments: Appointment[] = [
  { id: 'a1', patientId: 'p1', patientName: 'Kamal Silva', doctorId: 'u3', doctorName: 'Dr. Perera', date: '2026-06-10', time: '09:00', status: 'Scheduled', reason: 'Fever and body aches' },
  { id: 'a2', patientId: 'p2', patientName: 'Nimali Jayawardena', doctorId: 'u3', doctorName: 'Dr. Perera', date: '2026-06-10', time: '10:00', status: 'Scheduled', reason: 'Headache' },
  { id: 'a3', patientId: 'p3', patientName: 'Ruwan Fernando', doctorId: 'u4', doctorName: 'Dr. Jayasinghe', date: '2026-06-09', time: '14:00', status: 'Completed', reason: 'Common cold' },
  { id: 'a4', patientId: 'p4', patientName: 'Sanduni Wickramasinghe', doctorId: 'u3', doctorName: 'Dr. Perera', date: '2026-06-11', time: '11:00', status: 'Scheduled', reason: 'Stomach pain' },
  { id: 'a5', patientId: 'p5', patientName: 'Chamara Bandara', doctorId: 'u4', doctorName: 'Dr. Jayasinghe', date: '2026-06-08', time: '09:30', status: 'Completed', reason: 'Sore throat' },
];

const defaultPrescriptions: Prescription[] = [
  {
    id: 'rx1',
    patientId: 'p3',
    patientName: 'Ruwan Fernando',
    doctorId: 'u4',
    doctorName: 'Dr. Jayasinghe',
    date: '2026-06-09',
    diagnosis: 'Common Cold',
    items: [
      { inventoryItemId: 'i1', medicationName: 'Paracetamol 500mg', dosage: '500mg', quantity: 10, instructions: 'Take 1 tablet 3 times daily after meals' },
      { inventoryItemId: 'i5', medicationName: 'Chlorphenamine 4mg', dosage: '4mg', quantity: 6, instructions: 'Take 1 tablet at night' },
    ],
    status: 'Processed',
    notes: 'Rest for 3 days. Drink plenty of fluids.',
  },
  {
    id: 'rx2',
    patientId: 'p5',
    patientName: 'Chamara Bandara',
    doctorId: 'u4',
    doctorName: 'Dr. Jayasinghe',
    date: '2026-06-08',
    diagnosis: 'Pharyngitis',
    items: [
      { inventoryItemId: 'i2', medicationName: 'Amoxicillin 250mg', dosage: '250mg', quantity: 15, instructions: 'Take 1 capsule 3 times daily for 5 days' },
      { inventoryItemId: 'i1', medicationName: 'Paracetamol 500mg', dosage: '500mg', quantity: 6, instructions: 'Take 1 tablet when needed for pain' },
    ],
    status: 'Pending',
    notes: 'Complete the full course of antibiotics.',
  },
];

function loadFromStorage<T>(key: string, defaultValue: T): T {
  try {
    const stored = localStorage.getItem(key);
    return stored ? JSON.parse(stored) : defaultValue;
  } catch {
    return defaultValue;
  }
}

function saveToStorage<T>(key: string, value: T): void {
  localStorage.setItem(key, JSON.stringify(value));
}

const AppContext = createContext<AppContextType | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<AppUser | null>(() => {
    const stored = localStorage.getItem('currentUser');
    return stored ? JSON.parse(stored) : null;
  });
  const [users, setUsers] = useState<AppUser[]>(() => loadFromStorage('users', defaultUsers));
  const [patients, setPatients] = useState<Patient[]>(() => loadFromStorage('patients', defaultPatients));
  const [appointments, setAppointments] = useState<Appointment[]>(() => loadFromStorage('appointments', defaultAppointments));
  const [inventory, setInventory] = useState<InventoryItem[]>(() => loadFromStorage('inventory', defaultInventory));
  const [prescriptions, setPrescriptions] = useState<Prescription[]>(() => loadFromStorage('prescriptions', defaultPrescriptions));

  useEffect(() => { saveToStorage('users', users); }, [users]);
  useEffect(() => { saveToStorage('patients', patients); }, [patients]);
  useEffect(() => { saveToStorage('appointments', appointments); }, [appointments]);
  useEffect(() => { saveToStorage('inventory', inventory); }, [inventory]);
  useEffect(() => { saveToStorage('prescriptions', prescriptions); }, [prescriptions]);

  const login = (username: string, password: string) => {
    const user = users.find(u => u.username === username && u.password === password);
    if (user) {
      setCurrentUser(user);
      localStorage.setItem('currentUser', JSON.stringify(user));
      return true;
    }
    return false;
  };

  const logout = () => {
    setCurrentUser(null);
    localStorage.removeItem('currentUser');
  };

  const genId = () => Date.now().toString(36) + Math.random().toString(36).slice(2);

  const addUser = (user: Omit<AppUser, 'id'>) => setUsers(prev => [...prev, { ...user, id: genId() }]);
  const updateUser = (id: string, data: Partial<AppUser>) => {
    setUsers(prev => prev.map(u => u.id === id ? { ...u, ...data } : u));
    if (currentUser?.id === id) {
      setCurrentUser(prev => prev ? { ...prev, ...data } : null);
    }
  };
  const removeUser = (id: string) => setUsers(prev => prev.filter(u => u.id !== id));

  const addPatient = (patient: Omit<Patient, 'id' | 'registeredDate'>) =>
    setPatients(prev => [...prev, { ...patient, id: genId(), registeredDate: new Date().toISOString().split('T')[0] }]);
  const updatePatient = (id: string, data: Partial<Patient>) =>
    setPatients(prev => prev.map(p => p.id === id ? { ...p, ...data } : p));
  const removePatient = (id: string) => setPatients(prev => prev.filter(p => p.id !== id));

  const addAppointment = (appt: Omit<Appointment, 'id'>) =>
    setAppointments(prev => [...prev, { ...appt, id: genId() }]);
  const updateAppointment = (id: string, data: Partial<Appointment>) =>
    setAppointments(prev => prev.map(a => a.id === id ? { ...a, ...data } : a));
  const removeAppointment = (id: string) => setAppointments(prev => prev.filter(a => a.id !== id));

  const addInventoryItem = (item: Omit<InventoryItem, 'id'>) =>
    setInventory(prev => [...prev, { ...item, id: genId() }]);
  const updateInventoryItem = (id: string, data: Partial<InventoryItem>) =>
    setInventory(prev => prev.map(i => i.id === id ? { ...i, ...data } : i));
  const removeInventoryItem = (id: string) => setInventory(prev => prev.filter(i => i.id !== id));

  const addPrescription = (rx: Omit<Prescription, 'id'>) =>
    setPrescriptions(prev => [...prev, { ...rx, id: genId() }]);

  const processPrescription = (id: string) => {
    const rx = prescriptions.find(p => p.id === id);
    if (!rx || rx.status === 'Processed') return;
    // Deduct from inventory
    setInventory(prev => prev.map(item => {
      const rxItem = rx.items.find(r => r.inventoryItemId === item.id);
      if (rxItem) return { ...item, quantity: Math.max(0, item.quantity - rxItem.quantity) };
      return item;
    }));
    setPrescriptions(prev => prev.map(p => p.id === id ? { ...p, status: 'Processed' } : p));
  };

  const exportBackup = () => {
    const data = { users, patients, appointments, inventory, prescriptions, exportedAt: new Date().toISOString() };
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `medihelp_backup_${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const importBackup = (jsonStr: string): boolean => {
    try {
      const data = JSON.parse(jsonStr);
      if (data.users) setUsers(data.users);
      if (data.patients) setPatients(data.patients);
      if (data.appointments) setAppointments(data.appointments);
      if (data.inventory) setInventory(data.inventory);
      if (data.prescriptions) setPrescriptions(data.prescriptions);
      return true;
    } catch {
      return false;
    }
  };

  return (
    <AppContext.Provider value={{
      currentUser, users, patients, appointments, inventory, prescriptions,
      login, logout,
      addUser, updateUser, removeUser,
      addPatient, updatePatient, removePatient,
      addAppointment, updateAppointment, removeAppointment,
      addInventoryItem, updateInventoryItem, removeInventoryItem,
      addPrescription, processPrescription,
      exportBackup, importBackup,
    }}>
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useApp must be used within AppProvider');
  return ctx;
}
