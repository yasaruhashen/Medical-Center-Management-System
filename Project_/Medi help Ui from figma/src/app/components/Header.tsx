import { Bell, Search, ChevronDown } from 'lucide-react';

export function Header() {
  return (
    <header className="h-16 bg-card border-b border-border flex items-center justify-between px-8">
      <div className="flex-1 max-w-xl">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search patients, appointments, records..."
            className="w-full pl-10 pr-4 py-2 bg-muted border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>

      <div className="flex items-center gap-4">
        <button className="relative p-2 hover:bg-muted rounded-lg transition-colors">
          <Bell className="w-5 h-5 text-foreground" />
          <span className="absolute top-1 right-1 w-2 h-2 bg-primary rounded-full"></span>
        </button>

        <div className="h-8 w-px bg-border"></div>

        <button className="flex items-center gap-3 hover:bg-muted rounded-lg px-3 py-2 transition-colors">
          <div className="w-8 h-8 bg-primary rounded-full flex items-center justify-center">
            <span className="text-primary-foreground text-sm font-medium">DR</span>
          </div>
          <div className="text-left">
            <p className="text-sm font-medium text-foreground">Dr. Rajesh Kumar</p>
            <p className="text-xs text-muted-foreground">Administrator</p>
          </div>
          <ChevronDown className="w-4 h-4 text-muted-foreground" />
        </button>
      </div>
    </header>
  );
}
