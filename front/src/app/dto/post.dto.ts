/**
 * DTO for post received from API
 */
export interface PostDto {
  id: string;
  title: string;
  salary: number;
  companyId: string;
  isDeleted: boolean;
}

/**
 * DTO for creating a post
 */
export interface CreatePostDto {
  title: string;
  salary: number;
  companyId: string;
}

/**
 * DTO for updating a post
 */
export interface UpdatePostDto {
  title?: string;
  salary?: number;
}

