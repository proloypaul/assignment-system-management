'use client';

import AuthGuard from '@/components/AuthGuard';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/Button';
import api from '@/lib/api';
import { useRouter } from 'next/navigation';

export default function TeacherDashboard() {
  const user = useAuthStore(state => state.user);
  const logout = useAuthStore(state => state.logout);
  const router = useRouter();

  const handleLogout = async () => {
    try {
      await api.post('/auth/logout');
    } catch (e) {
      console.error(e);
    }
    logout();
    router.push('/login');
  };

  return (
    <AuthGuard allowedRoles={['Teacher']}>
      <div className="min-h-screen bg-muted p-8">
        <div className="max-w-4xl mx-auto bg-background rounded-xl shadow border border-border p-8">
          <div className="flex justify-between items-center mb-8">
            <h1 className="text-3xl font-bold tracking-tight">Teacher Dashboard</h1>
            <Button variant="outline" onClick={handleLogout}>Sign Out</Button>
          </div>
          
          <div className="p-6 bg-primary/10 rounded-lg border border-primary/20 mb-8">
            <h2 className="text-xl font-semibold mb-2">Welcome back, {user?.name}!</h2>
            <p className="text-muted-foreground">
              You are signed in as a <span className="font-medium text-foreground">Teacher</span>.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="p-6 border border-border rounded-lg shadow-sm bg-card">
              <h3 className="font-semibold text-lg">Manage Assignments</h3>
              <p className="text-sm text-muted-foreground mt-2">Create and update assignments for your subjects.</p>
            </div>
            <div className="p-6 border border-border rounded-lg shadow-sm bg-card">
              <h3 className="font-semibold text-lg">Grade Submissions</h3>
              <p className="text-sm text-muted-foreground mt-2">Review and grade student submissions.</p>
            </div>
          </div>
        </div>
      </div>
    </AuthGuard>
  );
}
