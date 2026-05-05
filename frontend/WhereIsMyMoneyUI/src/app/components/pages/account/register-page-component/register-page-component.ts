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
  selector: 'app-register-page-component',
  imports: [
    InputGroupModule,
    InputTextModule,
    ReactiveFormsModule,
    InputGroupAddon,
    Dialog,
    Password,
    ButtonDirective,
    RouterModule,
  ],
  templateUrl: './register-page-component.html',
  styleUrl: './register-page-component.scss',
})
export class RegisterPageComponent {
  visible = true;
  private readonly formBuilder = inject(FormBuilder);
  readonly registerForm: FormGroup = this.formBuilder.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  private readonly accountService = inject(AccountService);

  async onSubmit(): Promise<void> {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { email, username, password } = this.registerForm.getRawValue();
    await this.accountService.register(email, username, password);
  }

  isInvalid(controlName: 'username' | 'email' | 'password'): boolean {
    const control = this.registerForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }
}
