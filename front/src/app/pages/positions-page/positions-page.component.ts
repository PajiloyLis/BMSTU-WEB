import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { PositionService } from '../../services/position.service';
import { PositionHistoryService } from '../../services/position-history.service';
import { EmployeeService } from '../../services/employee.service';
import { CompanyService } from '../../services/company.service';
import { AuthService } from '../../services/auth.service';
import { ScoreService } from '../../services/score.service';
import { PositionHierarchyWithEmployee } from '../../models/position-history.model';
import { PositionHierarchy } from '../../models/position.model';
import { Company } from '../../models/company.model';
import { Employee } from '../../models/employee.model';
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
    private positionHistoryService: PositionHistoryService,
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
              this.tree = {
                positionId: position.id,
                title: position.title,
                children: []
              };
              this.isLoading = false;
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

            // Для авторизованных пользователей загружаем данные о сотрудниках
            if (this.isAuthorized) {
              this.loadEmployeesForPositions(headPositionId, rootNode);
            } else {
              this.tree = rootNode;
              this.isLoading = false;
            }
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

  loadEmployeesForPositions(headPositionId: string, positionTree: PositionNode): void {
    // Шаг 2: headEmployeeId - это ID текущего авторизованного сотрудника
    const headEmployeeId = this.authService.getUserId();
    
    if (!headEmployeeId) {
      // Если нет авторизованного пользователя, просто отображаем позиции без сотрудников
      this.tree = positionTree;
      this.isLoading = false;
      return;
    }

    // Получаем текущих подчиненных текущего сотрудника
    this.loadEmployeeHierarchy(headEmployeeId, positionTree);
  }

  loadEmployeeHierarchy(headEmployeeId: string, positionTree: PositionNode): void {
    // Шаг 3: Получаем текущих подчиненных сотрудника (иерархия с сотрудниками, level относительно headEmployeeId)
    this.positionHistoryService.getCurrentSubordinatesPositionHistories(headEmployeeId).subscribe({
      next: (employeeHierarchy) => {
        console.log('Employee hierarchy loaded:', employeeHierarchy);
        
        if (employeeHierarchy.length === 0) {
          // Нет подчиненных - просто отображаем позиции без сотрудников
          this.tree = positionTree;
          this.isLoading = false;
          return;
        }

        // Вычисляем дату два месяца назад
        const twoMonthsAgo = new Date();
        twoMonthsAgo.setMonth(twoMonthsAgo.getMonth() - 2);
        const startDate = twoMonthsAgo.toISOString().split('T')[0]; // Формат YYYY-MM-DD

        // Для каждого employeeId из иерархии загружаем информацию о сотруднике и его оценки за последние 2 месяца
        const employeeRequests = employeeHierarchy.map(emp => 
          forkJoin({
            employee: this.employeeService.getById(emp.employeeId).pipe(
              catchError(error => {
                console.error(`Error loading employee ${emp.employeeId}:`, error);
                return of(null);
              })
            ),
            scores: this.scoreService.getByEmployeeId(emp.employeeId, 1, 12, startDate).pipe(
              catchError(error => {
                console.error(`Error loading scores for employee ${emp.employeeId}:`, error);
                return of([]);
              })
            )
          }).pipe(
            map(result => ({
              positionId: emp.positionId,
              employeeId: emp.employeeId,
              employeeName: result.employee?.fullName,
              scores: result.scores
            }))
          )
        );

        // Загружаем всех сотрудников и их оценки параллельно
        forkJoin(employeeRequests).subscribe({
          next: (employeeData) => {
            console.log('Employee data with scores loaded:', employeeData);
            
            // Создаем карту positionId -> employeeId, employeeName и последние оценки
            const employeeMap = new Map<string, { 
              employeeId: string; 
              employeeName: string;
              efficiency?: number;
              engagement?: number;
              competency?: number;
            }>();
            
            employeeData.forEach(data => {
              if (data.employeeName) {
                if (data.scores && data.scores.length > 0) {
                  // Берем последнюю оценку (самую свежую) из оценок за последние 2 месяца
                  const latestScore = data.scores.sort((a, b) => 
                    new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
                  )[0];
                  
                  employeeMap.set(data.positionId, {
                    employeeId: data.employeeId,
                    employeeName: data.employeeName,
                    efficiency: latestScore.efficiencyScore,
                    engagement: latestScore.engagementScore,
                    competency: latestScore.competencyScore
                  });
                } else {
                  // Есть сотрудник, но нет оценок за последние 2 месяца - показываем серые кружки
                  employeeMap.set(data.positionId, {
                    employeeId: data.employeeId,
                    employeeName: data.employeeName
                    // efficiency, engagement, competency остаются undefined - будут серые кружки
                  });
                }
              }
            });

            console.log('Employee map created:', Array.from(employeeMap.entries()));
            console.log('Position tree before enrichment:', JSON.stringify(positionTree, null, 2));

            // Создаем новое дерево с данными о сотрудниках (не мутируем существующее)
            const enrichedTree = this.enrichTreeWithEmployeesNew(positionTree, employeeMap);
            
            console.log('Position tree after enrichment:', JSON.stringify(enrichedTree, null, 2));
            console.log('isAuthorized:', this.isAuthorized);
            
            // Устанавливаем новое дерево
            this.tree = enrichedTree;
            this.isLoading = false;
            
            // Принудительно обновляем представление
            setTimeout(() => {
              this.cdr.detectChanges();
              console.log('Tree after detectChanges:', this.tree);
            }, 0);
          },
          error: (error) => {
            console.error('Error loading employees:', error);
            // В случае ошибки просто отображаем позиции без сотрудников
            this.tree = positionTree;
            this.isLoading = false;
          }
        });
      },
      error: (error) => {
        console.error('Error loading employee hierarchy:', error);
        // В случае ошибки просто отображаем позиции без сотрудников
        this.tree = positionTree;
        this.isLoading = false;
      }
    });
  }

  enrichTreeWithEmployees(node: PositionNode, employeeMap: Map<string, { employeeId: string; employeeName: string }>): void {
    // Обновляем текущий узел
    const employeeData = employeeMap.get(node.positionId);
    console.log(`Enriching node ${node.positionId} (${node.title}):`, employeeData);
    if (employeeData) {
      node.employeeId = employeeData.employeeId;
      node.employeeName = employeeData.employeeName;
      console.log(`Set employeeName for ${node.positionId}: ${node.employeeName}`);
    } else {
      console.log(`No employee data found for position ${node.positionId}`);
    }

    // Рекурсивно обновляем дочерние узлы
    if (node.children) {
      node.children.forEach(child => {
        this.enrichTreeWithEmployees(child, employeeMap);
      });
    }
  }

  enrichTreeWithEmployeesNew(node: PositionNode, employeeMap: Map<string, { 
    employeeId: string; 
    employeeName: string;
    efficiency?: number;
    engagement?: number;
    competency?: number;
  }>): PositionNode {
    // Создаем новый узел (не мутируем существующий)
    const employeeData = employeeMap.get(node.positionId);
    console.log(`Enriching node ${node.positionId} (${node.title}):`, employeeData);
    
    const newNode: PositionNode = {
      positionId: node.positionId,
      title: node.title,
      children: node.children ? node.children.map(child => this.enrichTreeWithEmployeesNew(child, employeeMap)) : undefined
    };

    if (employeeData) {
      newNode.employeeId = employeeData.employeeId;
      newNode.employeeName = employeeData.employeeName;
      newNode.efficiency = employeeData.efficiency;
      newNode.engagement = employeeData.engagement;
      newNode.competency = employeeData.competency;
      console.log(`Set employeeName for ${node.positionId}: ${newNode.employeeName}`);
      console.log(`Set scores for ${node.positionId}:`, {
        efficiency: newNode.efficiency,
        engagement: newNode.engagement,
        competency: newNode.competency
      });
    } else {
      console.log(`No employee data found for position ${node.positionId}`);
    }

    return newNode;
  }

  private deepCopy<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
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
