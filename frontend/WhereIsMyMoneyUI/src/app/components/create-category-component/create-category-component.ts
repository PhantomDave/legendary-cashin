import { ChangeDetectionStrategy, Component, inject, model, output } from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputText } from 'primeng/inputtext';
import { PrimeTemplate } from 'primeng/api';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category/Category';

@Component({
  selector: 'app-create-category-component',
  imports: [
    Button,
    Dialog,
    InputGroup,
    InputGroupAddon,
    InputText,
    PrimeTemplate,
    ReactiveFormsModule,
  ],
  templateUrl: './create-category-component.html',
  styleUrl: './create-category-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateCategoryComponent {
  visible = model<boolean>(false);
  private readonly formBuilder = inject(FormBuilder);
  readonly categoryForm = this.formBuilder.group({
    categoryName: ['', [Validators.required, Validators.minLength(3)]],
    amount: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+(\.\d{1,2})?$/)]],
  });
  private readonly categoryService = inject(CategoryService);
  readonly categoryCreated = output<Category>();

  protected async onSubmit() {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const { categoryName, amount } = this.categoryForm.getRawValue();
    const createdCategory = await this.categoryService.createCategory({
      name: categoryName!,
      budget: amount!,
    });

    if (!createdCategory) {
      return;
    }

    this.visible.set(false);
    this.categoryForm.reset({
      categoryName: '',
      amount: 0,
    });
    this.categoryCreated.emit(createdCategory);
  }

  protected isInvalid(controlName: 'categoryName' | 'amount'): boolean {
    const control = this.categoryForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }
}
