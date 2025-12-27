/**
 * Domain model for position (organizational position)
 */
export interface Position {
  id: string;
  parentId?: string;
  title: string;
  companyId: string;
  isDeleted: boolean;
}

/**
 * Domain model for position hierarchy
 */
export interface PositionHierarchy {
  positionId: string;
  parentId?: string;
  title: string;
  level: number;
}

/**
 * Domain model for creating a position
 */
export interface CreatePosition {
  title: string;
  companyId: string;
  parentId?: string;
}

/**
 * Domain model for updating a position
 */
export interface UpdatePosition {
  title?: string;
  parentId?: string;
}

