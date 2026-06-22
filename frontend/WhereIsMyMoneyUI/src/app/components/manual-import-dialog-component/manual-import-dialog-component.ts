import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { EnableBankingBankSession } from '../../models/import/EnableBankingBankSession';

export interface ManualImportRequest {
  session: EnableBankingBankSession;
  from: Date;
}

@Component({
  selector: 'app-manual-import-dialog',
  standalone: true,
  imports: [FormsModule, Dialog, Button, DatePicker],
  templateUrl: './manual-import-dialog-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManualImportDialogComponent {
  visible = input<boolean>(false);
  session = input<EnableBankingBankSession | null>(null);

  visibleChange = output<boolean>();
  importRequested = output<ManualImportRequest>();

  selectedDate: Date | null = null;
  readonly today = new Date();

  onHide(): void {
    this.selectedDate = null;
  }

  cancel(): void {
    this.visibleChange.emit(false);
  }

  confirm(): void {
    const session = this.session();
    if (!this.selectedDate || !session) return;

    this.importRequested.emit({ session, from: this.selectedDate });
    this.visibleChange.emit(false);
  }
}
