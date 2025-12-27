/**
 * Domain model for post history
 */
export interface PostHistory {
  postId: string;
  employeeId: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for creating a post history
 */
export interface CreatePostHistory {
  postId: string;
  employeeId: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for updating a post history
 */
export interface UpdatePostHistory {
  startDate?: string; // ISO date string
  endDate?: string; // ISO date string
}

