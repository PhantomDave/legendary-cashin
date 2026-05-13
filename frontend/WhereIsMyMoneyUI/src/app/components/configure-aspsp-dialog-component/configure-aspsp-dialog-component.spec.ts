import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfigureAspspDialogComponent } from './configure-aspsp-dialog-component';
import { provideTestDependencies } from '../../../testing/test-providers';

describe('ConfigureAspspDialogComponent', () => {
  let component: ConfigureAspspDialogComponent;
  let fixture: ComponentFixture<ConfigureAspspDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfigureAspspDialogComponent],
      providers: provideTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(ConfigureAspspDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
