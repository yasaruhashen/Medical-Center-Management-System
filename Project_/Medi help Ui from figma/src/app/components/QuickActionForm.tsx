import { UserPlus, FileText, Pill } from 'lucide-react';

export function QuickActionForm() {
  return (
    <div className="bg-card border border-border rounded-xl p-6">
      <h2 className="font-semibold text-foreground mb-4">Quick Actions</h2>

      <div className="grid grid-cols-3 gap-4 mb-6">
        <button className="flex flex-col items-center gap-3 p-4 border-2 border-border rounded-lg hover:border-primary hover:bg-primary/5 transition-all">
          <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center">
            <UserPlus className="w-6 h-6 text-primary" />
          </div>
          <span className="text-sm font-medium text-foreground">New Patient</span>
        </button>

        <button className="flex flex-col items-center gap-3 p-4 border-2 border-border rounded-lg hover:border-primary hover:bg-primary/5 transition-all">
          <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center">
            <FileText className="w-6 h-6 text-primary" />
          </div>
          <span className="text-sm font-medium text-foreground">Health Record</span>
        </button>

        <button className="flex flex-col items-center gap-3 p-4 border-2 border-border rounded-lg hover:border-primary hover:bg-primary/5 transition-all">
          <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center">
            <Pill className="w-6 h-6 text-primary" />
          </div>
          <span className="text-sm font-medium text-foreground">Prescription</span>
        </button>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">Patient Name</label>
          <input
            type="text"
            placeholder="Enter patient name"
            className="w-full px-4 py-2 bg-input-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-foreground mb-2">Age</label>
            <input
              type="number"
              placeholder="Age"
              className="w-full px-4 py-2 bg-input-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-foreground mb-2">Blood Group</label>
            <select className="w-full px-4 py-2 bg-input-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring">
              <option>Select</option>
              <option>A+</option>
              <option>A-</option>
              <option>B+</option>
              <option>B-</option>
              <option>O+</option>
              <option>O-</option>
              <option>AB+</option>
              <option>AB-</option>
            </select>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-foreground mb-2">Department</label>
          <select className="w-full px-4 py-2 bg-input-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring">
            <option>Select Department</option>
            <option>Cardiology</option>
            <option>Neurology</option>
            <option>Orthopedics</option>
            <option>Pediatrics</option>
            <option>Dermatology</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-foreground mb-2">Symptoms / Notes</label>
          <textarea
            rows={3}
            placeholder="Enter symptoms or medical notes..."
            className="w-full px-4 py-2 bg-input-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none"
          />
        </div>

        <button className="w-full bg-primary text-primary-foreground py-3 rounded-lg font-medium hover:bg-primary/90 transition-colors">
          Submit Record
        </button>
      </div>
    </div>
  );
}
