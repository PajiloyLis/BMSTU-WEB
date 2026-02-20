import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';
import { PositionHistoryService } from '../../services/position-history.service';
import { PostHistoryService } from '../../services/post-history.service';
import { EducationService } from '../../services/education.service';
import { ScoreService } from '../../services/score.service';
import { PositionService } from '../../services/position.service';
import { PostService } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';
import { Employee } from '../../models/employee.model';
import { PositionHistory } from '../../models/position-history.model';
import { Education } from '../../models/education.model';
import { Score } from '../../models/score.model';
import { Position } from '../../models/position.model';
import { Post } from '../../models/post.model';

interface HistoryEntry {
  text: string;
}

interface EducationEntry {
  text: string;
}

interface ScoreEntry {
  period: string;
  efficiency: number;
  engagement: number;
  competency: number;
}

@Component({
  selector: 'app-personal-cabinet-page',
  templateUrl: './personal-cabinet-page.component.html',
  styleUrls: ['./personal-cabinet-page.component.scss'],
})
export class PersonalCabinetPageComponent implements OnInit {
  employeeId: string | null = null;
  employee: Employee | null = null;
  isSelf: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  isMenuOpen: boolean = false;

  history: HistoryEntry[] = [];
  education: EducationEntry[] = [];
  scores: ScoreEntry[] = [];

  phone: string = '';
  email: string = '';
  birthday: string = '';
  salary: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private positionHistoryService: PositionHistoryService,
    private postHistoryService: PostHistoryService,
    private educationService: EducationService,
    private scoreService: ScoreService,
    private positionService: PositionService,
    private postService: PostService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const queryEmployeeId = params['employeeId'];
      const authUserId = this.authService.getUserId();
      
      console.log('=== Personal Cabinet Init ===');
      console.log('Query params employeeId:', queryEmployeeId);
      console.log('Auth service userId (which is employeeId):', authUserId);
      console.log('Auth service isAuthenticated:', this.authService.isAuthenticated());
      console.log('localStorage user_id:', localStorage.getItem('user_id'));
      
      // Use query param if provided, otherwise use auth userId
      this.employeeId = queryEmployeeId || authUserId;
      
      console.log('Final employeeId after assignment:', this.employeeId);
      console.log('employeeId type:', typeof this.employeeId);
      console.log('employeeId is truthy?', !!this.employeeId);
      
      if (this.employeeId && this.employeeId !== 'undefined' && this.employeeId !== 'null') {
        const currentUserId = this.authService.getUserId();
        this.isSelf = currentUserId === this.employeeId;
        console.log('Using employeeId:', this.employeeId);
        console.log('Is self:', this.isSelf);
        
        // Call load methods
        this.loadEmployee();
        this.loadHistory();
        this.loadEducation();
        this.loadScores();
      } else {
        console.error('No valid employeeId found!');
        console.error('queryEmployeeId:', queryEmployeeId);
        console.error('authUserId:', authUserId);
        this.errorMessage = 'Не указан сотрудник';
      }
    });
  }

  loadEmployee(): void {
    if (!this.employeeId) {
      console.error('EmployeeId is null or undefined');
      this.errorMessage = 'Не указан ID сотрудника';
      return;
    }

    console.log('Loading employee with ID:', this.employeeId);
    console.log('EmployeeId type:', typeof this.employeeId);
    console.log('EmployeeId length:', this.employeeId.length);
    this.isLoading = true;
    this.employeeService.getById(this.employeeId).subscribe({
      next: (employee) => {
        console.log('Employee loaded successfully:', employee);
        this.employee = employee;
        this.email = employee.email;
        this.phone = employee.phoneNumber;
        this.birthday = this.formatDate(employee.birthday);
        this.isLoading = false;
        this.errorMessage = '';
      },
      error: (error) => {
        console.error('Error loading employee:', error);
        console.error('Error details:', {
          url: error.url,
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          error: error.error
        });
        this.errorMessage = error.message || 'Ошибка загрузки данных сотрудника';
        this.isLoading = false;
      }
    });
  }

  loadHistory(): void {
    const employeeId = this.employeeId;
    if (!employeeId) {
      console.warn('loadHistory: employeeId is not set, current value:', employeeId);
      return;
    }

    console.log('loadHistory: Loading history for employeeId:', employeeId);
    this.positionHistoryService.getByEmployeeId(employeeId, 1, 40).subscribe({
      next: (histories) => {
        const historyPromises = histories.map(history => 
          this.positionService.getById(history.positionId).toPromise()
        );
        
        Promise.all(historyPromises).then(positions => {
          this.history = histories.map((history, index) => {
            const position = positions[index];
            const startDate = this.formatDate(history.startDate);
            const endDate = history.endDate ? this.formatDate(history.endDate) : 'н. в.';
            return {
              text: `${position?.title || 'Неизвестная позиция'} (${startDate} - ${endDate})`
            };
          });
        });
      },
      error: (error) => {
        console.error('Error loading history:', error);
      }
    });
  }

  loadEducation(): void {
    const employeeId = this.employeeId;
    if (!employeeId) {
      console.warn('loadEducation: employeeId is not set, current value:', employeeId);
      return;
    }

    console.log('loadEducation: Loading education for employeeId:', employeeId);
    this.educationService.getByEmployeeId(employeeId, 1, 100).subscribe({
      next: (educations) => {
        this.education = educations.map(edu => {
          const startDate = this.formatDate(edu.startDate);
          const endDate = edu.endDate ? this.formatDate(edu.endDate) : '';
          const dateStr = endDate ? `(${startDate} - ${endDate})` : `(${startDate})`;
          return {
            text: `${edu.institution} ${dateStr}: ${edu.level}`
          };
        });
      },
      error: (error) => {
        console.error('Error loading education:', error);
      }
    });
  }

  loadScores(): void {
    const employeeId = this.employeeId;
    if (!employeeId) {
      console.warn('loadScores: employeeId is not set, current value:', employeeId);
      return;
    }

    console.log('loadScores: Loading scores for employeeId:', employeeId);
    this.scoreService.getByEmployeeId(employeeId, 1, 36).subscribe({
      next: (scores) => {
        this.scores = scores.map(score => {
          const date = new Date(score.createdAt);
          const period = `${String(date.getMonth() + 1).padStart(2, '0')}.${date.getFullYear()}`;
          return {
            period,
            efficiency: score.efficiencyScore,
            engagement: score.engagementScore,
            competency: score.competencyScore
          };
        }).sort((a, b) => {
          // Sort by period descending
          const [monthA, yearA] = a.period.split('.').map(Number);
          const [monthB, yearB] = b.period.split('.').map(Number);
          if (yearA !== yearB) return yearB - yearA;
          return monthB - monthA;
        });
      },
      error: (error) => {
        console.error('Error loading scores:', error);
      }
    });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  goToPersonalCabinet(): void {
    // If already on personal cabinet, navigate to own cabinet (without employeeId)
    const userId = this.authService.getUserId();
    if (userId) {
      this.router.navigate(['/cabinet'], { queryParams: { employeeId: userId } });
    } else {
      this.router.navigate(['/cabinet']);
    }
  }

  get fullName(): string {
    return this.employee?.fullName || 'Загрузка...';
  }
}
