import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateBudgetComponent } from './create-budget-component';

describe('CreateBudgetComponent', () => {
  let component: CreateBudgetComponent;
  let fixture: ComponentFixture<CreateBudgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateBudgetComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateBudgetComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
