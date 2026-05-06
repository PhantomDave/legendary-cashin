import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { Dialog } from 'primeng/dialog';
import { Password } from 'primeng/password';
import { ButtonDirective } from 'primeng/button';
import { AccountService } from '../../../../services/account.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-login-page-component',
  imports: [
    ReactiveFormsModule,
    InputGroupModule,
    InputTextModule,
    InputGroupAddon,
    Dialog,
    Password,
    ButtonDirective,
    RouterModule,
  ],
  templateUrl: './login-page-component.html',
  styleUrl: './login-page-component.scss',
})
export class LoginPageComponent {
  visible = true;
  private readonly formBuilder = inject(FormBuilder);
  readonly loginForm: FormGroup = this.formBuilder.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  private readonly accountService = inject(AccountService);

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const { username, password } = this.loginForm.getRawValue();
    await this.accountService.login(username, password);
  }

  isInvalid(controlName: 'username' | 'password'): boolean {
    const control = this.loginForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }
}
