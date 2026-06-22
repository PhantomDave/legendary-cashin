import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { FileUploadHandlerEvent, FileUploadModule } from 'primeng/fileupload';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-step-certificate',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputGroupModule,
    InputGroupAddon,
    TextareaModule,
    ToggleSwitchModule,
    FileUploadModule,
  ],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h3 class="text-lg font-semibold m-0 mb-2">Certificate</h3>
        <p class="text-sm text-surface-500 m-0">
          Upload or paste your certificate. You can switch between file upload and text input using
          the toggle below.
        </p>
      </div>

      <div
        class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 p-6"
      >
        <form [formGroup]="stepForm" class="flex flex-col gap-4">
          <div
            class="flex items-center gap-3 pb-4 border-b border-surface-200 dark:border-surface-700"
          >
            <p-toggleswitch formControlName="textAreaEnabled"></p-toggleswitch>
            <label class="text-sm font-medium m-0">
              @if (stepForm.get('textAreaEnabled')?.value) {
                <span>Paste Certificate Text</span>
              } @else {
                <span>Upload Certificate File</span>
              }
            </label>
          </div>

          <div class="flex flex-col gap-3">
            @if (!stepForm.get('textAreaEnabled')?.value) {
              <div>
                <label class="block text-sm font-medium mb-3">Certificate File *</label>
                <p-fileupload
                  name="certificateFile"
                  [customUpload]="true"
                  accept=".pem,.key,.txt,.crt"
                  [showUploadButton]="false"
                  [showCancelButton]="false"
                  chooseLabel="Choose Certificate"
                  (onSelect)="onFileUpload($event)"
                  class="align-middle w-full"
                ></p-fileupload>
                @if (stepForm.get('certificate')?.value) {
                  <small class="text-green-600 flex items-center gap-1 mt-2">
                    <i class="pi pi-check text-sm"></i>
                    Certificate file loaded
                  </small>
                }
              </div>
            } @else {
              <div>
                <label class="block text-sm font-medium mb-3">Certificate Text *</label>
                <p-inputgroup>
                  <p-inputgroup-addon><i class="pi pi-lock"></i></p-inputgroup-addon>
                  <textarea
                    rows="8"
                    cols="30"
                    pTextarea
                    formControlName="certificateText"
                    placeholder="-----BEGIN RSA PRIVATE KEY-----&#10;...&#10;-----END RSA PRIVATE KEY-----"
                    class="w-full"
                  >
                  </textarea>
                </p-inputgroup>
                @if (stepForm.get('certificateText')?.value) {
                  <small class="text-green-600 flex items-center gap-1 mt-2">
                    <i class="pi pi-check text-sm"></i>
                    Certificate text provided ({{ certificateLength() }} characters)
                  </small>
                }
              </div>
            }
          </div>
        </form>
      </div>
    </div>
  `,
})
export class StepCertificateComponent {
  private readonly formBuilder = inject(FormBuilder);

  @Input() certificate: string = '';
  @Input() textAreaEnabled: boolean = false;
  @Output() certificateChange = new EventEmitter<string>();
  @Output() textAreaEnabledChange = new EventEmitter<boolean>();

  readonly stepForm = this.formBuilder.group({
    certificate: [''],
    certificateText: [''],
    textAreaEnabled: [false],
  });

  constructor() {
    this.stepForm.get('textAreaEnabled')?.valueChanges.subscribe((value) => {
      this.textAreaEnabledChange.emit(value || false);
      // Clear the opposite field when toggling
      if (value) {
        this.stepForm.patchValue({ certificate: '' });
      } else {
        this.stepForm.patchValue({ certificateText: '' });
      }
    });

    this.stepForm.get('certificate')?.valueChanges.subscribe((value) => {
      this.certificateChange.emit(value || '');
    });

    this.stepForm.get('certificateText')?.valueChanges.subscribe((value) => {
      this.certificateChange.emit(value || '');
    });

    if (this.certificate) {
      this.stepForm.patchValue({ certificate: this.certificate });
    }
    this.stepForm.patchValue({ textAreaEnabled: this.textAreaEnabled });
  }

  onFileUpload(event: FileUploadHandlerEvent): void {
    const files = event.files;
    if (files && files.length > 0) {
      const file = files[0];
      if (!file) return;

      const reader = new FileReader();
      reader.onload = () => {
        const fileContent = reader.result as string;
        this.stepForm.patchValue({
          certificate: fileContent,
        });
      };
      reader.readAsText(file);
    }
  }

  certificateLength(): number {
    const cert = this.stepForm.get('certificateText')?.value || '';
    return cert.length;
  }

  isValid(): boolean {
    const certificate = this.stepForm.get('certificate')?.value;
    const certificateText = this.stepForm.get('certificateText')?.value;
    return !!(certificate || certificateText);
  }

  getValue(): string {
    const cert = this.stepForm.get('certificate')?.value;
    const text = this.stepForm.get('certificateText')?.value;
    return cert || text || '';
  }
}
