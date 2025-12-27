import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { PositionDto, CreatePositionDto, UpdatePositionDto, PositionHierarchyDto } from '../dto/position.dto';
import { Position, CreatePosition, UpdatePosition, PositionHierarchy } from '../models/position.model';

@Injectable({
  providedIn: 'root'
})
export class PositionService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(positionId: string): Observable<Position> {
    return this.httpService.get<PositionDto>(`positions/${positionId}`).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  getSubordinates(headPositionId: string): Observable<PositionHierarchy[]> {
    return this.httpService.get<PositionHierarchyDto[]>(`positions/${headPositionId}/subordinates`).pipe(
      map(dtos => dtos.map(dto => this.converters.positionHierarchyDtoToModel(dto)))
    );
  }

  getCompanyHeadPosition(companyId: string): Observable<Position> {
    return this.httpService.get<PositionDto>(`companies/${companyId}/headPosition`).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  create(position: CreatePosition): Observable<Position> {
    const dto = this.converters.createPositionToDto(position);
    return this.httpService.post<PositionDto>('positions', dto).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  update(positionId: string, position: UpdatePosition): Observable<Position> {
    const dto = this.converters.updatePositionToDto(position);
    return this.httpService.patch<PositionDto>(`positions/${positionId}`, dto).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  updateTitle(positionId: string, title: string): Observable<Position> {
    return this.httpService.patch<PositionDto>(`positions/${positionId}/title`, { title }).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  updateParent(positionId: string, parentId: string | null, updateMode: 1 | 2 = 1): Observable<Position> {
    return this.httpService.patch<PositionDto>(`positions/${positionId}/parent?updateMode=${updateMode}`, { parentId }).pipe(
      map(dto => this.converters.positionDtoToModel(dto))
    );
  }

  delete(positionId: string): Observable<void> {
    return this.httpService.delete<void>(`positions/${positionId}`);
  }
}

