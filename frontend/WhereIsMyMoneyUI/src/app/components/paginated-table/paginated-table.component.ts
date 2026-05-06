import {
  ChangeDetectionStrategy,
  Component,
  contentChild,
  input,
  output,
  TemplateRef,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TableModule } from 'primeng/table';
import { PaginatorState } from 'primeng/paginator';

@Component({
  selector: 'app-paginated-table',
  imports: [TableModule, NgTemplateOutlet],
  templateUrl: './paginated-table.component.html',
  styleUrl: './paginated-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginatedTableComponent {
  readonly items = input<unknown[]>([]);
  readonly loading = input(false);
  readonly rows = input(10);
  readonly first = input(0);
  readonly totalRecords = input(0);
  readonly rowsPerPageOptions = input<number[]>([10, 25, 50]);
  readonly minWidth = input('50rem');
  readonly size = input<'small' | 'large' | undefined>('small');
  readonly showGridlines = input(true);
  readonly showCurrentPageReport = input(true);
  readonly currentPageReportTemplate = input('Showing {first} to {last} of {totalRecords} entries');

  readonly headerTemplate = contentChild.required<TemplateRef<unknown>>('headerTemplate');
  readonly bodyTemplate =
    contentChild.required<TemplateRef<{ $implicit: unknown }>>('bodyTemplate');

  readonly pageChange = output<PaginatorState>();

  onPageChange(event: PaginatorState): void {
    this.pageChange.emit(event);
  }
}
