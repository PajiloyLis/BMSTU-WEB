/**
 * Education level enum
 */
export enum EducationLevel {
  Bachelor = 'Высшее (бакалавриат)',
  Master = 'Высшее (магистратура)',
  Specialist = 'Высшее (специалитет)',
  SecondaryProfessionalPKR = 'Среднее профессиональное (ПКР)',
  SecondaryProfessionalPSSZ = 'Среднее профессиональное (ПССЗ)',
  RetrainingProgram = 'Программы переподготовки',
  QualificationCourses = 'Курсы повышения квалификации'
}

/**
 * Domain model for education
 */
export interface Education {
  id: string;
  employeeId: string;
  institution: string;
  level: EducationLevel | string;
  studyField: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for creating an education
 */
export interface CreateEducation {
  employeeId: string;
  institution: string;
  level: EducationLevel | string;
  studyField: string;
  startDate: string; // ISO date string
  endDate?: string; // ISO date string
}

/**
 * Domain model for updating an education
 */
export interface UpdateEducation {
  institution?: string;
  level?: EducationLevel | string;
  studyField?: string;
  startDate?: string; // ISO date string
  endDate?: string; // ISO date string
}

