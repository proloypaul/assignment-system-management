import { redirect } from 'next/navigation';

export default function Home() {
  // Redirect root to login; login page will redirect authenticated users to their dashboard
  redirect('/login');
}

