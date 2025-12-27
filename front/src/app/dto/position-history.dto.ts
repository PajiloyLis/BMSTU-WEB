/**
 * DTO for position history received from API
 */
export interface PositionHistoryDto {
  positionId: string;
  employeeId: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for position hierarchy with employee received from API
 */
export interface PositionHierarchyWithEmployeeDto {
  positionId: string;
  employeeId: string;
  parentId?: string;
  title: string;
  level: number;
}

/**
 * DTO for creating a position history
 */
export interface CreatePositionHistoryDto {
  positionId: string;
  employeeId: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for updating a position history
 */
export interface UpdatePositionHistoryDto {
  startDate?: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

