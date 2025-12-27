import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CompanyService } from '../../services/company.service';
import { AuthService } from '../../services/auth.service';
import { Company } from '../../models/company.model';

@Component({
  selector: 'app-home-page',
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.scss'],
})
export class HomePageComponent implements OnInit {
  companies: Company[] = [];
  expandedIndex: number | null = null;
  isLoading: boolean = false;
  errorMessage: string = '';
  isAuthorized: boolean = false;
  userEmail: string | null = null;

  constructor(
    private companyService: CompanyService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isAuthorized = this.authService.isAuthenticated();
    this.userEmail = this.authService.getEmail();
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies = companies.filter(c => !c.isDeleted);
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = error.message || 'Ошибка загрузки компаний';
        this.isLoading = false;
      }
    });
  }

  toggleCompany(index: number): void {
    this.expandedIndex = this.expandedIndex === index ? null : index;
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  goToPositions(companyId: string): void {
    this.router.navigate(['/positions'], { queryParams: { companyId } });
  }

  goToPosts(companyId: string): void {
    this.router.navigate(['/posts'], { queryParams: { companyId } });
  }

  goToPersonalCabinet(): void {
    const userId = this.authService.getUserId();
    if (userId) {
      this.router.navigate(['/cabinet'], { queryParams: { employeeId: userId } });
    } else {
      this.router.navigate(['/cabinet']);
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
  }
}
