import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';
import { PaginatedTableComponent } from '../../components/paginated-table/paginated-table.component';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { Category } from '../../models/category/Category';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-categories-page-component',
  imports: [SectionHeaderComponent, PaginatedTableComponent],
  templateUrl: './categories-page-component.html',
  styleUrl: './categories-page-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesPageComponent {
  private readonly categoryService = inject(CategoryService);
  private latestLoadRequestId = 0;

  readonly isLoading = this.categoryService.isLoading;
  readonly rowsPerPageOptions = [10, 25, 50];
  readonly categories = signal<PaginatedResponse<Category> | null>(null);

  first = 0;
  rows = 10;
  currentPage = 1;

  constructor() {
    effect(() => {
      void this.loadCategories();
    });
  }

  addCategory(): void {
    // Placeholder for upcoming create-category flow.
  }

  onPageChange(event: PaginatorState): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? this.rows;
    this.currentPage = Math.floor(this.first / this.rows) + 1;

    void this.loadCategories();
  }

  private async loadCategories(): Promise<void> {
    const requestId = ++this.latestLoadRequestId;
    const response = await this.categoryService.getCategories(this.currentPage, this.rows);

    if (requestId !== this.latestLoadRequestId) {
      return;
    }

    this.categories.set(response);
  }
}
