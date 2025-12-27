import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { CompanyDto, CreateCompanyDto, UpdateCompanyDto } from '../dto/company.dto';
import { Company, CreateCompany, UpdateCompany } from '../models/company.model';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getAll(): Observable<Company[]> {
    return this.httpService.get<CompanyDto[]>('companies').pipe(
      map(dtos => dtos.map(dto => this.converters.companyDtoToModel(dto)))
    );
  }

  getById(companyId: string): Observable<Company> {
    return this.httpService.get<CompanyDto>(`companies/${companyId}`).pipe(
      map(dto => this.converters.companyDtoToModel(dto))
    );
  }

  create(company: CreateCompany): Observable<Company> {
    const dto = this.converters.createCompanyToDto(company);
    return this.httpService.post<CompanyDto>('companies', dto).pipe(
      map(dto => this.converters.companyDtoToModel(dto))
    );
  }

  update(companyId: string, company: UpdateCompany): Observable<Company> {
    const dto = this.converters.updateCompanyToDto(company);
    return this.httpService.patch<CompanyDto>(`companies/${companyId}`, dto).pipe(
      map(dto => this.converters.companyDtoToModel(dto))
    );
  }

  delete(companyId: string): Observable<void> {
    return this.httpService.delete<void>(`companies/${companyId}`);
  }
}

