import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap, map } from 'rxjs/operators';
import { HttpService } from './http.service';
import { AuthorizationDataDto, LoginDto } from '../dto/user.dto';
import { AuthorizationData, LoginCredentials } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_ID_KEY = 'user_id';
  private readonly EMAIL_KEY = 'user_email';
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private httpService: HttpService) {}

  login(credentials: LoginCredentials): Observable<AuthorizationData> {
    const loginDto: LoginDto = {
      email: credentials.email,
      password: credentials.password
    };

    return this.httpService.post<AuthorizationDataDto>('auth/login', loginDto).pipe(
      tap((data: AuthorizationDataDto) => {
        console.log('=== Login Response ===');
        console.log('Full response data:', data);
        console.log('data.id:', data.id);
        console.log('data.email:', data.email);
        console.log('data.token:', data.token ? 'present' : 'missing');
        this.setAuthData(data);
        this.isAuthenticatedSubject.next(true);
      }),
      map((dto: AuthorizationDataDto) => ({
        token: dto.token,
        email: dto.email,
        userId: dto.id // Convert 'id' to 'userId' for domain model
      }))
    );
  }

  register(credentials: LoginCredentials): Observable<AuthorizationData> {
    const loginDto: LoginDto = {
      email: credentials.email,
      password: credentials.password
    };

    return this.httpService.post<AuthorizationDataDto>('auth/register', loginDto).pipe(
      tap((data: AuthorizationDataDto) => {
        this.setAuthData(data);
        this.isAuthenticatedSubject.next(true);
      }),
      map((dto: AuthorizationDataDto) => ({
        token: dto.token,
        email: dto.email,
        userId: dto.id // Convert 'id' to 'userId' for domain model
      }))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_ID_KEY);
    localStorage.removeItem(this.EMAIL_KEY);
    this.isAuthenticatedSubject.next(false);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUserId(): string | null {
    return localStorage.getItem(this.USER_ID_KEY);
  }

  getEmail(): string | null {
    return localStorage.getItem(this.EMAIL_KEY);
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  private setAuthData(data: AuthorizationDataDto): void {
    console.log('=== Setting Auth Data ===');
    console.log('data.id:', data.id);
    console.log('data.id type:', typeof data.id);
    console.log('data.id is truthy?', !!data.id);
    
    localStorage.setItem(this.TOKEN_KEY, data.token);
    
    // Backend returns 'id' instead of 'userId'
    const userId = data.id;
    if (userId) {
      localStorage.setItem(this.USER_ID_KEY, userId);
      console.log('Saved userId to localStorage:', userId);
    } else {
      console.error('data.id is missing or undefined!');
      console.error('Full data object:', JSON.stringify(data, null, 2));
    }
    
    localStorage.setItem(this.EMAIL_KEY, data.email);
    
    // Verify what was saved
    console.log('Verification - localStorage user_id:', localStorage.getItem(this.USER_ID_KEY));
  }
}

