'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Assignment, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Plus } from 'lucide-react';

export default function TeacherDashboard() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data, isLoading } = useQuery({
    queryKey: ['teacher-assignments', page, pageSize], // Assuming a custom query for teacher's own assignments
    queryFn: async () => {
      // NOTE: For now using the generic assignments endpoint. 
      // In a real app, you'd want an endpoint like /assignments/my that filters by TeacherId.
      const response = await api.get<PaginatedResponse<Assignment>>('/assignments', {
        params: { page, pageSize }
      });
      return response.data;
    }
  });

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
      header: 'Max Marks',
      accessorKey: 'maxMarks',
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
          <Button size="sm" variant="outline" onClick={() => alert(`Edit ${item.title}`)}>
            Edit
          </Button>
          <Button size="sm" variant="outline" onClick={() => alert(`Grade ${item.title}`)}>
            Grade
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
              <h2 className="text-2xl font-bold tracking-tight text-foreground">My Assignments</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Manage your assignments and grade submissions.
              </p>
            </div>
            <Button onClick={() => alert('Open Create Assignment Modal')}>
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
            emptyMessage="You haven't created any assignments yet."
          />
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
