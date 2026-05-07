import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly messageService = inject(MessageService);

  success(summary: string, detail?: string): void {
    const message: { severity: string; summary: string; detail?: string } = {
      severity: 'success',
      summary,
    };
    if (detail !== undefined) {
      message.detail = detail;
    }
    this.messageService.add(message);
  }

  error(summary: string, detail?: string): void {
    const message: { severity: string; summary: string; detail?: string } = {
      severity: 'error',
      summary,
    };
    if (detail !== undefined) {
      message.detail = detail;
    }
    this.messageService.add(message);
  }

  info(summary: string, detail?: string): void {
    const message: { severity: string; summary: string; detail?: string } = {
      severity: 'info',
      summary,
    };
    if (detail !== undefined) {
      message.detail = detail;
    }
    this.messageService.add(message);
  }

  warn(summary: string, detail?: string): void {
    const message: { severity: string; summary: string; detail?: string } = {
      severity: 'warn',
      summary,
    };
    if (detail !== undefined) {
      message.detail = detail;
    }
    this.messageService.add(message);
  }
}
