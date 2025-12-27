/**
 * Domain model for post (job position)
 */
export interface Post {
  id: string;
  title: string;
  salary: number;
  companyId: string;
  isDeleted: boolean;
}

/**
 * Domain model for creating a post
 */
export interface CreatePost {
  title: string;
  salary: number;
  companyId: string;
}

/**
 * Domain model for updating a post
 */
export interface UpdatePost {
  title?: string;
  salary?: number;
}

