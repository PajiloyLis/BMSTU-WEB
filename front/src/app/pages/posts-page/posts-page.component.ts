import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PostService } from '../../services/post.service';
import { PostHistoryService } from '../../services/post-history.service';
import { EmployeeService } from '../../services/employee.service';
import { CompanyService } from '../../services/company.service';
import { AuthService } from '../../services/auth.service';
import { Post } from '../../models/post.model';
import { Employee } from '../../models/employee.model';
import { Company } from '../../models/company.model';

interface PostRow {
  post: Post;
  employeeName?: string;
}

@Component({
  selector: 'app-posts-page',
  templateUrl: './posts-page.component.html',
  styleUrls: ['./posts-page.component.scss'],
})
export class PostsPageComponent implements OnInit {
  isAuthorized: boolean = false;
  companyId: string | null = null;
  company: Company | null = null;
  posts: PostRow[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  userEmail: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private postService: PostService,
    private postHistoryService: PostHistoryService,
    private employeeService: EmployeeService,
    private companyService: CompanyService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAuthorized = this.authService.isAuthenticated();
    this.userEmail = this.authService.getEmail();
    
    this.route.queryParams.subscribe(params => {
      this.companyId = params['companyId'] || null;
      if (this.companyId) {
        this.loadCompany();
        this.loadPosts();
      } else {
        this.errorMessage = 'Не указана компания';
      }
    });
  }

  loadCompany(): void {
    if (!this.companyId) return;
    
    this.companyService.getById(this.companyId).subscribe({
      next: (company) => {
        this.company = company;
      },
      error: (error) => {
        this.errorMessage = error.message || 'Ошибка загрузки компании';
      }
    });
  }

  loadPosts(): void {
    if (!this.companyId) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.postService.getByCompanyId(this.companyId).subscribe({
      next: (posts) => {
        this.posts = posts
          .filter(p => !p.isDeleted)
          .map(post => ({ post, employeeName: undefined }));
        
        if (this.isAuthorized) {
          this.loadEmployeeNames();
        } else {
          this.isLoading = false;
        }
      },
      error: (error) => {
        this.errorMessage = error.message || 'Ошибка загрузки должностей';
        this.isLoading = false;
      }
    });
  }

  loadEmployeeNames(): void {
    // Load employee names for each post
    // This would require additional API calls or a different endpoint
    // For now, we'll just mark loading as complete
    this.isLoading = false;
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  goToPersonalCabinet(): void {
    const userId = this.authService.getUserId();
    if (userId) {
      this.router.navigate(['/cabinet'], { queryParams: { employeeId: userId } });
    } else {
      this.router.navigate(['/cabinet']);
    }
  }
}
