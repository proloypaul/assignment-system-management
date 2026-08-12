'use client';

import { Suspense } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useMutation } from '@tanstack/react-query';
import api from '@/lib/api';
import { useAuthStore, User } from '@/store/authStore';
import { useRouter, useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Loader2, BookOpen } from 'lucide-react';
import { toast } from 'sonner';

const loginSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

function LoginContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectUrl = searchParams.get('redirect') || '/dashboard';
  const loginToStore = useAuthStore((state) => state.login);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const loginMutation = useMutation({
    mutationFn: async (data: LoginFormValues) => {
      await api.post('/auth/login', data);
      
      const mockedUser: User = {
        id: 'user-id',
        email: data.email,
        name: data.email.split('@')[0],
        role: data.email.includes('admin') ? 'Admin' : data.email.includes('teacher') ? 'Teacher' : 'Student',
      };
      
      return mockedUser;
    },
    onSuccess: (user) => {
      loginToStore(user);
      toast.success('Successfully logged in');
      router.push(redirectUrl);
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Login failed. Please check your credentials.';
      toast.error(message);
    },
  });

  const onSubmit = (data: LoginFormValues) => {
    loginMutation.mutate(data);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted p-4">
      <div className="w-full max-w-md bg-background rounded-xl shadow-xl overflow-hidden border border-border">
        <div className="p-8 text-center bg-primary text-primary-foreground">
          <BookOpen className="w-12 h-12 mx-auto mb-4 opacity-90" />
          <h1 className="text-2xl font-bold tracking-tight">Assignment System</h1>
          <p className="text-primary-foreground/80 mt-2 text-sm">Sign in to your account</p>
        </div>
        
        <div className="p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">Email</label>
              <Input
                type="email"
                placeholder="you@example.com"
                {...register('email')}
                className={errors.email ? 'border-destructive focus-visible:ring-destructive' : ''}
              />
              {errors.email && (
                <p className="text-xs text-destructive">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium text-foreground">Password</label>
                <a href="#" className="text-xs text-primary hover:underline">Forgot password?</a>
              </div>
              <Input
                type="password"
                placeholder="••••••••"
                {...register('password')}
                className={errors.password ? 'border-destructive focus-visible:ring-destructive' : ''}
              />
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password.message}</p>
              )}
            </div>

            <Button
              type="submit"
              className="w-full h-11 mt-6 text-base"
              disabled={loginMutation.isPending}
            >
              {loginMutation.isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Signing in...
                </>
              ) : (
                'Sign In'
              )}
            </Button>
          </form>
        </div>
        
        <div className="p-6 text-center border-t border-border bg-muted/50">
          <p className="text-sm text-muted-foreground">
            Don't have an account? <a href="#" className="text-primary font-medium hover:underline">Contact Administrator</a>
          </p>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center bg-muted"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>}>
      <LoginContent />
    </Suspense>
  );
}

