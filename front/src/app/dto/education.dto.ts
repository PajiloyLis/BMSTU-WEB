/**
 * DTO for education received from API
 */
export interface EducationDto {
  id: string;
  employeeId: string;
  institution: string;
  level: string;
  studyField: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for creating an education
 */
export interface CreateEducationDto {
  employeeId: string;
  institution: string;
  level: string;
  studyField: string;
  startDate: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

/**
 * DTO for updating an education
 */
export interface UpdateEducationDto {
  institution?: string;
  level?: string;
  studyField?: string;
  startDate?: string; // ISO date string (DateOnly from backend)
  endDate?: string; // ISO date string (DateOnly from backend)
}

