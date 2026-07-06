import { ChangeDetectionStrategy, Component, inject, model, output, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { RuleService } from '../../services/rule.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-apply-to-existing-component',
  imports: [Button, Dialog, CheckboxModule, ReactiveFormsModule],
  templateUrl: './apply-to-existing-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplyToExistingComponent {
  visible = model<boolean>(false);
  readonly applied = output<number>();

  private readonly fb = inject(FormBuilder);
  private readonly ruleService = inject(RuleService);
  private readonly toast = inject(ToastService);

  readonly isLoading = this.ruleService.isLoading;
  readonly previewCount = signal<number | null>(null);
  readonly previewLoaded = signal(false);

  readonly form = this.fb.group({
    overwriteExisting: new FormControl<boolean>(false, { nonNullable: true }),
  });

  private resetForm(): void {
    this.previewCount.set(null);
    this.previewLoaded.set(false);
    this.form.reset({ overwriteExisting: false });
  }

  protected async onPreview(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const count = await this.ruleService.countExisting({
      overwriteExisting: this.form.controls.overwriteExisting.value,
    });
    this.previewCount.set(count);
    this.previewLoaded.set(true);
  }

  protected async onApply(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const updated = await this.ruleService.applyToExisting({
      overwriteExisting: this.form.controls.overwriteExisting.value,
    });

    if (updated === null) return;

    this.toast.success('Rules applied', `Applied rules to ${updated} transaction(s).`);
    this.visible.set(false);
    this.resetForm();
    this.applied.emit(updated);
  }

  protected onHide(): void {
    this.resetForm();
  }
}
