import { ChangeDetectionStrategy, Component, inject, model, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ToastService } from '../../services/toast.service';
import { ImportService } from '../../services/import.service';
import { EnableBanking } from '../../models/import/EnableBanking';
import { AspspData } from '../../models/import/AspspData';
import { StepperModule } from 'primeng/stepper';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';

import { EU_COUNTRIES } from '../../constants/countries';

@Component({
  selector: 'app-configure-aspsp-dialog-component',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    Button,
    Dialog,
    MultiSelectModule,
    ProgressSpinnerModule,
    StepperModule,
    TagModule,
    TableModule,
  ],
  templateUrl: './configure-aspsp-dialog-component.html',
  styleUrl: './configure-aspsp-dialog-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfigureAspspDialogComponent {
  visible = model<boolean>(false);
  integration = model<EnableBanking | null>(null);

  readonly countries = signal(EU_COUNTRIES);
  readonly isLoading = signal(false);
  readonly currentStep = signal(1);
  readonly availableAsps = signal<AspspData[]>([]);
  readonly selectedAsps = signal<string[]>([]);

  private readonly importService = inject(ImportService);
  private readonly toast = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);

  readonly countriesForm = this.formBuilder.group({
    countries: [[] as string[], Validators.required],
  });

  // Computed properties for review
  readonly selectedCountriesDisplay = computed(() => {
    const selected = this.countriesForm.get('countries')?.value || [];
    return this.countries().filter((c) => selected.includes(c.value));
  });

  readonly selectedAspsDisplay = computed(() => {
    const selected = this.selectedAsps();
    return this.availableAsps().filter((a) => selected.includes(a.name));
  });

  async onCountriesNext(): Promise<void> {
    if (this.countriesForm.invalid) {
      this.toast.error('Please select at least one country');
      return;
    }

    const selected = this.countriesForm.get('countries')?.value || [];
    this.isLoading.set(true);
    try {
      const asps = await this.importService.configureCountries(this.integration()!.id, selected);

      if (asps && asps.length > 0) {
        this.availableAsps.set(asps);
        this.selectedAsps.set([]);
        this.currentStep.set(2);
      } else {
        this.toast.error('No banks found for selected countries');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  onAspNext(): void {
    if (this.selectedAsps().length === 0) {
      this.toast.error('Please select at least one bank');
      return;
    }
    this.currentStep.set(3);
  }

  async onSubmit(): Promise<void> {
    if (this.selectedAsps().length === 0) {
      this.toast.error('Please select at least one bank');
      return;
    }

    const currentIntegration = this.integration();
    if (!currentIntegration) {
      this.toast.error('Integration not found');
      return;
    }

    const selectedCountries = this.countriesForm.get('countries')?.value || [];
    const selectedAspsps = this.selectedAsps();

    this.isLoading.set(true);
    try {
      const success = await this.importService.saveAspspsConfiguration(
        currentIntegration.id,
        selectedAspsps,
        selectedCountries,
      );

      if (success) {
        this.toast.success('Configuration updated successfully');
        this.visible.set(false);
        this.reset();
      } else {
        this.toast.error('Failed to configure banks and countries');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  toggleAspSelection(aspName: string): void {
    const selected = this.selectedAsps();
    const index = selected.indexOf(aspName);
    if (index > -1) {
      selected.splice(index, 1);
    } else {
      selected.push(aspName);
    }
    this.selectedAsps.set([...selected]);
  }

  isAspSelected(aspName: string): boolean {
    return this.selectedAsps().includes(aspName);
  }

  onDialogShow(): void {
    let countries: string[] = [];
    const configuration = this.integration()?.configuration;
    if (configuration) {
      try {
        const cfg = JSON.parse(configuration);
        countries = cfg.Countries ?? cfg.countries ?? [];
      } catch {
        // malformed JSON — leave countries empty
      }
    }
    this.countriesForm.get('countries')?.setValue(countries, { emitEvent: false });
    this.currentStep.set(1);
  }

  onDialogHide(): void {
    this.reset();
  }

  private reset(): void {
    this.countriesForm.reset({ countries: [] });
    this.currentStep.set(1);
    this.availableAsps.set([]);
    this.selectedAsps.set([]);
  }
}
