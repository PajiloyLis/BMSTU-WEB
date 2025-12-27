import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpService } from './http.service';
import { ConvertersService } from './converters.service';
import { PostDto, CreatePostDto, UpdatePostDto } from '../dto/post.dto';
import { Post, CreatePost, UpdatePost } from '../models/post.model';

@Injectable({
  providedIn: 'root'
})
export class PostService {
  constructor(
    private httpService: HttpService,
    private converters: ConvertersService
  ) {}

  getById(postId: string): Observable<Post> {
    return this.httpService.get<PostDto>(`posts/${postId}`).pipe(
      map(dto => this.converters.postDtoToModel(dto))
    );
  }

  getByCompanyId(companyId: string): Observable<Post[]> {
    return this.httpService.get<PostDto[]>(`companies/${companyId}/posts`).pipe(
      map(dtos => dtos.map(dto => this.converters.postDtoToModel(dto)))
    );
  }

  create(post: CreatePost): Observable<Post> {
    const dto = this.converters.createPostToDto(post);
    return this.httpService.post<PostDto>('posts', dto).pipe(
      map(dto => this.converters.postDtoToModel(dto))
    );
  }

  update(postId: string, post: UpdatePost): Observable<Post> {
    const dto = this.converters.updatePostToDto(post);
    return this.httpService.patch<PostDto>(`posts/${postId}`, dto).pipe(
      map(dto => this.converters.postDtoToModel(dto))
    );
  }

  delete(postId: string): Observable<void> {
    return this.httpService.delete<void>(`posts/${postId}`);
  }
}

