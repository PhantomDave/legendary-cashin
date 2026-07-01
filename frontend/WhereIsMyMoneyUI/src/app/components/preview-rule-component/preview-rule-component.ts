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
import { TableModule } from 'primeng/table';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RuleService } from '../../services/rule.service';
import { Transaction } from '../../models/transaction/Transaction';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-preview-rule-component',
  imports: [Button, Dialog, TableModule, PaginatorModule, CurrencyPipe, DatePipe],
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
       const open = this.visible();
       if (!open) {
         this.result.set(null);
         this.currentPage.set(1);
         return;
       }

       const id = this.ruleId();
       if (id === null) return;

       // Read pagination signals to create dependencies for re-loading on pagination changes
       void this.currentPage();
       void this.pageSize();

       void this.loadPreview(id);
     });
   }

   private async loadPreview(ruleId: number): Promise<void> {
     const data = await this.ruleService.previewRule(ruleId, this.currentPage(), this.pageSize());
     this.result.set(data);
   }

   onPageChange(event: PaginatorState): void {
     this.currentPage.set(Math.floor((event.first ?? 0) / (event.rows ?? 10)) + 1);
     this.pageSize.set(event.rows ?? 10);
   }

  getCategoryNames(categoryIds: number[]): string {
    const all = this.categoryService.categories();
    return categoryIds.map((id) => all.find((c) => c.id === id)?.name ?? id).join(', ');
  }
}
