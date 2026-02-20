import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { ScoreDto, CreateScoreDto, UpdateScoreDto } from '../dto/score.dto';
import { Score, CreateScore, UpdateScore } from '../models/score.model';

@Injectable({
  providedIn: 'root'
})
export class ScoreService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(scoreId: string): Observable<Score> {
    return this.httpService.get<ScoreDto>(`scores/${scoreId}`).pipe(
      map(dto => this.converters.scoreDtoToModel(dto))
    );
  }

  getByEmployeeId(
    employeeId: string,
    pageNumber: number = 1,
    pageSize: 12 | 24 | 36 = 12,
    startDate?: string,
    endDate?: string
  ): Observable<Score[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<ScoreDto[]>(`employees/${employeeId}/scores`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.scoreDtoToModel(dto)))
    );
  }

  getByAuthorId(
    authorId: string,
    pageNumber: number = 1,
    pageSize: 12 | 24 | 36 = 12,
    startDate?: string,
    endDate?: string
  ): Observable<Score[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<ScoreDto[]>(`employees/scoreAuthor/${authorId}/scores`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.scoreDtoToModel(dto)))
    );
  }

  getByPositionId(
    positionId: string,
    pageNumber: number = 1,
    pageSize: 12 | 24 | 36 = 12,
    startDate?: string,
    endDate?: string
  ): Observable<Score[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<ScoreDto[]>(`positions/${positionId}/scores`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.scoreDtoToModel(dto)))
    );
  }

  getSubordinatesScores(
    headEmployeeId: string,
    pageNumber: number = 1,
    pageSize: 12 | 24 | 36 = 12,
    startDate?: string,
    endDate?: string
  ): Observable<Score[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<ScoreDto[]>(`employees/${headEmployeeId}/subordinates/scores`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.scoreDtoToModel(dto)))
    );
  }

  getSubordinatesLastScores(headEmployeeId: string): Observable<Score[]> {
    return this.httpService.get<ScoreDto[]>(`employees/${headEmployeeId}/subordinates/lasrScores`).pipe(
      map(dtos => dtos.map(dto => this.converters.scoreDtoToModel(dto)))
    );
  }

  create(score: CreateScore): Observable<Score> {
    const dto = this.converters.createScoreToDto(score);
    return this.httpService.post<ScoreDto>('scores', dto).pipe(
      map(dto => this.converters.scoreDtoToModel(dto))
    );
  }

  update(scoreId: string, score: UpdateScore): Observable<Score> {
    const dto = this.converters.updateScoreToDto(score);
    return this.httpService.patch<ScoreDto>(`scores/${scoreId}`, dto).pipe(
      map(dto => this.converters.scoreDtoToModel(dto))
    );
  }

  delete(scoreId: string): Observable<void> {
    return this.httpService.delete<void>(`scores/${scoreId}`);
  }
}

