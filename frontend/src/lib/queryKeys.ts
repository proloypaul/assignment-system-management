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
    all: (page?: number, pageSize?: number) => ['courses', 'all', page ?? 0, pageSize ?? 0] as const,
  },
  subjects: {
    all: (page?: number, pageSize?: number) => ['subjects', 'all', page ?? 0, pageSize ?? 0] as const,
  },
};
