/**
 * DTO for authorization data received from API
 */
export interface AuthorizationDataDto {
  token: string;
  email: string;
  id: string; // Backend returns 'id' instead of 'userId'
}

/**
 * DTO for login request
 */
export interface LoginDto {
  email: string;
  password: string;
}

