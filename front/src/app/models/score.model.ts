/**
 * Domain model for score (employee evaluation)
 */
export interface Score {
  id: string;
  employeeId: string;
  authorId: string;
  positionId: string;
  createdAt: string; // ISO date-time string
  efficiencyScore: number; // 1-5
  engagementScore: number; // 1-5
  competencyScore: number; // 1-5
}

/**
 * Domain model for creating a score
 */
export interface CreateScore {
  employeeId: string;
  authorId: string;
  positionId: string;
  createdAt: string; // ISO date-time string
  efficiencyScore: number; // 1-5
  engagementScore: number; // 1-5
  competencyScore: number; // 1-5
}

/**
 * Domain model for updating a score
 */
export interface UpdateScore {
  createdAt?: string; // ISO date-time string
  efficiencyScore?: number; // 1-5
  engagementScore?: number; // 1-5
  competencyScore?: number; // 1-5
}

