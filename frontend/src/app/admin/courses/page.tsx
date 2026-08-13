'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Course, UserProfile, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Plus, Edit2, Trash2 } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';

type CourseFormValues = {
  name: string;
  code?: string;
  description: string;
  capacity: number;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
};

export default function AdminCoursesPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isEnrollModalOpen, setIsEnrollModalOpen] = useState(false);
  const [selectedCourseId, setSelectedCourseId] = useState<string | null>(null);
  const [studentId, setStudentId] = useState('');
  
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);

  const queryClient = useQueryClient();
  const router = useRouter();

  const { data: courses, isLoading } = useQuery({
    queryKey: queryKeys.courses.all(),
    queryFn: async () => {
      const response = await api.get<Course[]>('/courses');
      return response.data;
    }
  });

  const { data: studentsData } = useQuery({
    queryKey: ['users', 1, 1000, '', 'Student'],
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<UserProfile>>('/users', {
        params: { page: 1, pageSize: 1000, role: 'Student' }
      });
      return response.data;
    }
  });

  const { register, handleSubmit, reset, formState: { errors }, setValue } = useForm<CourseFormValues>();

  const createCourseMutation = useMutation({
    mutationFn: async (data: CourseFormValues) => {
      await api.post('/courses', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.all() });
      setIsModalOpen(false);
      reset();
      toast.success('Course created successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create course');
    }
  });

  const updateCourseMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string, data: Partial<CourseFormValues> }) => {
      await api.put(`/courses/${id}`, {
        name: data.name,
        description: data.description,
        capacity: data.capacity,
        isActive: data.isActive
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.all() });
      setIsModalOpen(false);
      reset();
      setEditingCourse(null);
      toast.success('Course updated successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update course');
    }
  });

  const deleteCourseMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/courses/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.all() });
      toast.success('Course deleted successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete course');
    }
  });

  const enrollMutation = useMutation({
    mutationFn: async ({ courseId, studentId }: { courseId: string, studentId: string }) => {
      await api.post(`/courses/${courseId}/enroll-student`, { studentId });
    },
    onSuccess: () => {
      setIsEnrollModalOpen(false);
      setStudentId('');
      toast.success('Student enrolled successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to enroll student');
    }
  });

  const onSubmit = (data: CourseFormValues) => {
    if (editingCourse) {
      updateCourseMutation.mutate({ id: editingCourse.id, data });
    } else {
      if (!data.code || !data.startDate || !data.endDate) {
        toast.error('Code, start date, and end date are required for new courses.');
        return;
      }
      createCourseMutation.mutate(data);
    }
  };

  const handleEdit = (course: Course) => {
    setEditingCourse(course);
    setValue('name', course.name);
    setValue('description', course.description || '');
    setValue('capacity', course.capacity);
    setValue('isActive', course.isActive);
    setIsModalOpen(true);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Are you sure you want to delete this course?')) {
      deleteCourseMutation.mutate(id);
    }
  };

  const openCreateModal = () => {
    setEditingCourse(null);
    reset();
    setIsModalOpen(true);
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
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" onClick={() => {
            setSelectedCourseId(item.id);
            setIsEnrollModalOpen(true);
          }}>
            Enroll Student
          </Button>
          <Button size="sm" variant="outline" onClick={() => router.push(`/admin/courses/${item.id}/enrollments`)}>
            View Enrollments
          </Button>
          <Button variant="outline" size="sm" onClick={() => handleEdit(item)}>
            <Edit2 className="w-4 h-4" />
          </Button>
          <Button variant="outline" size="sm" onClick={() => handleDelete(item.id)} className="text-red-600 hover:text-red-700">
            <Trash2 className="w-4 h-4" />
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
            <Button onClick={openCreateModal}>
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

        {/* Create / Edit Course Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title={editingCourse ? 'Edit Course' : 'Create New Course'}>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <Input {...register('name', { required: 'Name is required' })} placeholder="Course Name" />
              {errors.name && <p className="text-sm text-red-500 mt-1">{errors.name.message}</p>}
            </div>
            
            {!editingCourse && (
              <div>
                <label className="block text-sm font-medium mb-1">Code</label>
                <Input {...register('code')} placeholder="CS101" />
              </div>
            )}
            
            <div>
              <label className="block text-sm font-medium mb-1">Description</label>
              <Input {...register('description', { required: 'Description is required' })} placeholder="Description" />
              {errors.description && <p className="text-sm text-red-500 mt-1">{errors.description.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Capacity</label>
              <Input {...register('capacity', { required: 'Capacity is required', min: 1 })} type="number" placeholder="50" />
              {errors.capacity && <p className="text-sm text-red-500 mt-1">{errors.capacity.message}</p>}
            </div>
            
            {!editingCourse && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Start Date</label>
                  <Input {...register('startDate')} type="date" />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">End Date</label>
                  <Input {...register('endDate')} type="date" />
                </div>
              </div>
            )}

            {editingCourse && (
               <div className="flex items-center gap-2">
                 <input type="checkbox" id="isActive" {...register('isActive')} />
                 <label htmlFor="isActive" className="text-sm font-medium">Is Active</label>
               </div>
            )}
            
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createCourseMutation.isPending || updateCourseMutation.isPending}>
                {editingCourse 
                  ? (updateCourseMutation.isPending ? 'Updating...' : 'Update Course') 
                  : (createCourseMutation.isPending ? 'Creating...' : 'Create Course')}
              </Button>
            </div>
          </form>
        </Modal>

        {/* Enroll Student Modal */}
        <Modal isOpen={isEnrollModalOpen} onClose={() => setIsEnrollModalOpen(false)} title="Enroll Student">
          <form onSubmit={handleEnroll} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Select Student</label>
              <select 
                value={studentId} 
                onChange={e => setStudentId(e.target.value)} 
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                required
              >
                <option value="">Choose a student...</option>
                {studentsData?.items?.map(student => (
                  <option key={student.id} value={student.id}>{student.name} ({student.email})</option>
                ))}
              </select>
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
