/**
 * Domain model for user authorization data
 */
export interface AuthorizationData {
  token: string;
  email: string;
  userId: string;
}

/**
 * Domain model for login credentials
 */
export interface LoginCredentials {
  email: string;
  password: string;
}

