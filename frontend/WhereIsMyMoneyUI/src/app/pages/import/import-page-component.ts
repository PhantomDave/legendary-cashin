import { Component } from '@angular/core';
import { AccordionModule } from 'primeng/accordion';
import { EnableBankingImportComponent } from '../../components/enable-banking-import-component/enable-banking-import-component';
import { ButtonModule } from 'primeng/button';
import { EnableBankingConnectionsComponent } from '../../components/enable-banking-connections-component/enable-banking-connections-component';

@Component({
  selector: 'app-import-page-component',
  imports: [
    AccordionModule,
    EnableBankingImportComponent,
    ButtonModule,
    EnableBankingConnectionsComponent,
  ],
  templateUrl: './import-page-component.html',
  styleUrl: './import-page-component.scss',
})
export class ImportPageComponent {
  showEnableBankingForm = false;
  addAnotherConnection() {
    throw new Error('Method not implemented.');
  }
}
