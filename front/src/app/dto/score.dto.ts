/**
 * DTO for score received from API
 */
export interface ScoreDto {
    id: string;
    employeeId: string;
    authorId: string;
    positionId: string;
    createdAt: string; // ISO date-time string (DateTimeOffset from backend)
    efficiencyScore: number; // 1-5
    engagementScore: number; // 1-5
    competencyScore: number; // 1-5
  }
  
  /**
   * DTO for creating a score
   */
  export interface CreateScoreDto {
    employeeId: string;
    authorId: string;
    positionId: string;
    createdAt: string; // ISO date-time string (DateTimeOffset from backend)
    efficiencyScore: number; // 1-5
    engagementScore: number; // 1-5
    competencyScore: number; // 1-5
  }
  
  /**
   * DTO for updating a score
   */
  export interface UpdateScoreDto {
    createdAt?: string; // ISO date-time string (DateTimeOffset from backend)
    efficiencyScore?: number; // 1-5
    engagementScore?: number; // 1-5
    competencyScore?: number; // 1-5
  }
  