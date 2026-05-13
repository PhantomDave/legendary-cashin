import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScheduledPageComponent } from './scheduled-page-component';
import { provideTestDependencies } from '../../../testing/test-providers';

describe('ScheduledPageComponent', () => {
  let component: ScheduledPageComponent;
  let fixture: ComponentFixture<ScheduledPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScheduledPageComponent],
      providers: provideTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduledPageComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
