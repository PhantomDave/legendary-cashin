import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { PrimeTemplate } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RuleService } from '../../services/rule.service';
import { Transaction } from '../../models/transaction/Transaction';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-preview-rule-component',
  imports: [Button, Dialog, PrimeTemplate, TableModule, PaginatorModule, CurrencyPipe, DatePipe],
  templateUrl: './preview-rule-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreviewRuleComponent {
  visible = model<boolean>(false);
  readonly ruleId = input<number | null>(null);

  private readonly ruleService = inject(RuleService);
  private readonly categoryService = inject(CategoryService);

  readonly result = signal<PaginatedResponse<Transaction> | null>(null);
  readonly isLoading = this.ruleService.isLoading;
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  constructor() {
    effect(() => {
      const id = this.ruleId();
      const open = this.visible();
      if (open && id !== null) {
        void this.loadPreview(id);
      } else if (!open) {
        this.result.set(null);
        this.currentPage.set(1);
      }
    });
  }

  private async loadPreview(ruleId: number): Promise<void> {
    const data = await this.ruleService.previewRule(ruleId, this.currentPage(), this.pageSize());
    this.result.set(data);
  }

  onPageChange(event: PaginatorState): void {
    const id = this.ruleId();
    if (id === null) return;
    this.currentPage.set(Math.floor((event.first ?? 0) / (event.rows ?? 10)) + 1);
    this.pageSize.set(event.rows ?? 10);
    void this.loadPreview(id);
  }

  getCategoryNames(categoryIds: number[]): string {
    const all = this.categoryService.categories();
    return categoryIds.map((id) => all.find((c) => c.id === id)?.name ?? id).join(', ');
  }
}
