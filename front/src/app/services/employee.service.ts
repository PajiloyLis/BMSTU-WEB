import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto, CurrentEmployeeDto } from '../dto/employee.dto';
import { Employee, CreateEmployee, UpdateEmployee, CurrentEmployee } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(employeeId: string): Observable<Employee> {
    return this.httpService.get<EmployeeDto>(`employees/${employeeId}`).pipe(
      map(dto => this.converters.employeeDtoToModel(dto))
    );
  }

  create(employee: CreateEmployee): Observable<Employee> {
    const dto = this.converters.createEmployeeToDto(employee);
    return this.httpService.post<EmployeeDto>('employees', dto).pipe(
      map(dto => this.converters.employeeDtoToModel(dto))
    );
  }

  update(employeeId: string, employee: UpdateEmployee): Observable<Employee> {
    const dto = this.converters.updateEmployeeToDto(employee);
    return this.httpService.patch<EmployeeDto>(`employees/${employeeId}`, dto).pipe(
      map(dto => this.converters.employeeDtoToModel(dto))
    );
  }

  delete(employeeId: string): Observable<void> {
    return this.httpService.delete<void>(`employees/${employeeId}`);
  }

  uploadPhoto(employeeId: string, photo: File): Observable<any> {
    const formData = new FormData();
    formData.append('photo', photo);
    return this.httpService.postFormData(`employees/${employeeId}/photo`, formData);
  }

  updatePhoto(employeeId: string, photo: File): Observable<any> {
    const formData = new FormData();
    formData.append('photo', photo);
    return this.httpService.putFormData(`employees/${employeeId}/photo`, formData);
  }

  deletePhoto(employeeId: string): Observable<void> {
    return this.httpService.delete<void>(`employees/${employeeId}/photo`);
  }

  getPhoto(employeeId: string): Observable<Blob> {
    return this.httpService.getBlob(`employees/${employeeId}/photo`);
  }

  /**
   * Получить текущих сотрудников компании (пары positionId + employeeId)
   */
  getCurrentEmployees(companyId: string): Observable<CurrentEmployee[]> {
    return this.httpService.get<CurrentEmployeeDto[]>(`employees/${companyId}/currentEmployees`).pipe(
      map(dtos => dtos.map(dto => ({
        positionId: dto.positionId,
        employeeId: dto.employeeId
      })))
    );
  }
}

