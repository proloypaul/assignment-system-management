'use client';

import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Submission } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { FileText } from 'lucide-react';

export default function StudentSubmissionsPage() {
  const { data: submissions, isLoading } = useQuery({
    queryKey: ['my-submissions'],
    queryFn: async () => {
      // Backend endpoint to list all of a student's own submissions
      const response = await api.get<Submission[]>('/submissions/my');
      return response.data;
    }
  });

  const columns: Column<Submission>[] = [
    {
      header: 'Assignment',
      cell: (item) => <span className="font-medium">{item.assignmentId}</span>,
    },
    {
      header: 'Submitted At',
      cell: (item) => new Date(item.submittedAt).toLocaleString(),
    },
    {
      header: 'Attachment',
      cell: (item) => {
        const url = item.attachmentFileUrl || item.attachmentUrl;
        return url ? (
          <a href={url} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-blue-600 hover:underline text-sm">
            <FileText className="w-3 h-3" />
            View PDF
          </a>
        ) : <span className="text-muted-foreground">No file</span>;
      },
    },
    {
      header: 'Status',
      cell: (item) => (
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
          item.status === 'Graded'
            ? 'bg-green-100 text-green-700'
            : item.status === 'Submitted'
            ? 'bg-blue-100 text-blue-700'
            : 'bg-yellow-100 text-yellow-700'
        }`}>
          {item.status}
        </span>
      ),
    },
    {
      header: 'Marks',
      cell: (item) => item.marksAwarded !== null && item.marksAwarded !== undefined
        ? <span className="font-semibold text-green-700">{item.marksAwarded}</span>
        : <span className="text-muted-foreground">Not graded</span>,
    },
    {
      header: 'Feedback',
      cell: (item) => item.feedback
        ? <span className="text-sm max-w-xs truncate block" title={item.feedback}>{item.feedback}</span>
        : <span className="text-muted-foreground text-sm">—</span>,
    },
  ];

  return (
    <AuthGuard allowedRoles={['Student']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">My Submissions</h2>
            <p className="text-muted-foreground mt-1 text-sm">
              View all your submitted assignments, grades, and teacher feedback.
            </p>
          </div>

          <DataTable
            data={submissions || []}
            columns={columns}
            isLoading={isLoading}
            page={1}
            totalPages={1}
            onPageChange={() => {}}
            emptyMessage="You haven't submitted any assignments yet."
          />
        </div>
      </DashboardLayout>
    </AuthGuard>
  );
}
