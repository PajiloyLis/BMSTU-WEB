/**
 * DTO for employee received from API
 */
export interface EmployeeDto {
  employeeId: string;
  fullName: string;
  phoneNumber: string;
  email: string;
  birthday: string; // ISO date string (DateOnly from backend)
  photoPath?: string;
  duties?: string; // JSON string
}

/**
 * DTO for creating an employee
 */
export interface CreateEmployeeDto {
  fullName: string;
  phoneNumber: string;
  email: string;
  birthday: string; // ISO date string (DateOnly from backend)
  photoPath?: string;
  duties?: string; // JSON string
}

/**
 * DTO for updating an employee
 */
export interface UpdateEmployeeDto {
  fullName?: string;
  phoneNumber?: string;
  email?: string;
  birthday?: string; // ISO date string (DateOnly from backend)
  photoPath?: string;
  duties?: string; // JSON string
}

