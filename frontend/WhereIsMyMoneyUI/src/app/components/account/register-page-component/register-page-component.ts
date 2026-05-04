import {Component, inject} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {InputGroupModule} from 'primeng/inputgroup';
import {InputTextModule} from 'primeng/inputtext';
import {InputGroupAddon} from 'primeng/inputgroupaddon';
import {Dialog} from 'primeng/dialog';
import {Password} from 'primeng/password';
import {ButtonDirective} from 'primeng/button';

@Component({
  selector: 'app-register-page-component',
  imports: [InputGroupModule, InputTextModule, ReactiveFormsModule, InputGroupAddon, Dialog, Password, ButtonDirective],
  templateUrl: './register-page-component.html',
  styleUrl: './register-page-component.scss',
})
export class RegisterPageComponent {
  visible = true;
  private readonly formBuilder = inject(FormBuilder);

  readonly registerForm = this.formBuilder.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const registerPayload = this.registerForm.getRawValue();
    console.log('Register payload:', registerPayload);
  }

  isInvalid(controlName: 'username' | 'email' | 'password'): boolean {
    const control = this.registerForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }
}
