import { Component } from '@angular/core';
import { AccordionModule } from 'primeng/accordion';
import { EnableBankingImportComponent } from '../../components/enable-banking-import-component/enable-banking-import-component';

@Component({
  selector: 'app-import-page-component',
  imports: [AccordionModule, EnableBankingImportComponent],
  templateUrl: './import-page-component.html',
  styleUrl: './import-page-component.scss',
})
export class ImportPageComponent {}
