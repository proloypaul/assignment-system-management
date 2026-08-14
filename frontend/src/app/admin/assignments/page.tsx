'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Assignment, PaginatedResponse } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';

export default function AdminAssignmentsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const router = useRouter();

  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [selectedAssignment, setSelectedAssignment] = useState<Assignment | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: queryKeys.assignments.all(page, pageSize),
    queryFn: async () => {
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
      header: 'Teacher',
      cell: (item) => item.teacher?.name || 'N/A',
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
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" onClick={() => {
            setSelectedAssignment(item);
            setIsDetailsModalOpen(true);
          }}>
            View Details
          </Button>
          <Button size="sm" variant="outline" onClick={() => router.push(`/teacher/assignments/${item.id}/submissions`)}>
            View Submissions
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
              <h2 className="text-2xl font-bold tracking-tight text-foreground">Assignments</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                View all assignments across the platform.
              </p>
            </div>
          </div>

          <DataTable
            data={data?.items || []}
            columns={columns}
            isLoading={isLoading}
            page={page}
            totalPages={data?.totalPages || 1}
            onPageChange={setPage}
            emptyMessage="No assignments found."
          />
        </div>

        {/* Details Modal */}
        <Modal isOpen={isDetailsModalOpen} onClose={() => setIsDetailsModalOpen(false)} title="Assignment Details">
          {selectedAssignment && (
            <div className="space-y-4 mt-4">
              <div>
                <h3 className="text-lg font-bold">{selectedAssignment.title}</h3>
                <p className="text-sm text-muted-foreground">Subject: {selectedAssignment.subject?.name || 'N/A'}</p>
                <p className="text-sm text-muted-foreground">Teacher: {selectedAssignment.teacher?.name || 'N/A'}</p>
              </div>
              <div>
                <h4 className="font-semibold text-sm">Description</h4>
                <p className="text-sm whitespace-pre-wrap bg-muted p-3 rounded-md mt-1">
                  {selectedAssignment.description || 'No description provided.'}
                </p>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <h4 className="font-semibold text-sm">Start Date</h4>
                  <p className="text-sm">{new Date(selectedAssignment.startDate).toLocaleDateString()}</p>
                </div>
                <div>
                  <h4 className="font-semibold text-sm">Deadline</h4>
                  <p className="text-sm">{new Date(selectedAssignment.endDate).toLocaleString()}</p>
                </div>
                <div>
                  <h4 className="font-semibold text-sm">Max Marks</h4>
                  <p className="text-sm">{selectedAssignment.maxMarks}</p>
                </div>
              </div>
              <div className="flex justify-end pt-4">
                <Button onClick={() => setIsDetailsModalOpen(false)}>Close</Button>
              </div>
            </div>
          )}
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
