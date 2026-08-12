'use client';

import { useAuthStore } from '@/store/authStore';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { BookOpen, LogOut, LayoutDashboard, FileText, Users, Settings } from 'lucide-react';
import { Button } from './ui/Button';

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuthStore();
  const pathname = usePathname();
  const router = useRouter();

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  const navLinks = {
    Student: [
      { name: 'Dashboard', href: '/student/dashboard', icon: LayoutDashboard },
    ],
    Teacher: [
      { name: 'Dashboard', href: '/teacher/dashboard', icon: LayoutDashboard },
    ],
    Admin: [
      { name: 'Dashboard', href: '/admin/dashboard', icon: LayoutDashboard },
    ],
  };

  const links = user ? navLinks[user.role as keyof typeof navLinks] || [] : [];

  return (
    <div className="min-h-screen flex bg-muted/20">
      {/* Sidebar */}
      <aside className="w-64 bg-background border-r border-border flex flex-col">
        <div className="h-16 flex items-center px-6 border-b border-border">
          <BookOpen className="w-6 h-6 text-primary mr-2" />
          <span className="font-bold text-lg tracking-tight">AssignmentSys</span>
        </div>
        
        <nav className="flex-1 py-6 px-4 space-y-1">
          {links.map((link) => {
            const isActive = pathname === link.href;
            const Icon = link.icon;
            return (
              <Link
                key={link.name}
                href={link.href}
                className={`flex items-center px-3 py-2.5 text-sm font-medium rounded-md transition-colors ${
                  isActive 
                    ? 'bg-primary/10 text-primary' 
                    : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                }`}
              >
                <Icon className={`w-5 h-5 mr-3 ${isActive ? 'text-primary' : 'text-muted-foreground'}`} />
                {link.name}
              </Link>
            );
          })}
        </nav>

        <div className="p-4 border-t border-border">
          <div className="flex items-center px-3 py-2 mb-4">
            <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-primary font-bold mr-3">
              {user?.name?.[0]?.toUpperCase() || 'U'}
            </div>
            <div className="flex flex-col">
              <span className="text-sm font-medium leading-none">{user?.name}</span>
              <span className="text-xs text-muted-foreground mt-1">{user?.role}</span>
            </div>
          </div>
          <Button variant="outline" className="w-full justify-start text-muted-foreground" onClick={handleLogout}>
            <LogOut className="w-4 h-4 mr-2" />
            Sign Out
          </Button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        <header className="h-16 bg-background border-b border-border flex items-center px-8 shadow-sm">
          <h1 className="text-lg font-medium text-foreground">
            {links.find(l => l.href === pathname)?.name || 'Dashboard'}
          </h1>
        </header>
        <div className="p-8">
          {children}
        </div>
      </main>
    </div>
  );
}
