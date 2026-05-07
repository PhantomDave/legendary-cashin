import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';
import { PaginatedTableComponent } from '../../components/paginated-table/paginated-table.component';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { Category } from '../../models/category/Category';
import { CategoryService } from '../../services/category.service';
import { CreateCategoryComponent } from '../../components/create-category-component/create-category-component';
import { Inplace } from 'primeng/inplace';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastService } from '../../services/toast.service';

interface EditValues {
  name: string;
  budget: number;
}

@Component({
  selector: 'app-categories-page-component',
  imports: [
    SectionHeaderComponent,
    PaginatedTableComponent,
    CreateCategoryComponent,
    InputNumberModule,
    Inplace,
    InputText,
    Button,
    FormsModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './categories-page-component.html',
  styleUrl: './categories-page-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesPageComponent {
  readonly rowsPerPageOptions = [10, 25, 50];
  readonly categories = signal<PaginatedResponse<Category> | null>(null);
  readonly editingValues = signal<Record<number, EditValues>>({});
  first = 0;
  rows = 10;
  currentPage = 1;
  isCreateCategoryModalVisible = false;
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);
  private readonly categoryService = inject(CategoryService);
  readonly isLoading = this.categoryService.isLoading;
  private latestLoadRequestId = 0;

  constructor() {
    effect(() => {
      this.loadCategories();
    });
  }

  getEditingValue(id: number): EditValues {
    return this.editingValues()[id]!;
  }

  addCategory(): void {
    this.isCreateCategoryModalVisible = true;
  }

  onPageChange(event: PaginatorState): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? this.rows;
    this.currentPage = Math.floor(this.first / this.rows) + 1;
    void this.loadCategories();
  }

  startEdit(item: Category): void {
    this.editingValues.update((current) => ({
      ...current,
      [item.id]: { name: item.name, budget: item.budget },
    }));
  }

  cancelEdit(id: number): void {
    this.editingValues.update((current) => {
      const { [id]: _, ...rest } = current;
      return rest;
    });
  }

  async saveEdit(item: Category, inplaceRef: Inplace): Promise<void> {
    const values = this.editingValues()[item.id];
    if (!values) return;

    const updated = await this.categoryService.updateCategory(item.id, {
      name: values.name,
      budget: values.budget,
    });

    if (updated) {
      this.categories.update((current) => {
        if (!current) return current;
        return {
          ...current,
          items: current.items.map((c) => (c.id === item.id ? updated : c)),
        };
      });
      this.cancelEdit(item.id);
      inplaceRef.deactivate();
    }
  }

  protected deleteCategory(event: Event, id: number) {
    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: 'Do you want to delete this category?',
      header: 'Delete Category',
      icon: 'pi pi-info-circle',
      rejectLabel: 'Cancel',
      rejectButtonProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptButtonProps: {
        label: 'Delete',
        severity: 'danger',
      },

      accept: () => {
        void this.categoryService.deleteCategory(id).then(() => {
          this.toast.success('Category deleted');
          void this.loadCategories();
        });
      },
    });
  }

  private async loadCategories(): Promise<void> {
    const requestId = ++this.latestLoadRequestId;
    const response = await this.categoryService.getCategories(this.currentPage, this.rows);
    if (requestId !== this.latestLoadRequestId) return;
    this.categories.set(response);
  }

  onCategoryCreated() {
    void this.loadCategories();
  }
}
