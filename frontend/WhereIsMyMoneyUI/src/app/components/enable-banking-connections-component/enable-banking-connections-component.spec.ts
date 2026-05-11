import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EnableBankingConnectionsComponent } from './enable-banking-connections-component';

describe('EnableBankingConnectionsComponent', () => {
  let component: EnableBankingConnectionsComponent;
  let fixture: ComponentFixture<EnableBankingConnectionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnableBankingConnectionsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(EnableBankingConnectionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
