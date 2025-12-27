/**
 * Domain model for company
 */
export interface Company {
  companyId: string;
  title: string;
  registrationDate: string; // ISO date string
  phoneNumber: string;
  email: string;
  inn: string;
  kpp: string;
  ogrn: string;
  address: string;
  isDeleted: boolean;
}

/**
 * Domain model for creating a company
 */
export interface CreateCompany {
  title: string;
  registrationDate: string; // ISO date string
  phoneNumber: string;
  email: string;
  inn: string;
  kpp: string;
  ogrn: string;
  address: string;
}

/**
 * Domain model for updating a company
 */
export interface UpdateCompany {
  title?: string;
  registrationDate?: string; // ISO date string
  phoneNumber?: string;
  email?: string;
  inn?: string;
  kpp?: string;
  ogrn?: string;
  address?: string;
}

