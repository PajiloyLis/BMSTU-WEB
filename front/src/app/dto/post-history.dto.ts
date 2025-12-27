/**
 * DTO for post history received from API
 */
export interface PostHistoryDto {
  postId: string;
  employeeId: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for creating a post history
 */
export interface CreatePostHistoryDto {
  postId: string;
  employeeId: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for updating a post history
 */
export interface UpdatePostHistoryDto {
  startDate?: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

