import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { CommonModule } from '@angular/common';
import { EnableBankingBankSession } from '../../models/import/EnableBankingBankSession';

export interface ForceSyncRequest {
  session: EnableBankingBankSession;
  startDate: Date;
  endDate: Date;
}

@Component({
  selector: 'app-force-sync-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, Dialog, Button, DatePicker],
  templateUrl: './force-sync-dialog-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForceSyncDialogComponent {
  visible = input<boolean>(false);
  session = input<EnableBankingBankSession | null>(null);
  isLoading = input<boolean>(false);

  visibleChange = output<boolean>();
  forceSyncRequested = output<ForceSyncRequest>();

  selectedStartDate: Date | null = null;
  selectedEndDate: Date | null = null;
  readonly today = new Date();

  onHide(): void {
    this.selectedStartDate = null;
    this.selectedEndDate = null;
  }

  cancel(): void {
    this.visibleChange.emit(false);
  }

  confirm(): void {
    const session = this.session();
    if (!this.selectedStartDate || !this.selectedEndDate || !session) return;

    // Ensure start date is before end date
    if (this.selectedStartDate >= this.selectedEndDate) {
      return;
    }

    this.forceSyncRequested.emit({
      session,
      startDate: this.selectedStartDate,
      endDate: this.selectedEndDate,
    });
    this.visibleChange.emit(false);
  }

  get isFormValid(): boolean {
    return (
      this.selectedStartDate !== null &&
      this.selectedEndDate !== null &&
      this.selectedStartDate < this.selectedEndDate
    );
  }

  get formatLastSync(): string {
    const session = this.session();
    if (!session || !session.lastImportAtUtc) {
      return 'Never';
    }
    return new Date(session.lastImportAtUtc).toLocaleString();
  }
}
