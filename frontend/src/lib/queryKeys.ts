export const queryKeys = {
  users: {
    all: (page: number, pageSize: number, search: string, role: string) =>
      ['users', 'all', page, pageSize, search, role] as const,
  },
  assignments: {
    all: (page: number, pageSize: number) => ['assignments', 'all', page, pageSize] as const,
    detail: (id: string) => ['assignments', 'detail', id] as const,
  },
  submissions: {
    mine: (assignmentId: string) => ['submissions', 'mine', assignmentId] as const,
    forAssignment: (assignmentId: string) => ['submissions', 'assignment', assignmentId] as const,
  },
  courses: {
    all: () => ['courses', 'all'] as const,
  },
  subjects: {
    all: () => ['subjects', 'all'] as const,
  },
};
