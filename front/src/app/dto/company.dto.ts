/**
 * DTO for company received from API
 */
export interface CompanyDto {
  companyId: string;
  title: string;
  registrationDate: string; // ISO date string (DateOnly from backend)
  phoneNumber: string;
  email: string;
  inn: string;
  kpp: string;
  ogrn: string;
  address: string;
  isDeleted: boolean;
}

/**
 * DTO for creating a company
 */
export interface CreateCompanyDto {
  title: string;
  registrationDate: string; // ISO date string (DateOnly from backend)
  phoneNumber: string;
  email: string;
  inn: string;
  kpp: string;
  ogrn: string;
  address: string;
}

/**
 * DTO for updating a company
 */
export interface UpdateCompanyDto {
  title?: string;
  registrationDate?: string; // ISO date string (DateOnly from backend)
  phoneNumber?: string;
  email?: string;
  inn?: string;
  kpp?: string;
  ogrn?: string;
  address?: string;
}

