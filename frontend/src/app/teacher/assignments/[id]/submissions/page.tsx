'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { Submission, Assignment } from '@/lib/types';
import DashboardLayout from '@/components/DashboardLayout';
import AuthGuard from '@/components/AuthGuard';
import { DataTable, Column } from '@/components/ui/DataTable';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { useParams, useRouter } from 'next/navigation';
import { toast } from 'sonner';

export default function TeacherSubmissionsPage() {
  const { id: assignmentId } = useParams() as { id: string };
  const router = useRouter();
  
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedSubmissionId, setSelectedSubmissionId] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { data: assignment } = useQuery({
    queryKey: queryKeys.assignments.detail(assignmentId),
    queryFn: async () => {
      const response = await api.get<Assignment>(`/assignments/${assignmentId}`);
      return response.data;
    }
  });

  const { data: submissions, isLoading } = useQuery({
    queryKey: queryKeys.submissions.forAssignment(assignmentId),
    queryFn: async () => {
      const response = await api.get<Submission[]>(`/assignments/${assignmentId}/submissions`);
      return response.data;
    }
  });

  const { register, handleSubmit, reset } = useForm<{ marksAwarded: number, feedback: string }>();

  const gradeMutation = useMutation({
    mutationFn: async (data: { marksAwarded: number, feedback: string }) => {
      await api.post(`/submissions/${selectedSubmissionId}/grade`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.submissions.forAssignment(assignmentId) });
      setIsModalOpen(false);
      reset();
      toast.success('Submission graded successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to grade submission');
    }
  });

  const onSubmit = (data: { marksAwarded: number, feedback: string }) => {
    gradeMutation.mutate(data);
  };

  const columns: Column<Submission>[] = [
    {
      header: 'Student Name',
      cell: (item) => <span className="font-medium">{item.student?.name}</span>,
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
          <a href={url} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">
            View PDF
          </a>
        ) : <span className="text-muted-foreground">No file</span>;
      },
    },
    {
      header: 'Status',
      cell: (item) => (
        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
          item.status === 'Graded' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
        }`}>
          {item.status}
        </span>
      ),
    },
    {
      header: 'Marks',
      cell: (item) => item.marksAwarded !== null && item.marksAwarded !== undefined 
        ? `${item.marksAwarded} / ${assignment?.maxMarks}` 
        : '-',
    },
    {
      header: 'Action',
      cell: (item) => (
        <Button size="sm" onClick={() => {
          setSelectedSubmissionId(item.id);
          setIsModalOpen(true);
        }}>
          {item.status === 'Graded' ? 'Update Grade' : 'Grade'}
        </Button>
      ),
    },
  ];

  return (
    <AuthGuard allowedRoles={['Teacher', 'Admin']}>
      <DashboardLayout>
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <Button variant="outline" size="sm" onClick={() => router.push('/teacher/assignments')} className="mb-4">
                &larr; Back to Assignments
              </Button>
              <h2 className="text-2xl font-bold tracking-tight text-foreground">
                Submissions for: {assignment?.title || 'Loading...'}
              </h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Review and grade student submissions. Max marks: {assignment?.maxMarks}
              </p>
            </div>
          </div>

          <DataTable
            data={submissions || []}
            columns={columns}
            isLoading={isLoading}
            page={1}
            totalPages={1}
            onPageChange={() => {}}
            emptyMessage="No submissions yet."
          />
        </div>

        {/* Grade Modal */}
        <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Grade Submission">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
            <div>
              <label className="block text-sm font-medium mb-1">Marks Awarded</label>
              <Input {...register('marksAwarded', { valueAsNumber: true })} type="number" placeholder="0" max={assignment?.maxMarks} />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Feedback</label>
              <textarea 
                {...register('feedback')} 
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 min-h-[100px]"
                placeholder="Great job..."
              />
            </div>
            <div className="flex justify-end space-x-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={gradeMutation.isPending}>
                {gradeMutation.isPending ? 'Saving...' : 'Save Grade'}
              </Button>
            </div>
          </form>
        </Modal>
      </DashboardLayout>
    </AuthGuard>
  );
}
