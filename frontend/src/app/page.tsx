import { redirect } from 'next/navigation';

export default function Home() {
  // Redirect root to dashboard, which will be protected by AuthGuard and bounce unauthenticated users to /login
  redirect('/dashboard');
}
