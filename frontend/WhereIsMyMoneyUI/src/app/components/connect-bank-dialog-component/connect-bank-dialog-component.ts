import {ChangeDetectionStrategy, Component, inject, input, output, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Dialog} from 'primeng/dialog';
import {Button} from 'primeng/button';
import {TableModule} from 'primeng/table';
import {ProgressSpinnerModule} from 'primeng/progressspinner';
import {TagModule} from 'primeng/tag';
import {MultiSelectModule} from 'primeng/multiselect';
import {ImportService} from '../../services/import.service';
import {ToastService} from '../../services/toast.service';
import {EnableBanking} from '../../models/import/EnableBanking';
import {AspspData} from '../../models/import/AspspData';
import {EU_COUNTRIES} from '../../constants/countries';

@Component({
  selector: 'app-connect-bank-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Dialog,
    Button,
    TableModule,
    ProgressSpinnerModule,
    TagModule,
    MultiSelectModule,
  ],
  templateUrl: './connect-bank-dialog-component.html',
  styleUrl: './connect-bank-dialog-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectBankDialogComponent {
  visible = input<boolean>(false);
  visibleChange = output<boolean>();
  integration = input<EnableBanking | null>(null);

  readonly allCountries = EU_COUNTRIES;
  readonly isLoading = signal(false);
  readonly connecting = signal('');
  readonly aspsps = signal<AspspData[]>([]);
  readonly showCountryPicker = signal(false);

  manualCountries: string[] = [];

  private readonly importService = inject(ImportService);
  private readonly toast = inject(ToastService);

  async onShow(): Promise<void> {
    const integ = this.integration();
    if (!integ) return;

    let countries: string[] = [];
    if (integ.configuration) {
      try {
        const cfg = JSON.parse(integ.configuration);
        countries = cfg.Countries ?? cfg.countries ?? [];
      } catch {
        // malformed JSON — fall through to picker
      }
    }

    if (countries.length === 0) {
      this.showCountryPicker.set(true);
      return;
    }

    await this.fetchAspsps(countries);
  }

  onHide(): void {
    this.aspsps.set([]);
    this.showCountryPicker.set(false);
    this.manualCountries = [];
  }

  async fetchAspsps(countries: string[]): Promise<void> {
    const integ = this.integration();
    if (!integ) return;
    this.isLoading.set(true);
    try {
      const asps = await this.importService.configureCountries(integ.id, countries);
      this.aspsps.set(asps);
      if (asps.length === 0) {
        this.toast.warn('No banks found for the selected countries.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  async connect(asp: AspspData): Promise<void> {
    const integ = this.integration();
    if (!integ) return;
    const key = asp.name + asp.country;
    this.connecting.set(key);
    try {
      const result = await this.importService.startBankAuth(integ.id, asp.name, asp.country);
      if (result?.url) {
        window.location.href = result.url;
      } else {
        this.toast.error('Failed to start bank authentication.');
      }
    } finally {
      this.connecting.set('');
    }
  }
}
