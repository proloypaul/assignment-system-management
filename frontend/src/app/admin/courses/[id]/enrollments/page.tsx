'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';
import { useParams, useRouter } from 'next/navigation';
import { Trash2 } from 'lucide-react';

interface Enrollment {
  studentId: string;
  studentName: string;
  studentEmail: string;
  enrolledAt: string;
}

export default function AdminCourseEnrollmentsPage() {
  const { id: courseId } = useParams() as { id: string };
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: enrollments, isLoading } = useQuery({
    queryKey: ['course-enrollments', courseId],
    queryFn: async () => {
      const response = await api.get<Enrollment[]>(`/courses/${courseId}/enrollments`);
      return response.data;
    }
  });

  const removeMutation = useMutation({
    mutationFn: async (studentId: string) => {
      await api.delete(`/courses/${courseId}/enrollments/${studentId}`);
    },
    onSuccess: () => {
      toast.success('Student removed from course successfully');
      queryClient.invalidateQueries({ queryKey: ['course-enrollments', courseId] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to remove student');
    }
  });

  const columns: Column<Enrollment>[] = [
    {
      header: 'Student Name',
      accessorKey: 'studentName',
      cell: (item) => <span className="font-bold">{item.studentName}</span>,
    },
    {
      header: 'Email',
      accessorKey: 'studentEmail',
    },
    {
      header: 'Enrolled At',
      cell: (item) => new Date(item.enrolledAt).toLocaleString(),
    },
    {
      header: 'Action',
      cell: (item) => (
        <Button 
          variant="outline" 
          size="sm" 
          onClick={() => {
            if (window.confirm('Are you sure you want to remove this student from the course?')) {
              removeMutation.mutate(item.studentId);
            }
          }} 
          className="text-red-600 hover:text-red-700"
        >
          <Trash2 className="w-4 h-4 mr-2" />
          Remove
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
              <Button variant="outline" size="sm" onClick={() => router.push('/admin/courses')} className="mb-4">
                &larr; Back to Courses
              </Button>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Enrolled Students</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage students enrolled in this course.
              </p>
            </div>
          </div>

          <DataTable
            data={enrollments || []}
            columns={columns}
            isLoading={isLoading}
            page={1}
            totalPages={1}
            onPageChange={() => {}}
            emptyMessage="No students are enrolled in this course."
          />
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
