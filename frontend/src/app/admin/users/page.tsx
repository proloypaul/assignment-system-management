'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { UserProfile, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Search, Plus, Loader2, Edit2, Trash2 } from 'lucide-react';
import { useDebounce } from '@/hooks/useDebounce';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';

type UserFormValues = {
  name: string;
  email: string;
  password?: string;
  role: 'Admin' | 'Teacher' | 'Student';
  phoneNumber?: string;
};

export default function AdminUsersPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearch = useDebounce(searchTerm, 500);
  
  const [roleFilter, setRoleFilter] = useState('');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserProfile | null>(null);
  
  const queryClient = useQueryClient();

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

  const { register, handleSubmit, reset, formState: { errors }, setValue } = useForm<UserFormValues>({
    defaultValues: {
      role: 'Student',
      email: '',
      name: '',
      password: '',
      phoneNumber: ''
    }
  });

  const createUserMutation = useMutation({
    mutationFn: async (data: UserFormValues) => {
      await api.post('/users', {
        name: data.name,
        email: data.email,
        password: data.password,
        role: data.role,
        phone: data.phoneNumber
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setIsModalOpen(false);
      reset();
      toast.success('User created successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create user');
    }
  });

  const updateUserMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string, data: Partial<UserFormValues> }) => {
      await api.put(`/users/${id}`, {
        name: data.name,
        phone: data.phoneNumber
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setIsModalOpen(false);
      reset();
      setEditingUser(null);
      toast.success('User updated successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update user');
    }
  });

  const deleteUserMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/users/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('User deleted successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete user');
    }
  });

  const onSubmit = (data: UserFormValues) => {
    if (editingUser) {
      updateUserMutation.mutate({ id: editingUser.id, data });
    } else {
      if (!data.password) {
        toast.error('Password is required for new users');
        return;
      }
      createUserMutation.mutate(data);
    }
  };

  const handleEdit = (user: UserProfile) => {
    setEditingUser(user);
    setValue('name', user.name);
    setValue('email', user.email);
    setValue('phoneNumber', user.phoneNumber || '');
    setValue('role', (user.role as any) || 'Student');
    setIsModalOpen(true);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Are you sure you want to delete this user?')) {
      deleteUserMutation.mutate(id);
    }
  };

  const openCreateModal = () => {
    setEditingUser(null);
    reset();
    setIsModalOpen(true);
  };

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
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
          item.role === 'Admin' ? 'bg-purple-100 text-purple-700' : 
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
      header: 'Actions',
      cell: (item) => (
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => handleEdit(item)}>
            <Edit2 className="w-4 h-4" />
          </Button>
          <Button variant="outline" size="sm" onClick={() => handleDelete(item.id)} className="text-red-600 hover:text-red-700">
            <Trash2 className="w-4 h-4" />
          </Button>
        </div>
      ),
    }
  ];

  return (
    <AuthGuard allowedRoles={['Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Users</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage all registered users in the system.
              </p>
            </div>
            <Button onClick={openCreateModal}>
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
                setPage(1);
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

        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title={editingUser ? 'Edit User' : 'Create New User'}>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <Input {...register('name', { required: 'Name is required' })} placeholder="Full Name" />
              {errors.name && <p className="text-sm text-red-500 mt-1">{errors.name.message}</p>}
            </div>
            
            {!editingUser && (
              <>
                <div>
                  <label className="block text-sm font-medium mb-1">Email</label>
                  <Input {...register('email', { required: 'Email is required' })} type="email" placeholder="user@example.com" />
                  {errors.email && <p className="text-sm text-red-500 mt-1">{errors.email.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Password</label>
                  <Input {...register('password')} type="password" placeholder="Password" />
                  {errors.password && <p className="text-sm text-red-500 mt-1">{errors.password.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Role</label>
                  <select {...register('role')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                    <option value="Student">Student</option>
                    <option value="Teacher">Teacher</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
              </>
            )}
            
            <div>
              <label className="block text-sm font-medium mb-1">Phone Number (Optional)</label>
              <Input {...register('phoneNumber')} placeholder="Phone Number" />
            </div>

            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createUserMutation.isPending || updateUserMutation.isPending}>
                {editingUser 
                  ? (updateUserMutation.isPending ? 'Updating...' : 'Update User') 
                  : (createUserMutation.isPending ? 'Creating...' : 'Create User')}
              </Button>
            </div>
          </form>
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
