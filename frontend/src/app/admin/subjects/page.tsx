'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Subject, Course, UserProfile, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Plus, Edit2, Trash2 } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';

type SubjectFormValues = {
  name: string;
  code?: string;
  credits: number;
  courseId?: string;
  syllabusUrl?: string;
};

export default function AdminSubjectsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [selectedSubjectId, setSelectedSubjectId] = useState<string | null>(null);
  const [teacherId, setTeacherId] = useState('');
  
  const [editingSubject, setEditingSubject] = useState<Subject | null>(null);

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

  const { data: teachersData } = useQuery({
    queryKey: ['users', 1, 1000, '', 'Teacher'],
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<UserProfile>>('/users', {
        params: { page: 1, pageSize: 1000, role: 'Teacher' }
      });
      return response.data;
    }
  });

  const { register, handleSubmit, reset, formState: { errors }, setValue } = useForm<SubjectFormValues>();

  const createSubjectMutation = useMutation({
    mutationFn: async (data: SubjectFormValues) => {
      await api.post('/subjects', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.subjects.all() });
      setIsModalOpen(false);
      reset();
      toast.success('Subject created successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create subject');
    }
  });

  const updateSubjectMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string, data: Partial<SubjectFormValues> }) => {
      await api.put(`/subjects/${id}`, {
        name: data.name,
        credits: data.credits,
        syllabusUrl: data.syllabusUrl
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.subjects.all() });
      setIsModalOpen(false);
      reset();
      setEditingSubject(null);
      toast.success('Subject updated successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update subject');
    }
  });

  const deleteSubjectMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/subjects/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.subjects.all() });
      toast.success('Subject deleted successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete subject');
    }
  });

  const assignMutation = useMutation({
    mutationFn: async ({ subjectId, teacherId }: { subjectId: string, teacherId: string }) => {
      await api.post(`/subjects/${subjectId}/assign-teacher`, { teacherId });
    },
    onSuccess: () => {
      setIsAssignModalOpen(false);
      setTeacherId('');
      toast.success('Teacher assigned successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to assign teacher');
    }
  });

  const onSubmit = (data: SubjectFormValues) => {
    if (editingSubject) {
      updateSubjectMutation.mutate({ id: editingSubject.id, data });
    } else {
      if (!data.code || !data.courseId) {
        toast.error('Code and Course are required for new subjects.');
        return;
      }
      createSubjectMutation.mutate(data);
    }
  };

  const handleEdit = (subject: Subject) => {
    setEditingSubject(subject);
    setValue('name', subject.name);
    setValue('credits', subject.credits);
    setValue('syllabusUrl', subject.syllabusUrl || '');
    setIsModalOpen(true);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Are you sure you want to delete this subject?')) {
      deleteSubjectMutation.mutate(id);
    }
  };

  const openCreateModal = () => {
    setEditingSubject(null);
    reset();
    setIsModalOpen(true);
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
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" onClick={() => {
            setSelectedSubjectId(item.id);
            setIsAssignModalOpen(true);
          }}>
            Assign Teacher
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
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Subjects</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage subjects and assign teachers.
              </p>
            </div>
            <Button onClick={openCreateModal}>
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

        {/* Create / Edit Subject Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title={editingSubject ? 'Edit Subject' : 'Create New Subject'}>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Name</label>
              <Input {...register('name', { required: 'Name is required' })} placeholder="Subject Name" />
              {errors.name && <p className="text-sm text-red-500 mt-1">{errors.name.message}</p>}
            </div>
            
            {!editingSubject && (
              <div>
                <label className="block text-sm font-medium mb-1">Code</label>
                <Input {...register('code')} placeholder="CS101" />
              </div>
            )}
            
            <div>
              <label className="block text-sm font-medium mb-1">Credits</label>
              <Input {...register('credits', { required: 'Credits is required', min: 1 })} type="number" placeholder="3" />
              {errors.credits && <p className="text-sm text-red-500 mt-1">{errors.credits.message}</p>}
            </div>
            
            {!editingSubject && (
              <div>
                <label className="block text-sm font-medium mb-1">Course</label>
                <select {...register('courseId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                  <option value="">Select a Course</option>
                  {courses?.map(course => (
                    <option key={course.id} value={course.id}>{course.name}</option>
                  ))}
                </select>
              </div>
            )}
            
            <div>
              <label className="block text-sm font-medium mb-1">Syllabus URL (Optional)</label>
              <Input {...register('syllabusUrl')} placeholder="https://example.com/syllabus" />
            </div>

            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createSubjectMutation.isPending || updateSubjectMutation.isPending}>
                {editingSubject 
                  ? (updateSubjectMutation.isPending ? 'Updating...' : 'Update Subject') 
                  : (createSubjectMutation.isPending ? 'Creating...' : 'Create Subject')}
              </Button>
            </div>
          </form>
        </Modal>

        {/* Assign Teacher Modal */}
        <Modal isOpen={isAssignModalOpen} onClose={() => setIsAssignModalOpen(false)} title="Assign Teacher">
          <form onSubmit={handleAssign} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Select Teacher</label>
              <select 
                value={teacherId} 
                onChange={e => setTeacherId(e.target.value)} 
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                required
              >
                <option value="">Choose a teacher...</option>
                {teachersData?.items?.map(teacher => (
                  <option key={teacher.id} value={teacher.id}>{teacher.name} ({teacher.email})</option>
                ))}
              </select>
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
