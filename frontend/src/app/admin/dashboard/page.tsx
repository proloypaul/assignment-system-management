'use client';

import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';

export default function AdminDashboard() {
  return (
    <AuthGuard allowedRoles={['Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">Welcome, Admin</h2>
            <p className="text-muted-foreground mt-1 text-sm">
              Here is an overview of the system status.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="p-6 bg-background rounded-xl border border-border shadow-sm">
              <h3 className="text-lg font-medium">Total Users</h3>
              <p className="text-3xl font-bold mt-2">Manage Users</p>
            </div>
            <div className="p-6 bg-background rounded-xl border border-border shadow-sm">
              <h3 className="text-lg font-medium">Total Courses</h3>
              <p className="text-3xl font-bold mt-2">Manage Courses</p>
            </div>
            <div className="p-6 bg-background rounded-xl border border-border shadow-sm">
              <h3 className="text-lg font-medium">Total Assignments</h3>
              <p className="text-3xl font-bold mt-2">View Assignments</p>
            </div>
          </div>
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
