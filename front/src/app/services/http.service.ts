import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ErrorDto } from '../dto/error.dto';

@Injectable({
  providedIn: 'root'
})
export class HttpService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('auth_token');
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return headers;
  }

  get<T>(url: string, params?: HttpParams): Observable<T> {
    const fullUrl = `${this.baseUrl}${url}`;
    console.log('GET Request URL:', fullUrl);
    console.log('GET Request Headers:', this.getHeaders().keys());
    return this.http.get<T>(fullUrl, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(this.handleError)
    );
  }

  post<T>(url: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${url}`, body, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  patch<T>(url: string, body: any): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}${url}`, body, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    );
  }

  delete<T>(url: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${url}`, {
      headers: this.getHeaders()
    }).pipe(
      catchError(this.handleError)
    ) as Observable<T>;
  }

  postFormData<T>(url: string, formData: FormData): Observable<T> {
    const token = localStorage.getItem('auth_token');
    let headers = new HttpHeaders();
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return this.http.post<T>(`${this.baseUrl}${url}`, formData, {
      headers
    }).pipe(
      catchError(this.handleError)
    );
  }

  putFormData<T>(url: string, formData: FormData): Observable<T> {
    const token = localStorage.getItem('auth_token');
    let headers = new HttpHeaders();
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return this.http.put<T>(`${this.baseUrl}${url}`, formData, {
      headers
    }).pipe(
      catchError(this.handleError)
    );
  }

  getBlob(url: string): Observable<Blob> {
    const token = localStorage.getItem('auth_token');
    let headers = new HttpHeaders();
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return this.http.get(`${this.baseUrl}${url}`, {
      headers,
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  private handleError = (error: any): Observable<never> => {
    let errorMessage = 'Произошла ошибка';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Ошибка: ${error.error.message}`;
    } else if (error.error && error.error.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    console.error('HTTP Error:', error);
    console.error('Error URL:', error.url);
    console.error('Error Status:', error.status);
    console.error('Error StatusText:', error.statusText);
    
    // Сохраняем все свойства оригинальной ошибки
    const enhancedError: any = new Error(errorMessage);
    enhancedError.url = error.url;
    enhancedError.status = error.status;
    enhancedError.statusText = error.statusText;
    enhancedError.error = error.error;
    enhancedError.originalError = error;
    
    return throwError(() => enhancedError);
  };
}

