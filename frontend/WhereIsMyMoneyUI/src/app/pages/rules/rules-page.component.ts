import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { TableModule, TableRowReorderEvent } from 'primeng/table';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { Button } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { RuleService } from '../../services/rule.service';
import { CategoryService } from '../../services/category.service';
import { BudgetService } from '../../services/budget.service';
import { ToastService } from '../../services/toast.service';
import { Rule } from '../../models/rule/Rule';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { CreateRuleComponent } from '../../components/create-rule-component/create-rule-component';
import { EditRuleComponent } from '../../components/edit-rule-component/edit-rule-component';
import { PreviewRuleComponent } from '../../components/preview-rule-component/preview-rule-component';
import { ApplyToExistingComponent } from '../../components/apply-to-existing-component/apply-to-existing-component';

@Component({
  selector: 'app-rules-page',
  imports: [
    SectionHeaderComponent,
    TableModule,
    PaginatorModule,
    Button,
    TagModule,
    TooltipModule,
    ConfirmDialogModule,
    CreateRuleComponent,
    EditRuleComponent,
    PreviewRuleComponent,
    ApplyToExistingComponent,
  ],
  providers: [ConfirmationService],
  templateUrl: './rules-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RulesPageComponent {
  private readonly ruleService = inject(RuleService);
  private readonly categoryService = inject(CategoryService);
  private readonly budgetService = inject(BudgetService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);

  readonly isLoading = this.ruleService.isLoading;
  readonly rules = signal<PaginatedResponse<Rule> | null>(null);
  readonly currentPage = signal(1);
  readonly rows = signal(25);
  readonly first = signal(0);

  readonly showCreate = signal(false);
  readonly showApply = signal(false);
  readonly selectedRule = signal<Rule | null>(null);
  readonly showEdit = signal(false);
  readonly showPreview = signal(false);

  constructor() {
    effect(() => {
      void this.loadRules();
    });
  }

  private async loadRules(): Promise<void> {
    const response = await this.ruleService.getRules(this.currentPage(), this.rows());
    this.rules.set(response);
  }

  onPageChange(event: PaginatorState): void {
    this.first.set(event.first ?? 0);
    this.rows.set(event.rows ?? this.rows());
    this.currentPage.set(Math.floor((event.first ?? 0) / (event.rows ?? this.rows())) + 1);
    void this.loadRules();
  }

  openCreate(): void {
    this.showCreate.set(true);
  }

  openApply(): void {
    this.showApply.set(true);
  }

  openEdit(rule: Rule): void {
    this.selectedRule.set(rule);
    this.showEdit.set(true);
  }

  openPreview(rule: Rule): void {
    this.selectedRule.set(rule);
    this.showPreview.set(true);
  }

  async toggleActive(rule: Rule): Promise<void> {
    const result = await this.ruleService.patchRule(rule.id, { isActive: !rule.isActive });
    if (result) {
      this.rules.update((current) => {
        if (!current) return current;
        return {
          ...current,
          items: current.items.map((r) => (r.id === rule.id ? { ...r, isActive: !r.isActive } : r)),
        };
      });
    }
  }

  async onRowReorder(event: TableRowReorderEvent): Promise<void> {
    const items = this.rules()?.items;
    if (!items) return;
    const dragIndex = event.dragIndex;
    const dropIndex = event.dropIndex;
    if (dragIndex === undefined || dropIndex === undefined) return;

    const reordered = [...items];
    const [moved] = reordered.splice(dragIndex, 1);
    reordered.splice(dropIndex, 0, moved);

    this.rules.update((current) => (current ? { ...current, items: reordered } : current));

    const ruleIds = reordered.map((r) => r.id);
    await this.ruleService.reorderRules(ruleIds);
  }

  onRuleCreated(rule: Rule): void {
    this.rules.update((current) => {
      if (!current) return current;
      return { ...current, items: [...current.items, rule], totalCount: current.totalCount + 1 };
    });
  }

  onRuleUpdated(rule: Rule): void {
    this.rules.update((current) => {
      if (!current) return current;
      return { ...current, items: current.items.map((r) => (r.id === rule.id ? rule : r)) };
    });
  }

  onApplied(): void {
    void this.loadRules();
  }

  confirmDelete(event: Event, rule: Rule): void {
    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: `Delete rule "${rule.name}"?`,
      header: 'Delete Rule',
      icon: 'pi pi-exclamation-triangle',
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      accept: () => void this.deleteRule(rule.id),
    });
  }

  private async deleteRule(id: number): Promise<void> {
    const ok = await this.ruleService.deleteRule(id);
    if (ok) {
      this.toast.success('Rule deleted');
      void this.loadRules();
    }
  }

  getCategoryNames(categoryIds: number[]): string {
    const all = this.categoryService.categories();
    return categoryIds.map((id) => all.find((c) => c.id === id)?.name ?? id).join(', ');
  }

  getBudgetName(budgetId: number | undefined): string {
    if (!budgetId) return '—';
    const all = this.budgetService.budgets();
    return all.find((b) => b.id === budgetId)?.name ?? String(budgetId);
  }

  formatAmountRange(rule: Rule): string {
    if (rule.minAmount == null && rule.maxAmount == null) return '—';
    const min = rule.minAmount != null ? rule.minAmount.toFixed(2) : '–∞';
    const max = rule.maxAmount != null ? rule.maxAmount.toFixed(2) : '+∞';
    return `${min} – ${max}`;
  }
}
