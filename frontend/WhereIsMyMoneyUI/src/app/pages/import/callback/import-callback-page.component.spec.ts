import {TestBed} from '@angular/core/testing';
import {signal} from '@angular/core';
import {ActivatedRoute, convertToParamMap, Router} from '@angular/router';
import {vi} from 'vitest';
import {ImportCallbackPageComponent} from './import-callback-page.component';
import {ImportService} from '../../../services/import.service';
import {ToastService} from '../../../services/toast.service';

type SetupOptions = {
  queryParams: Record<string, string>;
  completeBankAuthResult?: boolean;
  importError?: string | null;
};

function setup(options: SetupOptions) {
  const completeBankAuthCalls: Array<[string, string]> = [];
  const completeBankAuth = async (code: string, state: string): Promise<boolean> => {
    completeBankAuthCalls.push([code, state]);
    return options.completeBankAuthResult ?? true;
  };

  const importServiceMock = {
    completeBankAuth,
    error: signal<string | null>(options.importError ?? null),
  } as Pick<ImportService, 'completeBankAuth' | 'error'>;

  const navigateCalls: Array<[string[], Record<string, unknown>]> = [];
  const navigate = async (
    commands: string[],
    extras: Record<string, unknown>,
  ): Promise<boolean> => {
    navigateCalls.push([commands, extras]);
    return true;
  };
  const routerMock = { navigate } as Pick<Router, 'navigate'>;
  const toastServiceMock = {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
    warn: vi.fn(),
  } as Pick<ToastService, 'success' | 'error' | 'info' | 'warn'>;

  TestBed.configureTestingModule({
    imports: [ImportCallbackPageComponent],
    providers: [
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            queryParamMap: convertToParamMap(options.queryParams),
          },
        },
      },
      { provide: ImportService, useValue: importServiceMock },
      { provide: Router, useValue: routerMock },
      { provide: ToastService, useValue: toastServiceMock },
    ],
  });

  const fixture = TestBed.createComponent(ImportCallbackPageComponent);
  const component = fixture.componentInstance;

  return { fixture, component, completeBankAuthCalls, navigateCalls };
}

describe('ImportCallbackPageComponent', () => {

  it('shows provider error when error query param exists', async () => {
    const { fixture, component, completeBankAuthCalls } = setup({
      queryParams: {
        error: 'access_denied',
        error_description: 'User canceled consent',
      },
    });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.status()).toBe('error');
    expect(component.errorMessage()).toBe('User canceled consent');
    expect(completeBankAuthCalls.length).toBe(0);
  });

  it('shows validation error when code/state query params are missing', async () => {
    const { fixture, component, completeBankAuthCalls } = setup({
      queryParams: {},
    });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.status()).toBe('error');
    expect(component.errorMessage()).toBe('Missing authorization code or state.');
    expect(completeBankAuthCalls.length).toBe(0);
  });

  it('completes auth and navigates back with success flag', async () => {
    const { fixture, component, completeBankAuthCalls, navigateCalls } = setup({
      queryParams: {
        code: 'auth-code',
        state: 'auth-state',
      },
      completeBankAuthResult: true,
    });

    vi.useFakeTimers();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(completeBankAuthCalls).toEqual([['auth-code', 'auth-state']]);
    expect(component.status()).toBe('success');

    vi.advanceTimersByTime(2000);
    await fixture.whenStable();
    vi.useRealTimers();

    expect(navigateCalls).toEqual([
      [['/configuration/import'], { queryParams: { refreshed: 'true' } }],
    ]);
  });

  it('shows service error when completing auth fails', async () => {
    const { fixture, component } = setup({
      queryParams: {
        code: 'auth-code',
        state: 'auth-state',
      },
      completeBankAuthResult: false,
      importError: 'State expired',
    });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.status()).toBe('error');
    expect(component.errorMessage()).toBe('State expired');
  });
});



