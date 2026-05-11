import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EnableBankingImportComponent } from './enable-banking-import-component';

describe('EnableBankingImportComponent', () => {
  let component: EnableBankingImportComponent;
  let fixture: ComponentFixture<EnableBankingImportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnableBankingImportComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(EnableBankingImportComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
