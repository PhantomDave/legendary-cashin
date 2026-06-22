import { EnvironmentProviders, Provider } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { MessageService } from 'primeng/api';

/**
 * Common providers required by most components under test.
 * Mirrors the providers registered in app.config.ts but uses
 * noop animations and an empty route table so tests run fast.
 */
export function provideTestDependencies(): Array<Provider | EnvironmentProviders> {
  return [MessageService, provideRouter([]), provideHttpClient(), provideNoopAnimations()];
}
