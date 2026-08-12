'use client';

import { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useRouter, usePathname } from 'next/navigation';
import { Loader2 } from 'lucide-react';

export default function AuthGuard({ children, allowedRoles }: { children: React.ReactNode, allowedRoles?: string[] }) {
  const { isAuthenticated, user, _hasHydrated } = useAuthStore();
  const router = useRouter();
  const pathname = usePathname();
  const [isChecking, setIsChecking] = useState(true);

  useEffect(() => {
    if (!_hasHydrated) return;

    // Basic protection logic
    if (!isAuthenticated) {
      router.push(`/login?redirect=${pathname}`);
    } else if (allowedRoles && user && !allowedRoles.includes(user.role)) {
      // If user doesn't have the right role, redirect to their dashboard
      router.push(`/${user.role.toLowerCase()}/dashboard`);
    } else {
      setIsChecking(false);
    }
  }, [isAuthenticated, user, router, pathname, allowedRoles, _hasHydrated]);

  if (isChecking) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <Loader2 className="w-8 h-8 animate-spin text-primary" />
      </div>
    );
  }

  return <>{children}</>;
}
