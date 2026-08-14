'use client';

import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';

export default function StudentDashboard() {
  return (
    <AuthGuard allowedRoles={['Student']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">Welcome, Student</h2>
            <p className="text-muted-foreground mt-1 text-sm">
              Track your upcoming assignments and grades.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="p-6 bg-background rounded-xl border border-border shadow-sm">
              <h3 className="text-lg font-medium">My Assignments</h3>
              <p className="text-3xl font-bold mt-2">View Assignments</p>
            </div>
            <div className="p-6 bg-background rounded-xl border border-border shadow-sm">
              <h3 className="text-lg font-medium">My Submissions</h3>
              <p className="text-3xl font-bold mt-2">View Grades</p>
            </div>
          </div>
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
