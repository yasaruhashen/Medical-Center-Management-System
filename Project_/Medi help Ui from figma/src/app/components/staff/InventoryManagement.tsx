import { useState } from 'react';
import { useApp, InventoryItem } from '../../context/AppContext';
import { Plus, Search, Pencil, Trash2, X, Check, AlertTriangle, Package } from 'lucide-react';

type FormData = Omit<InventoryItem, 'id'>;
const emptyForm: FormData = { name: '', category: '', quantity: 0, unit: 'Tablets', expiryDate: '', minStock: 10, supplier: '' };

export function InventoryManagement() {
  const { inventory, addInventoryItem, updateInventoryItem, removeInventoryItem } = useApp();
  const [search, setSearch] = useState('');
  const [filterCat, setFilterCat] = useState('All');
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [msg, setMsg] = useState('');

  const categories = ['All', ...Array.from(new Set(inventory.map(i => i.category)))];
  const filtered = inventory.filter(i =>
    (filterCat === 'All' || i.category === filterCat) &&
    (i.name.toLowerCase().includes(search.toLowerCase()) || i.category.toLowerCase().includes(search.toLowerCase()))
  );

  const openAdd = () => { setForm(emptyForm); setEditId(null); setShowForm(true); };
  const openEdit = (i: InventoryItem) => { setForm({ name: i.name, category: i.category, quantity: i.quantity, unit: i.unit, expiryDate: i.expiryDate, minStock: i.minStock, supplier: i.supplier || '' }); setEditId(i.id); setShowForm(true); };

  const handleSave = () => {
    if (!form.name || !form.category || !form.unit) { setMsg('Name, category, and unit are required.'); return; }
    if (editId) { updateInventoryItem(editId, form); flash('Item updated.'); }
    else { addInventoryItem(form); flash('Item added to inventory.'); }
    setShowForm(false);
  };

  const flash = (text: string) => { setMsg(text); setTimeout(() => setMsg(''), 3000); };

  const stockStatus = (i: InventoryItem) => {
    if (i.quantity === 0) return { label: 'Out of Stock', bg: '#fef2f2', color: '#991b1b' };
    if (i.quantity <= i.minStock) return { label: 'Low Stock', bg: '#fffbeb', color: '#92400e' };
    return { label: 'In Stock', bg: '#f0fdf4', color: '#16a34a' };
  };

  const isExpiringSoon = (expiry: string) => {
    const d = new Date(expiry);
    const diff = (d.getTime() - Date.now()) / (1000 * 60 * 60 * 24);
    return diff <= 30 && diff > 0;
  };

  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div><h2 className="text-gray-800" style={{ fontWeight: 700 }}>Inventory Management</h2><p className="text-gray-500 text-sm">Manage medicines and medical supplies</p></div>
        <button onClick={openAdd} className="flex items-center gap-2 px-4 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}><Plus size={16} /> Add Item</button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total Items', value: inventory.length, color: '#8A0007' },
          { label: 'In Stock', value: inventory.filter(i => i.quantity > i.minStock).length, color: '#16a34a' },
          { label: 'Low Stock', value: inventory.filter(i => i.quantity > 0 && i.quantity <= i.minStock).length, color: '#d97706' },
          { label: 'Out of Stock', value: inventory.filter(i => i.quantity === 0).length, color: '#991b1b' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 text-center">
            <p style={{ fontSize: '1.6rem', fontWeight: 700, color: s.color }}>{s.value}</p>
            <p className="text-gray-500 text-xs">{s.label}</p>
          </div>
        ))}
      </div>

      {msg && <div className="px-4 py-2.5 rounded-lg text-sm bg-green-50 text-green-800 border border-green-200">{msg}</div>}

      <div className="flex gap-3">
        <div className="relative flex-1"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search items..." className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none" /></div>
        <select value={filterCat} onChange={e => setFilterCat(e.target.value)} className="px-3 py-2 border border-gray-200 rounded-lg bg-white text-sm outline-none">
          {categories.map(c => <option key={c}>{c}</option>)}
        </select>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table className="w-full">
          <thead><tr style={{ background: '#4E0205' }}>{['Item', 'Category', 'Quantity', 'Unit', 'Expiry', 'Min Stock', 'Status', 'Actions'].map(h => <th key={h} className="px-4 py-3 text-left text-white text-xs" style={{ fontWeight: 600 }}>{h}</th>)}</tr></thead>
          <tbody>
            {filtered.map((item, i) => {
              const st = stockStatus(item);
              return (
                <tr key={item.id} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <Package size={15} style={{ color: '#8A0007' }} />
                      <span className="text-gray-800 text-sm" style={{ fontWeight: 500 }}>{item.name}</span>
                      {isExpiringSoon(item.expiryDate) && <AlertTriangle size={13} className="text-amber-500" title="Expiring soon" />}
                    </div>
                    {item.supplier && <p className="text-gray-400 text-xs ml-5">{item.supplier}</p>}
                  </td>
                  <td className="px-4 py-3"><span className="px-2 py-0.5 rounded text-xs" style={{ background: '#f3f4f6', color: '#374151' }}>{item.category}</span></td>
                  <td className="px-4 py-3 text-sm" style={{ fontWeight: 700, color: item.quantity <= item.minStock ? '#8A0007' : '#374151' }}>{item.quantity}</td>
                  <td className="px-4 py-3 text-gray-600 text-sm">{item.unit}</td>
                  <td className="px-4 py-3 text-sm" style={{ color: isExpiringSoon(item.expiryDate) ? '#d97706' : '#6b7280' }}>{item.expiryDate}</td>
                  <td className="px-4 py-3 text-gray-600 text-sm">{item.minStock}</td>
                  <td className="px-4 py-3"><span className="px-2 py-0.5 rounded-full text-xs" style={{ background: st.bg, color: st.color, fontWeight: 600 }}>{st.label}</span></td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button onClick={() => openEdit(item)} className="p-1.5 rounded-lg hover:bg-blue-50 text-blue-600"><Pencil size={14} /></button>
                      <button onClick={() => setDeleteId(item.id)} className="p-1.5 rounded-lg hover:bg-red-50" style={{ color: '#8A0007' }}><Trash2 size={14} /></button>
                    </div>
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && <tr><td colSpan={8} className="text-center py-8 text-gray-400 text-sm">No items found.</td></tr>}
          </tbody>
        </table>
      </div>

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4">
            <div className="flex items-center justify-between px-6 py-4" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)', borderRadius: '1rem 1rem 0 0' }}>
              <h3 className="text-white" style={{ fontWeight: 700 }}>{editId ? 'Edit Item' : 'Add Inventory Item'}</h3>
              <button onClick={() => setShowForm(false)} className="text-red-200 hover:text-white"><X size={20} /></button>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                {[
                  { label: 'Item Name *', field: 'name', type: 'text', col: 'col-span-2' },
                  { label: 'Category *', field: 'category', type: 'text', col: '' },
                  { label: 'Unit *', field: 'unit', type: 'text', col: '' },
                  { label: 'Quantity', field: 'quantity', type: 'number', col: '' },
                  { label: 'Min Stock', field: 'minStock', type: 'number', col: '' },
                  { label: 'Expiry Date', field: 'expiryDate', type: 'date', col: '' },
                  { label: 'Supplier', field: 'supplier', type: 'text', col: '' },
                ].map(({ label, field, type, col }) => (
                  <div key={field} className={col || ''}>
                    <label className="block text-sm text-gray-700 mb-1" style={{ fontWeight: 500 }}>{label}</label>
                    <input type={type} value={(form as Record<string, string | number>)[field]} onChange={e => setForm(f => ({ ...f, [field]: type === 'number' ? Number(e.target.value) : e.target.value }))}
                      className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none focus:border-red-400 bg-white" min={0} />
                  </div>
                ))}
              </div>
              {msg && <p className="text-red-600 text-sm">{msg}</p>}
              <div className="flex gap-3 pt-2">
                <button onClick={() => setShowForm(false)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
                <button onClick={handleSave} className="flex-1 py-2 rounded-lg text-white text-sm flex items-center justify-center gap-2" style={{ background: '#8A0007', fontWeight: 600 }}><Check size={16} />{editId ? 'Update' : 'Add Item'}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 p-6 text-center">
            <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-4" style={{ background: '#fff0f0' }}><Trash2 size={22} style={{ color: '#8A0007' }} /></div>
            <h3 className="text-gray-800 mb-2" style={{ fontWeight: 700 }}>Remove Item?</h3>
            <p className="text-gray-500 text-sm mb-5">This will permanently delete the inventory item.</p>
            <div className="flex gap-3">
              <button onClick={() => setDeleteId(null)} className="flex-1 py-2 rounded-lg border border-gray-200 text-gray-600 text-sm">Cancel</button>
              <button onClick={() => { removeInventoryItem(deleteId); setDeleteId(null); flash('Item removed.'); }} className="flex-1 py-2 rounded-lg text-white text-sm" style={{ background: '#8A0007', fontWeight: 600 }}>Remove</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
