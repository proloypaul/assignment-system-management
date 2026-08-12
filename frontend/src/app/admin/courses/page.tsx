'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Course } from '@/lib/types';
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

const courseSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  code: z.string().min(1, 'Code is required'),
  description: z.string().min(1, 'Description is required'),
  capacity: z.coerce.number().int().min(1, 'Capacity must be at least 1'),
  startDate: z.string().min(1, 'Start date is required'),
  endDate: z.string().min(1, 'End date is required'),
});

type CourseFormValues = {
  name: string;
  code: string;
  description: string;
  capacity: number;
  startDate: string;
  endDate: string;
};

export default function AdminCoursesPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isEnrollModalOpen, setIsEnrollModalOpen] = useState(false);
  const [selectedCourseId, setSelectedCourseId] = useState<string | null>(null);
  const [studentId, setStudentId] = useState('');
  
  const queryClient = useQueryClient();

  const { data: courses, isLoading } = useQuery({
    queryKey: queryKeys.courses.all(),
    queryFn: async () => {
      const response = await api.get<Course[]>('/courses');
      return response.data;
    }
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<CourseFormValues>({
    resolver: zodResolver(courseSchema) as any,
  });

  const createCourseMutation = useMutation({
    mutationFn: async (data: CourseFormValues) => {
      await api.post('/courses', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.all() });
      setIsModalOpen(false);
      reset();
      alert('Course created successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Failed to create course');
    }
  });

  const enrollMutation = useMutation({
    mutationFn: async ({ courseId, studentId }: { courseId: string, studentId: string }) => {
      await api.post(`/courses/${courseId}/enroll-student`, { studentId });
    },
    onSuccess: () => {
      setIsEnrollModalOpen(false);
      setStudentId('');
      alert('Student enrolled successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Failed to enroll student');
    }
  });

  const onSubmit = (data: CourseFormValues) => {
    createCourseMutation.mutate(data);
  };

  const handleEnroll = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedCourseId && studentId) {
      enrollMutation.mutate({ courseId: selectedCourseId, studentId });
    }
  };

  const columns: Column<Course>[] = [
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
      header: 'Capacity',
      accessorKey: 'capacity',
    },
    {
      header: 'Start Date',
      cell: (item) => new Date(item.startDate).toLocaleDateString(),
    },
    {
      header: 'Status',
      cell: (item) => (
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
          item.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
        }`}>
          {item.isActive ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      header: 'Action',
      cell: (item) => (
        <div className="space-x-2">
          <Button size="sm" variant="outline" onClick={() => {
            setSelectedCourseId(item.id);
            setIsEnrollModalOpen(true);
          }}>
            Enroll Student
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
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Courses</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage academic courses and enrollments.
              </p>
            </div>
            <Button onClick={() => setIsModalOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Add Course
            </Button>
          </div>

          <DataTable
            data={courses || []}
            columns={columns}
            isLoading={isLoading}
            page={1}
            totalPages={1}
            onPageChange={() => {}}
            emptyMessage="No courses found."
          />
        </div>

        {/* Create Course Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Course">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <Input {...register('name')} placeholder="Course Name" />
              {errors.name && <p className="text-sm text-red-500 mt-1">{errors.name.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Code</label>
              <Input {...register('code')} placeholder="CS101" />
              {errors.code && <p className="text-sm text-red-500 mt-1">{errors.code.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Description</label>
              <Input {...register('description')} placeholder="Description" />
              {errors.description && <p className="text-sm text-red-500 mt-1">{errors.description.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Capacity</label>
              <Input {...register('capacity')} type="number" placeholder="50" />
              {errors.capacity && <p className="text-sm text-red-500 mt-1">{errors.capacity.message}</p>}
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
              <Button type="submit" disabled={createCourseMutation.isPending}>
                {createCourseMutation.isPending ? 'Creating...' : 'Create Course'}
              </Button>
            </div>
          </form>
        </Modal>

        {/* Enroll Student Modal */}
        <Modal isOpen={isEnrollModalOpen} onClose={() => setIsEnrollModalOpen(false)} title="Enroll Student">
          <form onSubmit={handleEnroll} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Student ID (UUID)</label>
              <Input value={studentId} onChange={e => setStudentId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
            </div>
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsEnrollModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={enrollMutation.isPending}>
                {enrollMutation.isPending ? 'Enrolling...' : 'Enroll'}
              </Button>
            </div>
          </form>
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
