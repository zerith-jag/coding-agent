import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /**
   * GET request
   */
  public get<T>(endpoint: string, options?: any): Observable<T> {
    return this.http.get(`${this.baseUrl}${endpoint}`, options) as Observable<T>;
  }

  /**
   * POST request
   */
  public post<T>(endpoint: string, body: any, options?: any): Observable<T> {
    return this.http.post(`${this.baseUrl}${endpoint}`, body, options) as Observable<T>;
  }

  /**
   * PUT request
   */
  public put<T>(endpoint: string, body: any, options?: any): Observable<T> {
    return this.http.put(`${this.baseUrl}${endpoint}`, body, options) as Observable<T>;
  }

  /**
   * DELETE request
   */
  public delete<T>(endpoint: string, options?: any): Observable<T> {
    return this.http.delete(`${this.baseUrl}${endpoint}`, options) as Observable<T>;
  }

  /**
   * PATCH request
   */
  public patch<T>(endpoint: string, body: any, options?: any): Observable<T> {
    return this.http.patch(`${this.baseUrl}${endpoint}`, body, options) as Observable<T>;
  }
}
