import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduledPageComponent } from './scheduled-page-component';

describe('ScheduledPageComponent', () => {
  let component: ScheduledPageComponent;
  let fixture: ComponentFixture<ScheduledPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScheduledPageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduledPageComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
