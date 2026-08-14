'use client';

import { useState, useEffect } from 'react';
import { useAuthStore } from '@/store/authStore';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { BookOpen, LogOut, LayoutDashboard, FileText, Users, Settings, Menu, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from './ui/Button';

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuthStore();
  const pathname = usePathname();
  const router = useRouter();
  
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [isDesktopCollapsed, setIsDesktopCollapsed] = useState(false);

  // Close mobile sidebar on navigation
  useEffect(() => {
    setIsMobileOpen(false);
  }, [pathname]);

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  const navLinks = {
    Student: [
      { name: 'Dashboard', href: '/student/dashboard', icon: LayoutDashboard },
      { name: 'Courses', href: '/student/courses', icon: BookOpen },
      { name: 'My Assignments', href: '/student/assignments', icon: BookOpen },
      { name: 'My Submissions', href: '/student/submissions', icon: FileText },
    ],
    Teacher: [
      { name: 'Dashboard', href: '/teacher/dashboard', icon: LayoutDashboard },
      { name: 'Assignments', href: '/teacher/assignments', icon: BookOpen },
    ],
    Admin: [
      { name: 'Dashboard', href: '/admin/dashboard', icon: LayoutDashboard },
      { name: 'Users', href: '/admin/users', icon: Users },
      { name: 'Courses', href: '/admin/courses', icon: BookOpen },
      { name: 'Subjects', href: '/admin/subjects', icon: BookOpen },
      { name: 'Assignments', href: '/admin/assignments', icon: FileText },
    ],
  };

  const links = user ? navLinks[user.role as keyof typeof navLinks] || [] : [];

  return (
    <div className="min-h-screen flex bg-muted/20 relative">
      
      {/* Mobile Backdrop */}
      {isMobileOpen && (
        <div 
          className="fixed inset-0 bg-black/50 z-40 md:hidden" 
          onClick={() => setIsMobileOpen(false)} 
        />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed inset-y-0 left-0 z-50 bg-background border-r border-border flex flex-col transition-all duration-300 ease-in-out
        ${isMobileOpen ? 'translate-x-0' : '-translate-x-full'} 
        md:relative md:translate-x-0
        ${isDesktopCollapsed ? 'md:w-20' : 'md:w-64'}
        w-64
      `}>
        <div className={`h-16 flex items-center border-b border-border relative ${isDesktopCollapsed ? 'justify-center px-0' : 'px-6'}`}>
          <BookOpen className="w-6 h-6 text-primary shrink-0" />
          {!isDesktopCollapsed && (
            <span className="font-bold text-lg tracking-tight ml-2">AssignmentSys</span>
          )}
          
          {/* Desktop Collapse Toggle */}
          <button 
            onClick={() => setIsDesktopCollapsed(!isDesktopCollapsed)}
            className="absolute -right-3 top-5 hidden md:flex items-center justify-center w-6 h-6 bg-background border border-border rounded-full hover:bg-muted text-muted-foreground z-10"
          >
            {isDesktopCollapsed ? <ChevronRight className="w-4 h-4" /> : <ChevronLeft className="w-4 h-4" />}
          </button>
        </div>
        
        <nav className="flex-1 py-6 px-4 space-y-1">
          {links.map((link) => {
            const isActive = pathname === link.href;
            const Icon = link.icon;
            return (
              <Link
                key={link.name}
                href={link.href}
                className={`flex items-center py-2.5 text-sm font-medium rounded-md transition-colors ${
                  isDesktopCollapsed ? 'justify-center px-0' : 'px-3'
                } ${
                  isActive 
                    ? 'bg-primary/10 text-primary' 
                    : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                }`}
                title={isDesktopCollapsed ? link.name : undefined}
              >
                <Icon className={`w-5 h-5 ${isDesktopCollapsed ? '' : 'mr-3'} ${isActive ? 'text-primary' : 'text-muted-foreground'}`} />
                {!isDesktopCollapsed && <span>{link.name}</span>}
              </Link>
            );
          })}
        </nav>

        <div className={`p-4 border-t border-border flex flex-col ${isDesktopCollapsed ? 'items-center' : ''}`}>
          <div className={`flex items-center mb-4 ${isDesktopCollapsed ? 'justify-center' : 'px-3 py-2'}`}>
            <div className={`w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-primary font-bold shrink-0 ${isDesktopCollapsed ? '' : 'mr-3'}`}>
              {user?.name?.[0]?.toUpperCase() || 'U'}
            </div>
            {!isDesktopCollapsed && (
              <div className="flex flex-col overflow-hidden">
                <span className="text-sm font-medium leading-none truncate">{user?.name}</span>
                <span className="text-xs text-muted-foreground mt-1 truncate">{user?.role}</span>
              </div>
            )}
          </div>
          <Button 
            variant="outline" 
            className={`w-full text-muted-foreground ${isDesktopCollapsed ? 'px-0 justify-center' : 'justify-start'}`} 
            onClick={handleLogout}
            title={isDesktopCollapsed ? 'Sign Out' : undefined}
          >
            <LogOut className={`w-4 h-4 ${isDesktopCollapsed ? '' : 'mr-2'}`} />
            {!isDesktopCollapsed && <span>Sign Out</span>}
          </Button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto flex flex-col min-w-0">
        <header className="h-16 bg-background border-b border-border flex items-center px-4 md:px-8 shadow-sm shrink-0">
          <button 
            className="md:hidden mr-4 text-muted-foreground hover:text-foreground"
            onClick={() => setIsMobileOpen(true)}
          >
            <Menu className="w-6 h-6" />
          </button>
          <h1 className="text-lg font-medium text-foreground truncate">
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
