import { useState } from 'react';
import { useApp, AppUser, UserRole } from '../../context/AppContext';
import { Plus, Pencil, Trash2, X, Check, Search } from 'lucide-react';

type FormData = { username: string; password: string; role: UserRole; name: string; email: string };
const emptyForm: FormData = { username: '', password: '', role: 'staff', name: '', email: '' };

export function UserManagement() {
  const { users, currentUser, addUser, updateUser, removeUser } = useApp();
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [msg, setMsg] = useState('');

  const filtered = users.filter(u =>
    u.name.toLowerCase().includes(search.toLowerCase()) ||
    u.username.toLowerCase().includes(search.toLowerCase()) ||
    u.role.toLowerCase().includes(search.toLowerCase())
  );

  const openAdd = () => { setForm(emptyForm); setEditId(null); setShowForm(true); };
  const openEdit = (u: AppUser) => { setForm({ username: u.username, password: u.password, role: u.role, name: u.name, email: u.email || '' }); setEditId(u.id); setShowForm(true); };

  const handleSave = () => {
    if (!form.username || !form.password || !form.name) { setMsg('Please fill all required fields.'); return; }
    if (!editId && users.find(u => u.username === form.username)) { setMsg('Username already exists.'); return; }
    if (editId) {
      updateUser(editId, form);
      setMsg('User updated successfully.');
    } else {
      addUser(form);
      setMsg('User added successfully.');
    }
    setShowForm(false);
    setTimeout(() => setMsg(''), 3000);
  };

  const handleDelete = (id: string) => {
    if (id === currentUser?.id) { setMsg('Cannot delete your own account.'); setTimeout(() => setMsg(''), 3000); return; }
    removeUser(id);
    setDeleteId(null);
    setMsg('User removed.');
    setTimeout(() => setMsg(''), 3000);
  };

  const roleColor: Record<string, string> = { admin: '#8A0007', doctor: '#4E0205', staff: '#b45309' };
  const roleBg: Record<string, string> = { admin: '#fff0f0', doctor: '#fdf0f0', staff: '#fffbeb' };

  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-gray-800" style={{ fontWeight: 700 }}>User Management</h2>
          <p className="text-gray-500 text-sm">Manage system users — add, edit, or remove accounts</p>
        </div>
        <button onClick={openAdd} className="flex items-center gap-2 px-4 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>
          <Plus size={16} /> Add User
        </button>
      </div>

      {msg && <div className="px-4 py-2.5 rounded-lg text-sm" style={{ background: msg.startsWith('Cannot') ? '#fef2f2' : '#f0fdf4', color: msg.startsWith('Cannot') ? '#991b1b' : '#166534', border: '1px solid', borderColor: msg.startsWith('Cannot') ? '#fecaca' : '#bbf7d0' }}>{msg}</div>}

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search users..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" style={{ fontSize: '0.875rem' }} />
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table className="w-full">
          <thead>
            <tr style={{ background: '#4E0205' }}>
              {['Name', 'Username', 'Role', 'Email', 'Actions'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-white text-xs" style={{ fontWeight: 600 }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {filtered.map((u, i) => (
              <tr key={u.id} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <div className="w-7 h-7 rounded-full flex items-center justify-center text-white text-xs" style={{ background: roleColor[u.role] || '#8A0007', fontWeight: 700 }}>{u.name.charAt(0)}</div>
                    <span className="text-gray-800 text-sm">{u.name}</span>
                    {u.id === currentUser?.id && <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: '#F7AA37', color: '#000' }}>You</span>}
                  </div>
                </td>
                <td className="px-4 py-3 text-gray-600 text-sm">{u.username}</td>
                <td className="px-4 py-3">
                  <span className="px-2 py-0.5 rounded-full text-xs capitalize" style={{ background: roleBg[u.role], color: roleColor[u.role], fontWeight: 600 }}>{u.role}</span>
                </td>
                <td className="px-4 py-3 text-gray-500 text-sm">{u.email || '—'}</td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <button onClick={() => openEdit(u)} className="p-1.5 rounded-lg hover:bg-blue-50 text-blue-600 transition-colors"><Pencil size={14} /></button>
                    <button onClick={() => setDeleteId(u.id)} className="p-1.5 rounded-lg hover:bg-red-50 transition-colors" style={{ color: '#8A0007' }}><Trash2 size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && <tr><td colSpan={5} className="text-center py-8 text-gray-400 text-sm">No users found.</td></tr>}
          </tbody>
        </table>
      </div>

      {/* Add/Edit Modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)', borderRadius: '1rem 1rem 0 0' }}>
              <h3 className="text-white" style={{ fontWeight: 700 }}>{editId ? 'Edit User' : 'Add New User'}</h3>
              <button onClick={() => setShowForm(false)} className="text-red-200 hover:text-white"><X size={20} /></button>
            </div>
            <div className="p-6 space-y-4">
              {[
                { label: 'Full Name *', field: 'name', type: 'text', placeholder: 'e.g. Dr. Perera' },
                { label: 'Username *', field: 'username', type: 'text', placeholder: 'e.g. doctor1' },
                { label: 'Password *', field: 'password', type: 'password', placeholder: '••••••••' },
                { label: 'Email', field: 'email', type: 'email', placeholder: 'user@medihelp.lk' },
              ].map(({ label, field, type, placeholder }) => (
                <div key={field}>
                  <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>{label}</label>
                  <input type={type} value={(form as Record<string, string>)[field]} onChange={e => setForm(f => ({ ...f, [field]: e.target.value }))} placeholder={placeholder}
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white" />
                </div>
              ))}
              <div>
                <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>Role *</label>
                <select value={form.role} onChange={e => setForm(f => ({ ...f, role: e.target.value as UserRole }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white">
                  <option value="admin">Admin</option>
                  <option value="doctor">Doctor</option>
                  <option value="staff">Staff</option>
                </select>
              </div>
              {msg && <p className="text-red-600 text-sm">{msg}</p>}
              <div className="flex gap-3 pt-2">
                <button onClick={() => setShowForm(false)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm hover:bg-gray-50">Cancel</button>
                <button onClick={handleSave} className="flex-1 py-2 rounded-lg text-white text-sm flex items-center justify-center gap-2" style={{ background: '#8A0007', fontWeight: 600 }}>
                  <Check size={16} />{editId ? 'Update' : 'Add User'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirm */}
      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 p-6 text-center">
            <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-4" style={{ background: '#fff0f0' }}>
              <Trash2 size={22} style={{ color: '#8A0007' }} />
            </div>
            <h3 className="text-gray-800 mb-2" style={{ fontWeight: 700 }}>Delete User?</h3>
            <p className="text-gray-500 text-sm mb-5">This action cannot be undone. The user will lose all access.</p>
            <div className="flex gap-3">
              <button onClick={() => setDeleteId(null)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
              <button onClick={() => handleDelete(deleteId)} className="flex-1 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
