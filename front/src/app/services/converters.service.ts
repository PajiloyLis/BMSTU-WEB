import { Injectable } from '@angular/core';
import { CompanyDto, CreateCompanyDto, UpdateCompanyDto } from '../dto/company.dto';
import { Company, CreateCompany, UpdateCompany } from '../models/company.model';
import { EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto } from '../dto/employee.dto';
import { Employee, CreateEmployee, UpdateEmployee } from '../models/employee.model';
import { PostDto, CreatePostDto, UpdatePostDto } from '../dto/post.dto';
import { Post, CreatePost, UpdatePost } from '../models/post.model';
import { PositionDto, CreatePositionDto, UpdatePositionDto, PositionHierarchyDto } from '../dto/position.dto';
import { Position, CreatePosition, UpdatePosition, PositionHierarchy } from '../models/position.model';
import { PositionHistoryDto, CreatePositionHistoryDto, UpdatePositionHistoryDto, PositionHierarchyWithEmployeeDto as PositionHistoryHierarchyDto } from '../dto/position-history.dto';
import { PositionHistory, CreatePositionHistory, UpdatePositionHistory, PositionHierarchyWithEmployee as PositionHistoryHierarchy } from '../models/position-history.model';
import { PostHistoryDto, CreatePostHistoryDto, UpdatePostHistoryDto } from '../dto/post-history.dto';
import { PostHistory, CreatePostHistory, UpdatePostHistory } from '../models/post-history.model';
import { EducationDto, CreateEducationDto, UpdateEducationDto } from '../dto/education.dto';
import { Education, CreateEducation, UpdateEducation } from '../models/education.model';
import { ScoreDto, CreateScoreDto, UpdateScoreDto } from '../dto/score.dto';
import { Score, CreateScore, UpdateScore } from '../models/score.model';
import { AuthorizationDataDto } from '../dto/user.dto';
import { AuthorizationData } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class ConvertersService {
  // Company converters
  companyDtoToModel(dto: CompanyDto): Company {
    return {
      companyId: dto.companyId,
      title: dto.title,
      registrationDate: dto.registrationDate,
      phoneNumber: dto.phoneNumber,
      email: dto.email,
      inn: dto.inn,
      kpp: dto.kpp,
      ogrn: dto.ogrn,
      address: dto.address,
      isDeleted: dto.isDeleted
    };
  }

  createCompanyToDto(model: CreateCompany): CreateCompanyDto {
    return {
      title: model.title,
      registrationDate: model.registrationDate,
      phoneNumber: model.phoneNumber,
      email: model.email,
      inn: model.inn,
      kpp: model.kpp,
      ogrn: model.ogrn,
      address: model.address
    };
  }

  updateCompanyToDto(model: UpdateCompany): UpdateCompanyDto {
    return {
      title: model.title,
      registrationDate: model.registrationDate,
      phoneNumber: model.phoneNumber,
      email: model.email,
      inn: model.inn,
      kpp: model.kpp,
      ogrn: model.ogrn,
      address: model.address
    };
  }

  // Employee converters
  employeeDtoToModel(dto: EmployeeDto): Employee {
    return {
      employeeId: dto.employeeId,
      fullName: dto.fullName,
      phoneNumber: dto.phoneNumber,
      email: dto.email,
      birthday: dto.birthday,
      photoPath: dto.photoPath,
      duties: dto.duties ? (typeof dto.duties === 'string' ? JSON.parse(dto.duties) : dto.duties) : null
    };
  }

  createEmployeeToDto(model: CreateEmployee): CreateEmployeeDto {
    return {
      fullName: model.fullName,
      phoneNumber: model.phoneNumber,
      email: model.email,
      birthday: model.birthday,
      photoPath: model.photoPath,
      duties: model.duties ? JSON.stringify(model.duties) : undefined
    };
  }

  updateEmployeeToDto(model: UpdateEmployee): UpdateEmployeeDto {
    return {
      fullName: model.fullName,
      phoneNumber: model.phoneNumber,
      email: model.email,
      birthday: model.birthday,
      photoPath: model.photoPath,
      duties: model.duties ? (typeof model.duties === 'string' ? model.duties : JSON.stringify(model.duties)) : undefined
    };
  }

  // Post converters
  postDtoToModel(dto: PostDto): Post {
    return {
      id: dto.id,
      title: dto.title,
      salary: dto.salary,
      companyId: dto.companyId,
      isDeleted: dto.isDeleted
    };
  }

  createPostToDto(model: CreatePost): CreatePostDto {
    return {
      title: model.title,
      salary: model.salary,
      companyId: model.companyId
    };
  }

  updatePostToDto(model: UpdatePost): UpdatePostDto {
    return {
      title: model.title,
      salary: model.salary
    };
  }

  // Position converters
  positionDtoToModel(dto: PositionDto): Position {
    return {
      id: dto.id,
      title: dto.title,
      companyId: dto.companyId,
      parentId: dto.parentId,
      isDeleted: dto.isDeleted
    };
  }

  createPositionToDto(model: CreatePosition): CreatePositionDto {
    return {
      title: model.title,
      companyId: model.companyId,
      parentId: model.parentId
    };
  }

  updatePositionToDto(model: UpdatePosition): UpdatePositionDto {
    return {
      title: model.title,
      parentId: model.parentId
    };
  }

  positionHierarchyDtoToModel(dto: PositionHierarchyDto): PositionHierarchy {
    return {
      positionId: dto.positionId,
      title: dto.title,
      parentId: dto.parentId,
      level: dto.level
    };
  }

  positionHierarchyWithEmployeeDtoToModel(dto: PositionHistoryHierarchyDto): PositionHistoryHierarchy {
    return {
      positionId: dto.positionId,
      employeeId: dto.employeeId,
      title: dto.title,
      parentId: dto.parentId,
      level: dto.level
    };
  }

  // PositionHistory converters
  positionHistoryDtoToModel(dto: PositionHistoryDto): PositionHistory {
    return {
      positionId: dto.positionId,
      employeeId: dto.employeeId,
      startDate: dto.startDate,
      endDate: dto.endDate
    };
  }

  createPositionHistoryToDto(model: CreatePositionHistory): CreatePositionHistoryDto {
    return {
      positionId: model.positionId,
      employeeId: model.employeeId,
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  updatePositionHistoryToDto(model: UpdatePositionHistory): UpdatePositionHistoryDto {
    return {
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  positionHistoryHierarchyDtoToModel(dto: PositionHistoryHierarchyDto): PositionHistoryHierarchy {
    return {
      positionId: dto.positionId,
      employeeId: dto.employeeId,
      title: dto.title,
      parentId: dto.parentId,
      level: dto.level
    };
  }

  // PostHistory converters
  postHistoryDtoToModel(dto: PostHistoryDto): PostHistory {
    return {
      postId: dto.postId,
      employeeId: dto.employeeId,
      startDate: dto.startDate,
      endDate: dto.endDate
    };
  }

  createPostHistoryToDto(model: CreatePostHistory): CreatePostHistoryDto {
    return {
      postId: model.postId,
      employeeId: model.employeeId,
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  updatePostHistoryToDto(model: UpdatePostHistory): UpdatePostHistoryDto {
    return {
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  // Education converters
  educationDtoToModel(dto: EducationDto): Education {
    return {
      id: dto.id,
      employeeId: dto.employeeId,
      institution: dto.institution,
      level: dto.level,
      studyField: dto.studyField,
      startDate: dto.startDate,
      endDate: dto.endDate
    };
  }

  createEducationToDto(model: CreateEducation): CreateEducationDto {
    return {
      employeeId: model.employeeId,
      institution: model.institution,
      level: model.level,
      studyField: model.studyField,
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  updateEducationToDto(model: UpdateEducation): UpdateEducationDto {
    return {
      institution: model.institution,
      level: model.level,
      studyField: model.studyField,
      startDate: model.startDate,
      endDate: model.endDate
    };
  }

  // Score converters
  scoreDtoToModel(dto: ScoreDto): Score {
    return {
      id: dto.id,
      employeeId: dto.employeeId,
      authorId: dto.authorId,
      positionId: dto.positionId,
      createdAt: dto.createdAt,
      efficiencyScore: dto.efficiencyScore,
      engagementScore: dto.engagementScore,
      competencyScore: dto.competencyScore
    };
  }

  createScoreToDto(model: CreateScore): CreateScoreDto {
    return {
      employeeId: model.employeeId,
      authorId: model.authorId,
      positionId: model.positionId,
      createdAt: model.createdAt,
      efficiencyScore: model.efficiencyScore,
      engagementScore: model.engagementScore,
      competencyScore: model.competencyScore
    };
  }

  updateScoreToDto(model: UpdateScore): UpdateScoreDto {
    return {
      createdAt: model.createdAt,
      efficiencyScore: model.efficiencyScore,
      engagementScore: model.engagementScore,
      competencyScore: model.competencyScore
    };
  }

  // Authorization converters
  authorizationDataDtoToModel(dto: AuthorizationDataDto): AuthorizationData {
    return {
      token: dto.token,
      email: dto.email,
      userId: dto.id // Backend returns 'id' instead of 'userId'
    };
  }
}

