# Enable Banking Stepper - Quick Start

## How It Works

### 1. Basic Usage

```typescript
// In your component TypeScript
import { EnableBankingStepperComponent } from './path-to-component';

@Component({
  imports: [EnableBankingStepperComponent],
})
export class MyComponent {
  isModalOpen = signal(false);

  openModal() {
    this.isModalOpen.set(true);
  }

  onSuccess() {
    console.log('Integration created successfully!');
    // Refresh data here
  }
}
```

### 2. HTML Template

```html
<!-- Button to open stepper -->
<p-button label="Add Integration" (click)="openModal()"></p-button>

<!-- Stepper component with two-way binding -->
<app-enable-banking-stepper-component
  [(isOpen)]="isModalOpen()"
  (onSuccess)="onSuccess()"
></app-enable-banking-stepper-component>
```

## Component Flow

```
┌─────────────────────────────────────┐
│ Modal Closed (isOpen = false)        │
└────────────┬────────────────────────┘
             │
             │ User clicks button
             ↓
┌─────────────────────────────────────┐
│ Modal Opens (isOpen = true)          │
│ Shows Step 1: Application ID         │
└────────────┬────────────────────────┘
             │
             │ User enters Application ID
             ↓
┌─────────────────────────────────────┐
│ Step 1 Valid                         │
│ "Next" button enabled                │
└────────────┬────────────────────────┘
             │
             │ User clicks Next
             ↓
┌─────────────────────────────────────┐
│ Step 2: Certificate Upload/Paste     │
└────────────┬────────────────────────┘
             │
             │ User uploads/pastes certificate
             ↓
┌─────────────────────────────────────┐
│ Step 2 Valid                         │
│ "Next" button enabled                │
└────────────┬────────────────────────┘
             │
             │ User clicks Next
             ↓
┌─────────────────────────────────────┐
│ Step 3: Review & Confirm             │
│ Shows summary of provided info       │
└────────────┬────────────────────────┘
             │
             │ All data valid
             ↓
┌─────────────────────────────────────┐
│ "Submit" button enabled              │
└────────────┬────────────────────────┘
             │
             │ User clicks Submit
             ↓
┌─────────────────────────────────────┐
│ Submission in progress               │
│ Submit button shows loading          │
└────────────┬────────────────────────┘
             │
             │ API Response
             ├──────────────┬──────────────┐
             ↓              ↓
        Success        Error
             │              │
    Modal closes      Error displayed
    onSuccess()       User can retry
```

## Step-by-Step Details

### Step 1: Application ID
- **What it does**: Collects user's Enable Banking application ID
- **Validation**: Required field, cannot be empty
- **What to look for**: Alphanumeric application ID from Enable Banking dashboard

### Step 2: Certificate
- **What it does**: Collects the certificate (file upload or text)
- **Options**:
  - Upload .pem, .key, .txt, or .crt files
  - Or paste certificate text directly
- **Validation**: Either file or text must be provided
- **Visual feedback**: Shows "✓ Certificate loaded" when valid

### Step 3: Review & Confirm
- **What it does**: Shows a summary of all entered data
- **Display includes**:
  - Application ID (entered in Step 1)
  - Certificate status and character count
  - Information about what happens after submission
- **No validation**: Just informational before final submission

## State Management

The stepper uses Angular signals for reactive state:

```typescript
// In enable-banking-stepper-component.ts
applicationId = signal('');        // Step 1 data
certificate = signal('');           // Step 2 data
textAreaEnabled = signal(false);     // Step 2 mode toggle
isLoading = signal(false);           // Submission state
errorMessage = signal('');           // Error display
```

## Customization

### Change Step Titles
Edit the `header` attribute in stepper panels:
```html
<p-stepper-panel header="My Custom Title">
```

### Modify Validation Rules
Edit `canProceedToStepN()` methods in the main component:
```typescript
canProceedToStep2(): boolean {
  // Add your custom validation here
  return !!this.applicationId();
}
```

### Change API Service
Update the import and inject statement:
```typescript
private readonly myService = inject(MyService);
```

Then update `onSubmit()` to call your service method.

## Common Tasks

### Add Another Input Field to Step 1
1. Add field to `stepForm` in `step-application-id.ts`
2. Update template with new input
3. Add validation rule
4. Emit value change in constructor

### Accept Different File Types
In `step-certificate.ts`, update the `accept` attribute:
```html
<p-fileupload accept=".pem,.key,.txt,.crt,.pfx">
```

### Add Another Step
1. Create new component `steps/step-name.ts`
2. Add to imports in main stepper
3. Add `<p-stepper-panel>` in template
4. Add validation method `canProceedToStepN()`
5. Update previous step's "Next" disabled condition

## Troubleshooting

### Modal doesn't open
- Check that `[(isOpen)]="showModal"` is properly bound
- Verify `showModal` is a signal: `signal(false)`

### Next button disabled
- Ensure the form control has a value
- Check that `canProceedToStepN()` returns true
- Verify form validation is correct

### File upload not working
- Check that file extension is in `accept` attribute
- Verify FileUploadModule is imported
- Check browser console for JavaScript errors

### Submit button doesn't work
- Ensure all steps are complete
- Check that `canSubmit()` returns true
- Verify ImportService is properly injected
- Check network tab to see API response

## Files Overview

| File | Purpose |
|------|---------|
| `enable-banking-stepper-component.ts` | Main component, state management, API calls |
| `enable-banking-stepper-component.html` | Modal dialog and stepper panels |
| `enable-banking-stepper-component.scss` | Custom styling for stepper |
| `steps/step-application-id.ts` | Step 1 component |
| `steps/step-certificate.ts` | Step 2 component |
| `steps/step-review.ts` | Step 3 component |
