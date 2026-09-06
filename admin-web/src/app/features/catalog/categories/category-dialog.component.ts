import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {
  ServiceCategory,
  ServiceCategoryPayload,
} from '../../../shared/models/catalog.models';

export interface CategoryDialogData {
  category: ServiceCategory | null;
}

@Component({
  selector: 'app-category-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
  ],
  templateUrl: './category-dialog.component.html',
  styleUrl: './category-dialog.component.scss',
})
export class CategoryDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<CategoryDialogComponent, ServiceCategoryPayload>>(MatDialogRef);

  readonly isEdit: boolean;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(2000)]],
    imageUrl: ['', [Validators.maxLength(1024)]],
    isActive: [true],
  });

  constructor(@Inject(MAT_DIALOG_DATA) data: CategoryDialogData) {
    this.isEdit = data.category !== null;

    if (data.category) {
      this.form.patchValue({
        name: data.category.name,
        description: data.category.description ?? '',
        imageUrl: data.category.imageUrl ?? '',
        isActive: data.category.isActive,
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.dialogRef.close({
      name: value.name.trim(),
      description: value.description.trim() || null,
      imageUrl: value.imageUrl.trim() || null,
      isActive: value.isActive,
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
