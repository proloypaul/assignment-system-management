'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Assignment, PaginatedResponse, Subject } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Plus } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';

const assignmentSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().min(1, 'Description is required'),
  startDate: z.string().min(1, 'Start Date is required'),
  endDate: z.string().min(1, 'End Date is required'),
  maxMarks: z.coerce.number().int().min(1, 'Max marks must be greater than 0'),
  subjectId: z.string().min(1, 'Subject is required'),
});

type AssignmentFormValues = {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  maxMarks: number;
  subjectId: string;
};

export default function TeacherAssignmentsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const [isModalOpen, setIsModalOpen] = useState(false);
  const queryClient = useQueryClient();
  const router = useRouter();

  // In a real application, you'd filter this to ONLY show the teacher's assignments, but for now we'll fetch all or rely on a generic endpoint
  const { data, isLoading } = useQuery({
    queryKey: queryKeys.assignments.all(page, pageSize),
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<Assignment>>('/assignments', {
        params: { page, pageSize }
      });
      return response.data;
    }
  });

  const { data: subjects } = useQuery({
    queryKey: queryKeys.subjects.all(),
    queryFn: async () => {
      const response = await api.get<Subject[]>('/subjects');
      return response.data;
    }
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<AssignmentFormValues>({
    resolver: zodResolver(assignmentSchema) as any,
  });

  const createMutation = useMutation({
    mutationFn: async (data: AssignmentFormValues) => {
      await api.post('/assignments', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assignments'] });
      setIsModalOpen(false);
      reset();
      toast.success('Assignment created successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create assignment');
    }
  });

  const publishMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.put(`/assignments/${id}/publish`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assignments'] });
    }
  });

  const onSubmit = (data: AssignmentFormValues) => {
    createMutation.mutate(data);
  };

  const columns: Column<Assignment>[] = [
    {
      header: 'Title',
      accessorKey: 'title',
      cell: (item) => <span className="font-medium">{item.title}</span>,
    },
    {
      header: 'Subject',
      cell: (item) => item.subject?.name || 'N/A',
    },
    {
      header: 'Deadline',
      cell: (item) => new Date(item.endDate).toLocaleDateString(),
    },
    {
      header: 'Status',
      cell: (item) => (
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
          item.status === 'Published' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
        }`}>
          {item.status}
        </span>
      ),
    },
    {
      header: 'Action',
      cell: (item) => (
        <div className="space-x-2">
          {item.status !== 'Published' && (
            <Button size="sm" variant="outline" onClick={() => publishMutation.mutate(item.id)}>
              Publish
            </Button>
          )}
          <Button size="sm" onClick={() => router.push(`/teacher/assignments/${item.id}/submissions`)}>
            View Submissions
          </Button>
        </div>
      ),
    },
  ];

  return (
    <AuthGuard allowedRoles={['Teacher', 'Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Assignments</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage your assignments.
              </p>
            </div>
            <Button onClick={() => setIsModalOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Create Assignment
            </Button>
          </div>

          <DataTable
            data={data?.items || []}
            columns={columns}
            isLoading={isLoading}
            page={page}
            totalPages={data?.totalPages || 1}
            onPageChange={setPage}
            emptyMessage="You haven't created any assignments."
          />
        </div>

        {/* Create Assignment Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Assignment">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Title</label>
              <Input {...register('title')} placeholder="Assignment Title" />
              {errors.title && <p className="text-sm text-red-500 mt-1">{errors.title.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Description</label>
              <Input {...register('description')} placeholder="Detailed description..." />
              {errors.description && <p className="text-sm text-red-500 mt-1">{errors.description.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Subject</label>
              <select {...register('subjectId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50">
                <option value="">Select a Subject</option>
                {subjects?.map(sub => (
                  <option key={sub.id} value={sub.id}>{sub.name}</option>
                ))}
              </select>
              {errors.subjectId && <p className="text-sm text-red-500 mt-1">{errors.subjectId.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Max Marks</label>
              <Input {...register('maxMarks')} type="number" placeholder="100" />
              {errors.maxMarks && <p className="text-sm text-red-500 mt-1">{errors.maxMarks.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">Start Date</label>
                <Input {...register('startDate')} type="date" />
                {errors.startDate && <p className="text-sm text-red-500 mt-1">{errors.startDate.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">End Date</label>
                <Input {...register('endDate')} type="date" />
                {errors.endDate && <p className="text-sm text-red-500 mt-1">{errors.endDate.message}</p>}
              </div>
            </div>
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? 'Creating...' : 'Create Draft'}
              </Button>
            </div>
          </form>
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
