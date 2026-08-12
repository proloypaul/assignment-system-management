'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Subject, Course } from '@/lib/types';
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

const subjectSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  code: z.string().min(1, 'Code is required'),
  credits: z.coerce.number().int().min(1, 'Credits must be at least 1'),
  courseId: z.string().min(1, 'Course ID is required'),
});

type SubjectFormValues = {
  name: string;
  code: string;
  credits: number;
  courseId: string;
};

export default function AdminSubjectsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [selectedSubjectId, setSelectedSubjectId] = useState<string | null>(null);
  const [teacherId, setTeacherId] = useState('');
  
  const queryClient = useQueryClient();

  const { data: subjects, isLoading } = useQuery({
    queryKey: queryKeys.subjects.all(),
    queryFn: async () => {
      const response = await api.get<Subject[]>('/subjects');
      return response.data;
    }
  });

  const { data: courses } = useQuery({
    queryKey: queryKeys.courses.all(),
    queryFn: async () => {
      const response = await api.get<Course[]>('/courses');
      return response.data;
    }
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<SubjectFormValues>({
    resolver: zodResolver(subjectSchema) as any,
  });

  const createSubjectMutation = useMutation({
    mutationFn: async (data: SubjectFormValues) => {
      await api.post('/subjects', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.subjects.all() });
      setIsModalOpen(false);
      reset();
      alert('Subject created successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Failed to create subject');
    }
  });

  const assignMutation = useMutation({
    mutationFn: async ({ subjectId, teacherId }: { subjectId: string, teacherId: string }) => {
      await api.post(`/subjects/${subjectId}/assign-teacher`, { teacherId });
    },
    onSuccess: () => {
      setIsAssignModalOpen(false);
      setTeacherId('');
      alert('Teacher assigned successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Failed to assign teacher');
    }
  });

  const onSubmit = (data: SubjectFormValues) => {
    createSubjectMutation.mutate(data);
  };

  const handleAssign = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedSubjectId && teacherId) {
      assignMutation.mutate({ subjectId: selectedSubjectId, teacherId });
    }
  };

  const columns: Column<Subject>[] = [
    {
      header: 'Code',
      accessorKey: 'code',
      cell: (item) => <span className="font-bold">{item.code}</span>,
    },
    {
      header: 'Name',
      accessorKey: 'name',
    },
    {
      header: 'Action',
      cell: (item) => (
        <div className="space-x-2">
          <Button size="sm" variant="outline" onClick={() => {
            setSelectedSubjectId(item.id);
            setIsAssignModalOpen(true);
          }}>
            Assign Teacher
          </Button>
        </div>
      ),
    },
  ];

  return (
    <AuthGuard allowedRoles={['Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Subjects</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage subjects and assign teachers.
              </p>
            </div>
            <Button onClick={() => setIsModalOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Add Subject
            </Button>
          </div>

          <DataTable
            data={subjects || []}
            columns={columns}
            isLoading={isLoading}
            page={1}
            totalPages={1}
            onPageChange={() => {}}
            emptyMessage="No subjects found."
          />
        </div>

        {/* Create Subject Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Subject">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <Input {...register('name')} placeholder="Subject Name" />
              {errors.name && <p className="text-sm text-red-500 mt-1">{errors.name.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Code</label>
              <Input {...register('code')} placeholder="CS101" />
              {errors.code && <p className="text-sm text-red-500 mt-1">{errors.code.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Credits</label>
              <Input {...register('credits')} type="number" placeholder="3" />
              {errors.credits && <p className="text-sm text-red-500 mt-1">{errors.credits.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Course</label>
              <select {...register('courseId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50">
                <option value="">Select a Course</option>
                {courses?.map(course => (
                  <option key={course.id} value={course.id}>{course.name}</option>
                ))}
              </select>
              {errors.courseId && <p className="text-sm text-red-500 mt-1">{errors.courseId.message}</p>}
            </div>
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createSubjectMutation.isPending}>
                {createSubjectMutation.isPending ? 'Creating...' : 'Create Subject'}
              </Button>
            </div>
          </form>
        </Modal>

        {/* Assign Teacher Modal */}
        <Modal isOpen={isAssignModalOpen} onClose={() => setIsAssignModalOpen(false)} title="Assign Teacher">
          <form onSubmit={handleAssign} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Teacher ID (UUID)</label>
              <Input value={teacherId} onChange={e => setTeacherId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
            </div>
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsAssignModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={assignMutation.isPending}>
                {assignMutation.isPending ? 'Assigning...' : 'Assign'}
              </Button>
            </div>
          </form>
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
