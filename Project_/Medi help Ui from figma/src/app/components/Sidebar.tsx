import { ReactNode } from 'react';
import { useApp } from '../context/AppContext';
import logo from '../../imports/logo.png';
import { LogOut } from 'lucide-react';

interface NavItem {
  id: string;
  label: string;
  icon: ReactNode;
}

interface SidebarProps {
  navItems: NavItem[];
  currentPage: string;
  onNavigate: (page: string) => void;
  title: string;
  subtitle: string;
}

export function Sidebar({ navItems, currentPage, onNavigate, title, subtitle }: SidebarProps) {
  const { currentUser, logout } = useApp();

  return (
    <aside className="flex flex-col h-full w-64 border-r border-gray-200 bg-white shadow-sm flex-shrink-0">
      {/* Logo area */}
      <div className="flex flex-col items-center px-4 py-5 border-b border-gray-100" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)' }}>
        <div className="bg-white rounded-xl p-2 shadow mb-3">
          <img src={logo} alt="MEDI HELP J'PURA" className="h-12 w-auto object-contain" />
        </div>
        <p className="text-white text-center leading-tight" style={{ fontSize: '0.75rem', fontWeight: 700 }}>{title}</p>
        <p className="text-red-200 text-center" style={{ fontSize: '0.65rem' }}>{subtitle}</p>
      </div>

      {/* User badge */}
      <div className="px-4 py-3 border-b border-gray-100 bg-gray-50">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-xs flex-shrink-0" style={{ background: '#8A0007', fontWeight: 700 }}>
            {currentUser?.name.charAt(0)}
          </div>
          <div className="min-w-0">
            <p className="text-gray-800 truncate" style={{ fontSize: '0.8rem', fontWeight: 600 }}>{currentUser?.name}</p>
            <p className="text-gray-400 capitalize" style={{ fontSize: '0.7rem' }}>{currentUser?.role}</p>
          </div>
        </div>
      </div>

      {/* Nav items */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        <ul className="space-y-1">
          {navItems.map(item => {
            const active = currentPage === item.id;
            return (
              <li key={item.id}>
                <button
                  onClick={() => onNavigate(item.id)}
                  className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all text-left"
                  style={{
                    background: active ? '#8A0007' : 'transparent',
                    color: active ? '#ffffff' : '#374151',
                    fontWeight: active ? 600 : 400,
                    fontSize: '0.875rem',
                  }}
                  onMouseEnter={e => { if (!active) { (e.currentTarget as HTMLElement).style.background = '#fff5f5'; (e.currentTarget as HTMLElement).style.color = '#8A0007'; } }}
                  onMouseLeave={e => { if (!active) { (e.currentTarget as HTMLElement).style.background = 'transparent'; (e.currentTarget as HTMLElement).style.color = '#374151'; } }}
                >
                  <span className="flex-shrink-0">{item.icon}</span>
                  <span className="truncate">{item.label}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Logout */}
      <div className="px-3 py-4 border-t border-gray-100">
        <button
          onClick={logout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all"
          style={{ color: '#6b7280', fontSize: '0.875rem' }}
          onMouseEnter={e => { (e.currentTarget as HTMLElement).style.background = '#fef2f2'; (e.currentTarget as HTMLElement).style.color = '#8A0007'; }}
          onMouseLeave={e => { (e.currentTarget as HTMLElement).style.background = 'transparent'; (e.currentTarget as HTMLElement).style.color = '#6b7280'; }}
        >
          <LogOut size={18} />
          <span>Log Out</span>
        </button>
      </div>
    </aside>
  );
}
