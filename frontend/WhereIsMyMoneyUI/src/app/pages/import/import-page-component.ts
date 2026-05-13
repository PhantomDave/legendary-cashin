import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AccordionModule } from 'primeng/accordion';
import { ButtonModule } from 'primeng/button';
import { EnableBankingConnectionsComponent } from '../../components/enable-banking-connections-component/enable-banking-connections-component';
import { EnableBankingStepperComponent } from '../../components/enable-banking-stepper-component/enable-banking-stepper-component';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-import-page-component',
  imports: [
    AccordionModule,
    ButtonModule,
    EnableBankingConnectionsComponent,
    EnableBankingStepperComponent,
  ],
  templateUrl: './import-page-component.html',
  styleUrl: './import-page-component.scss',
})
export class ImportPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  showEnableBankingForm = signal(false);

  ngOnInit(): void {
    const bankConnected = this.route.snapshot.queryParamMap.get('bankConnected');
    if (bankConnected === 'true') {
      this.toast.success('Bank account connected successfully!');
    }
  }

  openEnableBankingModal(): void {
    this.showEnableBankingForm.set(true);
  }

  onEnableBankingSuccess(): void {
    // Refresh the connections list or show success message
    this.showEnableBankingForm.set(false);
  }
}
