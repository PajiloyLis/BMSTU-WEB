import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpParams } from '@angular/common/http';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { PostHistoryDto, CreatePostHistoryDto, UpdatePostHistoryDto } from '../dto/post-history.dto';
import { PostHistory, CreatePostHistory, UpdatePostHistory } from '../models/post-history.model';
import { PostDto } from '../dto/post.dto';
import { Post } from '../models/post.model';

@Injectable({
  providedIn: 'root'
})
export class PostHistoryService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(employeeId: string, postId: string): Observable<PostHistory> {
    return this.httpService.get<PostHistoryDto>(`employees/${employeeId}/postHistories/${postId}`).pipe(
      map(dto => this.converters.postHistoryDtoToModel(dto))
    );
  }

  getByEmployeeId(
    employeeId: string,
    pageNumber: number = 1,
    pageSize: 10 | 20 | 40 = 10,
    startDate?: string,
    endDate?: string
  ): Observable<Post[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<PostDto[]>(`employees/${employeeId}/postHistories`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.postDtoToModel(dto)))
    );
  }

  getSubordinatesPostHistories(
    headEmployeeId: string,
    pageNumber: number = 1,
    pageSize: 10 | 20 | 40 = 10,
    startDate?: string,
    endDate?: string
  ): Observable<Post[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.httpService.get<PostDto[]>(`employees/${headEmployeeId}/subordinates/postHistories`, params).pipe(
      map(dtos => dtos.map(dto => this.converters.postDtoToModel(dto)))
    );
  }

  create(history: CreatePostHistory): Observable<PostHistory> {
    const dto = this.converters.createPostHistoryToDto(history);
    return this.httpService.post<PostHistoryDto>('postHistories', dto).pipe(
      map(dto => this.converters.postHistoryDtoToModel(dto))
    );
  }

  update(employeeId: string, postId: string, history: UpdatePostHistory): Observable<PostHistory> {
    const dto = this.converters.updatePostHistoryToDto(history);
    return this.httpService.patch<PostHistoryDto>(`employees/${employeeId}/postHistories/${postId}`, dto).pipe(
      map(dto => this.converters.postHistoryDtoToModel(dto))
    );
  }

  delete(employeeId: string, postId: string): Observable<void> {
    return this.httpService.delete<void>(`employees/${employeeId}/postHistories/${postId}`);
  }
}

