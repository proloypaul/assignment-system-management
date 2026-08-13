'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Course, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

export default function StudentCoursesPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: queryKeys.courses.all(page, pageSize),
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<Course>>('/courses', {
        params: { page, pageSize }
      });
      return response.data;
    }
  });

  const enrollMutation = useMutation({
    mutationFn: async (courseId: string) => {
      await api.post(`/courses/${courseId}/enroll`);
    },
    onSuccess: () => {
      toast.success('Successfully enrolled in course!');
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.all(page, pageSize) });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to enroll in course');
    }
  });

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
      header: 'Time Period',
      cell: (item) => (
        <span className="text-sm">
          {new Date(item.startDate).toLocaleDateString()} - {new Date(item.endDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      header: 'Action',
      cell: (item) => {
        const now = new Date();
        const startDate = new Date(item.startDate);
        const endDate = new Date(item.endDate);
        const isActive = now >= startDate && now <= endDate;
        
        if (item.isEnrolled) {
          return (
            <span className="px-3 py-1.5 text-xs font-medium bg-green-100 text-green-700 rounded-md">
              Enrolled
            </span>
          );
        }

        return (
          <Button 
            size="sm" 
            variant="outline" 
            disabled={!isActive || enrollMutation.isPending}
            onClick={() => enrollMutation.mutate(item.id)}
          >
            {isActive ? 'Enroll' : 'Not Open'}
          </Button>
        );
      },
    },
  ];

  return (
    <AuthGuard allowedRoles={['Student']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Available Courses</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Browse and enroll in active courses.
              </p>
            </div>
          </div>

          <DataTable
            data={data?.items || []}
            columns={columns}
            isLoading={isLoading}
            page={data?.page || 1}
            totalPages={data?.totalPages || 1}
            onPageChange={setPage}
            emptyMessage="No courses available."
          />
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
