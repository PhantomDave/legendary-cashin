import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EnableBankingConnectionsComponent } from './enable-banking-connections-component';
import { provideTestDependencies } from '../../../testing/test-providers';

describe('EnableBankingConnectionsComponent', () => {
  let component: EnableBankingConnectionsComponent;
  let fixture: ComponentFixture<EnableBankingConnectionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnableBankingConnectionsComponent],
      providers: provideTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(EnableBankingConnectionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
