import { Component, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { FileUploadHandlerEvent, FileUploadModule } from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-enable-banking-import-component',
  imports: [
    ReactiveFormsModule,
    InputGroupModule,
    InputGroupAddon,
    InputTextModule,
    TextareaModule,
    FormsModule,
    ToggleSwitchModule,
    FileUploadModule,
    ButtonModule,
  ],
  templateUrl: './enable-banking-import-component.html',
  styleUrl: './enable-banking-import-component.scss',
})
export class EnableBankingImportComponent {
  private readonly formBuilder = inject(FormBuilder);
  textAreaEnabled = false;
  readonly enableBankingForm = this.formBuilder.group({
    applicationId: ['', Validators.required],
    certificate: ['', Validators.required],
    certificateText: [''],
    textAreaEnabled: [false],
  });

  onFileUpload(event: FileUploadHandlerEvent): void {
    const files = event.files;
    if (files && files.length > 0) {
      const file = files[0];
      if (!file) return;

      const reader = new FileReader();

      reader.onload = () => {
        const fileContent = reader.result as string;
        this.enableBankingForm.patchValue({
          certificate: fileContent,
        });
      };

      reader.readAsText(file);
    }
  }

  onSubmit(): void {
    if (this.enableBankingForm.valid) {
      const formData = this.enableBankingForm.value;
      console.log('Form submitted with data:', formData);
    } else {
      console.log('Form is invalid');
    }
  }
}
