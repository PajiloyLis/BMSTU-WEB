import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { PositionService } from '../../services/position.service';
import { EmployeeService } from '../../services/employee.service';
import { CompanyService } from '../../services/company.service';
import { AuthService } from '../../services/auth.service';
import { ScoreService } from '../../services/score.service';
import { PositionHierarchy } from '../../models/position.model';
import { Company } from '../../models/company.model';
import { Employee, CurrentEmployee } from '../../models/employee.model';
import { Score } from '../../models/score.model';

interface PositionNode {
  positionId: string;
  title: string;
  employeeName?: string;
  employeeId?: string;
  efficiency?: number;
  engagement?: number;
  competency?: number;
  children?: PositionNode[];
}

@Component({
  selector: 'app-positions-page',
  templateUrl: './positions-page.component.html',
  styleUrls: ['./positions-page.component.scss'],
})
export class PositionsPageComponent implements OnInit {
  isAuthorized: boolean = false;
  isFullTree: boolean = false;
  companyId: string | null = null;
  company: Company | null = null;
  tree: PositionNode | null = null;
  isLoading: boolean = false;
  errorMessage: string = '';
  userEmail: string | null = null;

  // Sidebar menu
  isMenuOpen: boolean = false;

  // Pan and zoom properties
  panX: number = 0;
  panY: number = 0;
  scale: number = 1;
  minScale: number = 0.5;
  maxScale: number = 3;
  
  // Pan state
  isPanning: boolean = false;
  lastPanX: number = 0;
  lastPanY: number = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private positionService: PositionService,
    private employeeService: EmployeeService,
    private companyService: CompanyService,
    private authService: AuthService,
    private scoreService: ScoreService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isAuthorized = this.authService.isAuthenticated();
    this.isFullTree = this.isAuthorized;
    this.userEmail = this.authService.getEmail();
    
    console.log('ngOnInit - isAuthorized:', this.isAuthorized);
    console.log('ngOnInit - authService.isAuthenticated():', this.authService.isAuthenticated());
    
    this.route.queryParams.subscribe(params => {
      this.companyId = params['companyId'] || null;
      if (this.companyId) {
        this.loadCompany();
        this.loadPositions();
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

  loadPositions(): void {
    if (!this.companyId) {
      console.error('loadPositions: companyId is not set');
      return;
    }

    console.log('loadPositions: Loading positions for companyId:', this.companyId);
    this.isLoading = true;
    this.errorMessage = '';

    this.positionService.getCompanyHeadPosition(this.companyId).subscribe({
      next: (headPosition) => {
        console.log('Company head position loaded:', headPosition);
        console.log('Head position id:', headPosition.id);
        this.errorMessage = '';
        
        // Получаем подчиненных головной позиции
        this.loadPositionHierarchy(headPosition.id);
      },
      error: (error) => {
        console.error('Error loading company head position:', error);
        console.error('Error details:', {
          url: error.url,
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          error: error.error
        });
        
        // 404 может означать, что у компании нет головной позиции - это нормально
        if (error.status === 404) {
          console.log('Company has no head position - this is normal');
          this.errorMessage = '';
          this.tree = {
            positionId: '',
            title: 'Нет позиций',
            children: []
          };
        } else {
          this.errorMessage = error.message || 'Ошибка загрузки позиций';
        }
        this.isLoading = false;
      }
    });
  }

  loadPositionHierarchy(headPositionId: string): void {
    // Шаг 1: Получаем подчиненных головной позиции (иерархия позиций с level относительно головной позиции)
    this.positionService.getSubordinates(headPositionId).subscribe({
      next: (positionHierarchy) => {
        console.log('Position hierarchy loaded:', positionHierarchy);
        
        if (positionHierarchy.length === 0) {
          // Нет подчиненных - загружаем только головную позицию
          this.positionService.getById(headPositionId).subscribe({
            next: (position) => {
              const rootNode: PositionNode = {
                positionId: position.id,
                title: position.title,
                children: []
              };
              // Загружаем сотрудников даже для одной позиции
              this.loadCurrentEmployeesForTree(rootNode);
            }
          });
          return;
        }

        // Строим дерево позиций из иерархии
        const positionMap = new Map<string, PositionNode>();
        
        // Сначала создаем узлы для всех позиций
        positionHierarchy.forEach(pos => {
          positionMap.set(pos.positionId, {
            positionId: pos.positionId,
            title: pos.title,
            children: []
          });
        });

        // Строим дерево на основе parentId и level
        const rootNodes: PositionNode[] = [];
        positionHierarchy.forEach(pos => {
          const node = positionMap.get(pos.positionId)!;
          if (pos.level === 1 || !pos.parentId || pos.parentId === headPositionId) {
            // Это прямые подчиненные головной позиции
            rootNodes.push(node);
          } else {
            // Это подчиненный другой позиции
            const parentNode = positionMap.get(pos.parentId);
            if (parentNode) {
              if (!parentNode.children) {
                parentNode.children = [];
              }
              parentNode.children.push(node);
            }
          }
        });

        // Создаем корневой узел с головной позицией
        this.positionService.getById(headPositionId).subscribe({
          next: (headPosition) => {
            const rootNode: PositionNode = {
              positionId: headPosition.id,
              title: headPosition.title,
              children: rootNodes
            };

            // Загружаем текущих сотрудников для всего дерева
            this.loadCurrentEmployeesForTree(rootNode);
          }
        });
      },
      error: (error) => {
        console.error('Error loading position hierarchy:', error);
        this.errorMessage = error.message || 'Ошибка загрузки позиций';
        this.isLoading = false;
      }
    });
  }

  /**
   * Загружает текущих сотрудников компании и обогащает дерево позиций именами
   */
  loadCurrentEmployeesForTree(positionTree: PositionNode): void {
    if (!this.companyId) {
      this.tree = positionTree;
      this.isLoading = false;
      return;
    }

    // Шаг 1: Получаем пары positionId → employeeId для данной компании
    this.employeeService.getCurrentEmployees(this.companyId).subscribe({
      next: (currentEmployees) => {
        console.log('Current employees loaded:', currentEmployees);

        if (currentEmployees.length === 0) {
          this.tree = positionTree;
          this.isLoading = false;
          return;
        }

        // Шаг 2: Для каждого employeeId загружаем информацию о сотруднике
        const employeeRequests = currentEmployees.map(ce =>
          this.employeeService.getById(ce.employeeId).pipe(
            map(employee => ({
              positionId: ce.positionId,
              employeeId: ce.employeeId,
              employeeName: employee.fullName,
              employeeEmail: employee.email
            })),
            catchError(error => {
              console.error(`Error loading employee ${ce.employeeId}:`, error);
              return of(null);
            })
          )
        );

        forkJoin(employeeRequests).subscribe({
          next: (employeeDataList) => {
            console.log('Employee data loaded:', employeeDataList);

            // Создаем карту positionId → { employeeId, employeeName }
            const employeeMap = new Map<string, { employeeId: string; employeeName: string }>();
            let currentUserEmployeeId: string | null = null;
            const currentEmail = this.authService.getEmail();

            employeeDataList.forEach(data => {
              if (data && data.employeeName) {
                employeeMap.set(data.positionId, {
                  employeeId: data.employeeId,
                  employeeName: data.employeeName
                });

                // Находим employeeId текущего авторизованного пользователя по email
                if (currentEmail && data.employeeEmail && data.employeeEmail.toLowerCase() === currentEmail.toLowerCase()) {
                  currentUserEmployeeId = data.employeeId;
                  console.log('Found current user employeeId:', currentUserEmployeeId, 'by email:', currentEmail);
                }
              }
            });

            console.log('Employee map:', Array.from(employeeMap.entries()));
            console.log('Current user employeeId:', currentUserEmployeeId);

            // Шаг 3: Обогащаем дерево именами сотрудников
            const enrichedTree = this.enrichTreeWithEmployees(positionTree, employeeMap);

            // Шаг 4: Если авторизован и нашли employeeId — загружаем последние оценки подчинённых
            if (this.isAuthorized && currentUserEmployeeId) {
              this.loadLastScoresForTree(enrichedTree, currentUserEmployeeId);
            } else {
              this.tree = enrichedTree;
              this.isLoading = false;
              setTimeout(() => {
                this.cdr.detectChanges();
              }, 0);
            }
          },
          error: (error) => {
            console.error('Error loading employees:', error);
            this.tree = positionTree;
            this.isLoading = false;
          }
        });
      },
      error: (error) => {
        console.error('Error loading current employees:', error);
        this.tree = positionTree;
        this.isLoading = false;
      }
    });
  }

  enrichTreeWithEmployees(node: PositionNode, employeeMap: Map<string, { employeeId: string; employeeName: string }>): PositionNode {
    const employeeData = employeeMap.get(node.positionId);

    const newNode: PositionNode = {
      positionId: node.positionId,
      title: node.title,
      children: node.children ? node.children.map(child => this.enrichTreeWithEmployees(child, employeeMap)) : undefined
    };

    if (employeeData) {
      newNode.employeeId = employeeData.employeeId;
      newNode.employeeName = employeeData.employeeName;
    }

    return newNode;
  }

  /**
   * Загружает последние оценки подчинённых авторизованного сотрудника и обогащает дерево
   */
  loadLastScoresForTree(positionTree: PositionNode, headEmployeeId: string): void {
    console.log('Loading last scores for headEmployeeId:', headEmployeeId);

    this.scoreService.getSubordinatesLastScores(headEmployeeId).subscribe({
      next: (scores) => {
        console.log('Last scores loaded:', scores);

        // Создаём карту: employeeId → последняя оценка
        const scoreMap = new Map<string, Score>();
        scores.forEach(score => {
          scoreMap.set(score.employeeId, score);
        });

        // Обогащаем дерево оценками
        const enrichedTree = this.enrichTreeWithScores(positionTree, scoreMap);

        this.tree = enrichedTree;
        this.isLoading = false;

        setTimeout(() => {
          this.cdr.detectChanges();
        }, 0);
      },
      error: (error) => {
        console.error('Error loading last scores:', error);
        // В случае ошибки отображаем дерево без оценок
        this.tree = positionTree;
        this.isLoading = false;
      }
    });
  }

  enrichTreeWithScores(node: PositionNode, scoreMap: Map<string, Score>): PositionNode {
    const score = node.employeeId ? scoreMap.get(node.employeeId) : undefined;

    const newNode: PositionNode = {
      ...node,
      children: node.children ? node.children.map(child => this.enrichTreeWithScores(child, scoreMap)) : undefined
    };

    if (score) {
      newNode.efficiency = score.efficiencyScore;
      newNode.engagement = score.engagementScore;
      newNode.competency = score.competencyScore;
    }

    return newNode;
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

  // Pan handlers
  onMouseDown(event: MouseEvent): void {
    if (event.button === 0) { // Left mouse button
      this.isPanning = true;
      this.lastPanX = event.clientX;
      this.lastPanY = event.clientY;
      event.preventDefault();
    }
  }

  onMouseMove(event: MouseEvent): void {
    if (this.isPanning) {
      const deltaX = event.clientX - this.lastPanX;
      const deltaY = event.clientY - this.lastPanY;
      
      this.panX += deltaX;
      this.panY += deltaY;
      
      this.lastPanX = event.clientX;
      this.lastPanY = event.clientY;
      
      this.constrainPan();
      this.cdr.detectChanges();
    }
  }

  onMouseUp(event: MouseEvent): void {
    if (event.button === 0) {
      this.isPanning = false;
    }
  }

  onMouseLeave(): void {
    this.isPanning = false;
  }

  // Zoom handler
  onWheel(event: WheelEvent): void {
    event.preventDefault();
    
    const delta = event.deltaY > 0 ? -0.1 : 0.1;
    const newScale = Math.max(this.minScale, Math.min(this.maxScale, this.scale + delta));
    
    if (newScale !== this.scale) {
      // Zoom towards mouse position
      const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
      const mouseX = event.clientX - rect.left;
      const mouseY = event.clientY - rect.top;
      
      const scaleChange = newScale / this.scale;
      this.panX = mouseX - (mouseX - this.panX) * scaleChange;
      this.panY = mouseY - (mouseY - this.panY) * scaleChange;
      
      this.scale = newScale;
      this.constrainPan();
      this.cdr.detectChanges();
    }
  }

  // Constrain pan to prevent content from going outside bounds
  private constrainPan(): void {
    // These values will be adjusted based on content size
    // For now, we'll use reasonable defaults
    const maxPan = 500;
    this.panX = Math.max(-maxPan, Math.min(maxPan, this.panX));
    this.panY = Math.max(-maxPan, Math.min(maxPan, this.panY));
  }

  // Reset pan and zoom
  resetView(): void {
    this.panX = 0;
    this.panY = 0;
    this.scale = 1;
    this.cdr.detectChanges();
  }

  // Get transform style for tree content
  getTransformStyle(): string {
    return `translate(${this.panX}px, ${this.panY}px) scale(${this.scale})`;
  }
}
