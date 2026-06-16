import { useState } from 'react';
import { useApp } from '../context/AppContext';
import logo from '../../imports/logo.png';
import { Lock, User, AlertCircle } from 'lucide-react';

export function Login() {
  const { login } = useApp();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    await new Promise(r => setTimeout(r, 400));
    const ok = login(username.trim(), password);
    if (!ok) setError('Invalid username or password. Please try again.');
    setLoading(false);
  };

  return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: 'linear-gradient(135deg, #4E0205 0%, #8A0007 50%, #4E0205 100%)' }}>
      {/* Background pattern */}
      <div className="absolute inset-0 opacity-5" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '30px 30px' }} />

      <div className="relative w-full max-w-md mx-4">
        {/* Card */}
        <div className="bg-white rounded-2xl shadow-2xl overflow-hidden">
          {/* Header band */}
          <div className="px-8 py-8 text-center" style={{ background: 'linear-gradient(135deg, #8A0007, #4E0205)' }}>
            <div className="flex justify-center mb-4">
              <div className="bg-white rounded-xl p-3 shadow-lg">
                <img src={logo} alt="MEDI HELP J'PURA" className="h-16 w-auto object-contain" />
              </div>
            </div>
            <h1 className="text-white text-xl mb-1" style={{ fontSize: '1.25rem', fontWeight: 700 }}>MEDI HELP J'PURA</h1>
            <p className="text-red-200 text-sm">Medical Center Management System</p>
          </div>

          {/* Form */}
          <div className="px-8 py-8">
            <p className="text-gray-500 text-sm text-center mb-6">Sign in to your account to continue</p>

            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label className="block text-sm text-gray-700 mb-1.5" style={{ fontWeight: 500 }}>Username</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
                  <input
                    type="text"
                    value={username}
                    onChange={e => setUsername(e.target.value)}
                    placeholder="Enter your username"
                    className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-lg outline-none focus:ring-2 transition-all bg-white"
                    style={{ fontSize: '0.9rem' }}
                    onFocus={e => (e.target.style.borderColor = '#8A0007', e.target.style.boxShadow = '0 0 0 3px rgba(138,0,7,0.1)')}
                    onBlur={e => (e.target.style.borderColor = '#d1d5db', e.target.style.boxShadow = 'none')}
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm text-gray-700 mb-1.5" style={{ fontWeight: 500 }}>Password</label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
                  <input
                    type="password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    placeholder="Enter your password"
                    className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-lg outline-none transition-all bg-white"
                    style={{ fontSize: '0.9rem' }}
                    onFocus={e => (e.target.style.borderColor = '#8A0007', e.target.style.boxShadow = '0 0 0 3px rgba(138,0,7,0.1)')}
                    onBlur={e => (e.target.style.borderColor = '#d1d5db', e.target.style.boxShadow = 'none')}
                    required
                  />
                </div>
              </div>

              {error && (
                <div className="flex items-center gap-2 text-red-600 bg-red-50 px-3 py-2.5 rounded-lg border border-red-200">
                  <AlertCircle size={16} />
                  <span className="text-sm">{error}</span>
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full py-3 rounded-lg text-white transition-all"
                style={{
                  background: loading ? '#c97b80' : 'linear-gradient(135deg, #8A0007, #4E0205)',
                  fontWeight: 600,
                  fontSize: '0.95rem',
                  cursor: loading ? 'not-allowed' : 'pointer',
                }}
              >
                {loading ? 'Signing in...' : 'Sign In'}
              </button>
            </form>

            <div className="mt-6 p-3 bg-gray-50 rounded-lg border border-gray-200">
              <p className="text-xs text-gray-500 text-center mb-2" style={{ fontWeight: 500 }}>Demo Accounts</p>
              <div className="grid grid-cols-3 gap-2 text-xs text-gray-600">
                <div className="text-center"><div style={{ color: '#8A0007', fontWeight: 600 }}>Admin</div><div>admin / admin123</div></div>
                <div className="text-center"><div style={{ color: '#8A0007', fontWeight: 600 }}>Staff</div><div>staff1 / staff123</div></div>
                <div className="text-center"><div style={{ color: '#8A0007', fontWeight: 600 }}>Doctor</div><div>doctor1 / doc123</div></div>
              </div>
            </div>
          </div>
        </div>

        <p className="text-center text-red-200 text-xs mt-4">© 2026 MEDI HELP J'PURA. All rights reserved.</p>
      </div>
    </div>
  );
}
