# Enable Banking Stepper Component

A multi-step wizard component for setting up Enable Banking integration.

## Components

### Main Stepper Component
**File:** `enable-banking-stepper-component.ts`

The parent component that orchestrates the entire stepper flow. It manages:
- Modal dialog state (`isOpen`)
- Form data (applicationId, certificate, textAreaEnabled)
- Loading states and error messages
- Validation and submission logic
- Event emissions for success/close

**Usage:**
```html
<app-enable-banking-stepper-component
  [(isOpen)]="showModal"
  (onSuccess)="onSuccess()"
></app-enable-banking-stepper-component>
```

**Properties:**
- `@Input() isOpen`: Boolean to control dialog visibility
- `@Output() isOpenChange`: Two-way binding for dialog state
- `@Output() onSuccess`: Emitted when integration is successfully created

### Step Components

#### Step 1: Application ID
**File:** `steps/step-application-id.ts`

Collects the Enable Banking application ID. Includes:
- Required field validation
- Real-time value emission

#### Step 2: Certificate
**File:** `steps/step-certificate.ts`

Allows users to upload or paste their certificate. Features:
- Toggle between file upload and text input
- File reader for certificate upload
- Real-time validation
- Visual feedback when certificate is loaded

#### Step 3: Review & Confirm
**File:** `steps/step-review.ts`

Shows a summary of the provided information before submission. Displays:
- Application ID
- Certificate status (with character count)
- Information about what happens after submission

## Features

- **Multi-step validation**: Each step validates before allowing progression
- **Modal integration**: Uses PrimeNG Dialog for modal display
- **Responsive design**: Built with Tailwind CSS utilities
- **Error handling**: Displays user-friendly error messages
- **Loading states**: Shows loading indicator during submission
- **Signal-based state**: Uses Angular signals for reactive state management

## Integration with Import Page

The stepper is integrated into the import page (`import-page-component.ts`):

1. Modal button in accordion header triggers `openEnableBankingModal()`
2. Stepper component is displayed in modal dialog
3. On successful submission, `onEnableBankingSuccess()` is called
4. Modal closes automatically on success or manual close

## Styling

Custom stepper styles are defined in `enable-banking-stepper-component.scss`:
- Removes default borders
- Transparent backgrounds
- Custom header styling for active/hover states
- Clean spacing and layout

## API Integration

The component uses `ImportService` to:
- Create Enable Banking integration with provided credentials
- Handle async submission with loading state
- Emit success event on completion
- Display error messages on failure
