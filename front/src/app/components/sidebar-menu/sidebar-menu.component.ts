import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { CompanyService } from '../../services/company.service';
import { AuthService } from '../../services/auth.service';
import { Company } from '../../models/company.model';

@Component({
  selector: 'app-sidebar-menu',
  templateUrl: './sidebar-menu.component.html',
  styleUrls: ['./sidebar-menu.component.scss'],
})
export class SidebarMenuComponent implements OnInit {
  @Input() isOpen: boolean = false;
  @Output() isOpenChange = new EventEmitter<boolean>();

  isAuthorized: boolean = false;
  companies: Company[] = [];

  constructor(
    private router: Router,
    private companyService: CompanyService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAuthorized = this.authService.isAuthenticated();
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies = companies.filter(c => !c.isDeleted);
      },
      error: (error) => {
        console.error('Error loading companies for sidebar:', error);
      }
    });
  }

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
  }

  goToHome(): void {
    this.close();
    this.router.navigate(['/home']);
  }

  goToCompanyPositions(companyId: string): void {
    this.close();
    this.router.navigate(['/positions'], { queryParams: { companyId } });
  }

  goToCompanyPosts(companyId: string): void {
    this.close();
    this.router.navigate(['/posts'], { queryParams: { companyId } });
  }

  goToLogin(): void {
    this.close();
    this.router.navigate(['/login']);
  }

  goToRegister(): void {
    this.close();
    this.router.navigate(['/register']);
  }
}

