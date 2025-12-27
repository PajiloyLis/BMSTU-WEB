import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { PositionHistoryDto, CreatePositionHistoryDto, UpdatePositionHistoryDto, PositionHierarchyWithEmployeeDto } from '../dto/position-history.dto';
import { PositionHistory, CreatePositionHistory, UpdatePositionHistory, PositionHierarchyWithEmployee } from '../models/position-history.model';

@Injectable({
  providedIn: 'root'
})
export class PositionHistoryService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(employeeId: string, positionId: string): Observable<PositionHistory> {
    return this.httpService.get<PositionHistoryDto>(`employees/${employeeId}/positionHistories/${positionId}`).pipe(
      map(dto => this.converters.positionHistoryDtoToModel(dto))
    );
  }

  getByEmployeeId(
    employeeId: string,
    pageNumber: number = 1,
    pageSize: 10 | 20 | 40 = 10,
    startDate?: string,
    endDate?: string
  ): Observable<PositionHistory[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<PositionHistoryDto[]>(`employees/${employeeId}/positionHistories`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.positionHistoryDtoToModel(dto)))
    );
  }

  getSubordinatesPositionHistories(
    employeeId: string,
    pageNumber: number = 1,
    pageSize: 10 | 20 | 40 = 10,
    startDate?: string,
    endDate?: string
  ): Observable<PositionHistory[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<PositionHistoryDto[]>(`employees/${employeeId}/subordinates/positionHistories`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.positionHistoryDtoToModel(dto)))
    );
  }

  getCurrentSubordinatesPositionHistories(headEmployeeId: string): Observable<PositionHierarchyWithEmployee[]> {
    return this.httpService.get<PositionHierarchyWithEmployeeDto[]>(`employees/${headEmployeeId}/currentSubordinates/positionHistories`).pipe(
      map(dtos => dtos.map(dto => this.converters.positionHistoryHierarchyDtoToModel(dto)))
    );
  }

  create(history: CreatePositionHistory): Observable<PositionHistory> {
    const dto = this.converters.createPositionHistoryToDto(history);
    return this.httpService.post<PositionHistoryDto>('positionHistories', dto).pipe(
      map(dto => this.converters.positionHistoryDtoToModel(dto))
    );
  }

  update(employeeId: string, positionId: string, history: UpdatePositionHistory): Observable<PositionHistory> {
    const dto = this.converters.updatePositionHistoryToDto(history);
    return this.httpService.patch<PositionHistoryDto>(`employees/${employeeId}/positionHistories/${positionId}`, dto).pipe(
      map(dto => this.converters.positionHistoryDtoToModel(dto))
    );
  }

  delete(employeeId: string, positionId: string): Observable<void> {
    return this.httpService.delete<void>(`employees/${employeeId}/positionHistories/${positionId}`);
  }
}

