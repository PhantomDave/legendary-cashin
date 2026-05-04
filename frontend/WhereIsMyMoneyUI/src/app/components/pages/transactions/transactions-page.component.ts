import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SectionHeaderComponent } from '../../section-header/section-header.component';

@Component({
  selector: 'app-transactions-page',
  imports: [TableModule, TagModule, SectionHeaderComponent],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsPageComponent {}
