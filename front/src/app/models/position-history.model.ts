/**
 * Domain model for position history
 */
export interface PositionHistory {
  positionId: string;
  employeeId: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for position hierarchy with employee
 */
export interface PositionHierarchyWithEmployee {
  positionId: string;
  employeeId: string;
  parentId?: string;
  title: string;
  level: number;
}

/**
 * Domain model for creating a position history
 */
export interface CreatePositionHistory {
  positionId: string;
  employeeId: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for updating a position history
 */
export interface UpdatePositionHistory {
  startDate?: string; // ISO date string
  endDate?: string; // ISO date string
}

