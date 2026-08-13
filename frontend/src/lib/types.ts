export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  createdAt: string;
}

export interface Subject {
  id: string;
  name: string;
  code?: string;
  credits: number;
  syllabusUrl?: string;
  courseId: string;
  courseName?: string;
  courseCode?: string;
  teacherNames?: string[];
}

export interface Teacher {
  id: string;
  name: string;
}

export interface Assignment {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  maxMarks: number;
  status: string;
  subject?: Subject;
  teacher?: Teacher;
}

export interface Course {
  id: string;
  name: string;
  code: string;
  description: string;
  capacity: number;
  isActive: boolean;
  startDate: string;
  endDate: string;
  subjects?: Subject[];
}

export interface Submission {
  id: string;
  assignmentId: string;
  assignmentTitle?: string;
  teacherName?: string;
  subjectName?: string;
  studentId: string;
  studentName?: string;
  studentEmail?: string;
  answerText?: string;
  /** Stored URL of uploaded PDF on the server */
  attachmentFileUrl?: string;
  /** Alias used in some responses */
  attachmentUrl?: string;
  status: string;
  marksAwarded?: number;
  feedback?: string;
  submittedAt: string;
  student?: {
    id: string;
    name: string;
    email: string;
  };
}
