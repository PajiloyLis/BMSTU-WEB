import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { EducationDto, CreateEducationDto, UpdateEducationDto } from '../dto/education.dto';
import { Education, CreateEducation, UpdateEducation } from '../models/education.model';

@Injectable({
  providedIn: 'root'
})
export class EducationService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(educationId: string): Observable<Education> {
    return this.httpService.get<EducationDto>(`educations/${educationId}`).pipe(
      map(dto => this.converters.educationDtoToModel(dto))
    );
  }

  getByEmployeeId(
    employeeId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<Education[]> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.httpService.get<EducationDto[]>(`employees/${employeeId}/educations`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.educationDtoToModel(dto)))
    );
  }

  create(education: CreateEducation): Observable<Education> {
    const dto = this.converters.createEducationToDto(education);
    return this.httpService.post<EducationDto>('educations', dto).pipe(
      map(dto => this.converters.educationDtoToModel(dto))
    );
  }

  update(educationId: string, education: UpdateEducation): Observable<Education> {
    const dto = this.converters.updateEducationToDto(education);
    return this.httpService.patch<EducationDto>(`educations/${educationId}`, dto).pipe(
      map(dto => this.converters.educationDtoToModel(dto))
    );
  }

  delete(educationId: string): Observable<void> {
    return this.httpService.delete<void>(`educations/${educationId}`);
  }
}

