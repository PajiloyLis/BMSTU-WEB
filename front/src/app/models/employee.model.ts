/**
 * Domain model for employee
 */
export interface Employee {
  employeeId: string;
  fullName: string;
  phoneNumber: string;
  email: string;
  birthday: string; // ISO date string
  photoPath?: string;
  duties?: string; // JSON string
}

/**
 * Domain model for creating an employee
 */
export interface CreateEmployee {
  fullName: string;
  phoneNumber: string;
  email: string;
  birthday: string; // ISO date string
  photoPath?: string;
  duties?: string; // JSON string
}

/**
 * Domain model for updating an employee
 */
export interface UpdateEmployee {
  fullName?: string;
  phoneNumber?: string;
  email?: string;
  birthday?: string; // ISO date string
  photoPath?: string;
  duties?: string; // JSON string
}

