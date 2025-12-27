/**
 * DTO for position received from API
 */
export interface PositionDto {
  id: string;
  parentId?: string;
  title: string;
  companyId: string;
  isDeleted: boolean;
}

/**
 * DTO for position hierarchy received from API
 */
export interface PositionHierarchyDto {
  positionId: string;
  parentId?: string;
  title: string;
  level: number;
}

/**
 * DTO for creating a position
 */
export interface CreatePositionDto {
  title: string;
  companyId: string;
  parentId?: string;
}

/**
 * DTO for updating a position
 */
export interface UpdatePositionDto {
  title?: string;
  parentId?: string;
}

