'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { UserProfile, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Search, Plus, Loader2 } from 'lucide-react';
import { useDebounce } from '@/hooks/useDebounce';
// import { useDebounce } from '@/hooks/useDebounce';

export default function AdminDashboard() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearch = useDebounce(searchTerm, 500);

  const [roleFilter, setRoleFilter] = useState('');

  const { data, isLoading, isFetching } = useQuery({
    queryKey: queryKeys.users.all(page, pageSize, debouncedSearch, roleFilter),
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<UserProfile>>('/users', {
        params: {
          page,
          pageSize,
          search: debouncedSearch || undefined,
          role: roleFilter || undefined
        }
      });
      return response.data;
    }
  });

  const columns: Column<UserProfile>[] = [
    {
      header: 'Name',
      accessorKey: 'name',
      cell: (item) => <span className="font-medium">{item.name}</span>,
    },
    {
      header: 'Email',
      accessorKey: 'email',
    },
    {
      header: 'Phone',
      cell: (item) => item.phoneNumber || '-',
    },
    {
      header: 'Role',
      cell: (item) => (
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${item.role === 'Admin' ? 'bg-purple-100 text-purple-700' :
            item.role === 'Teacher' ? 'bg-blue-100 text-blue-700' :
              'bg-gray-100 text-gray-700'
          }`}>
          {item.role}
        </span>
      ),
    },
    {
      header: 'Created',
      cell: (item) => new Date(item.createdAt).toLocaleDateString(),
    },
    {
      header: 'Action',
      cell: (item) => (
        <Button size="sm" variant="outline" onClick={() => alert(`Edit user ${item.id}`)}>
          Edit
        </Button>
      ),
    },
  ];

  return (
    <AuthGuard allowedRoles={['Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">User Management</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage all registered users in the system.
              </p>
            </div>
            <Button onClick={() => alert('Open Create User Modal')}>
              <Plus className="w-4 h-4 mr-2" />
              Add User
            </Button>
          </div>

          <div className="flex items-center gap-4 bg-background p-4 rounded-lg border border-border shadow-sm">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by name, email, or phone..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-9"
              />
            </div>

            <select
              value={roleFilter}
              onChange={(e) => {
                setRoleFilter(e.target.value);
                setPage(1); // Reset page on filter change
              }}
              className="h-10 rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="">All Roles</option>
              <option value="Student">Student</option>
              <option value="Teacher">Teacher</option>
              <option value="Admin">Admin</option>
            </select>

            {isFetching && <Loader2 className="w-4 h-4 animate-spin text-muted-foreground ml-2" />}
          </div>

          <DataTable
            data={data?.items || []}
            columns={columns}
            isLoading={isLoading}
            page={page}
            totalPages={data?.totalPages || 1}
            onPageChange={setPage}
            emptyMessage="No users found matching your criteria."
          />
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
