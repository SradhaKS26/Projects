import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  CatalogService,
  CatalogServicePayload,
  ServiceCategory,
} from '../../../shared/models/catalog.models';

export interface ServiceDialogData {
  service: CatalogService | null;
  categories: ServiceCategory[];
  defaultCategoryId?: string | null;
}

@Component({
  selector: 'app-service-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
  ],
  templateUrl: './service-dialog.component.html',
  styleUrl: './service-dialog.component.scss',
})
export class ServiceDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<ServiceDialogComponent, CatalogServicePayload>>(MatDialogRef);

  readonly isEdit: boolean;
  readonly categories: ServiceCategory[];

  readonly form = this.fb.nonNullable.group({
    categoryId: ['', [Validators.required]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(2000)]],
    basePrice: [0, [Validators.required, Validators.min(0)]],
    estimatedDurationMinutes: [null as number | null, [Validators.min(1)]],
    isActive: [true],
  });

  constructor(@Inject(MAT_DIALOG_DATA) data: ServiceDialogData) {
    this.isEdit = data.service !== null;
    this.categories = data.categories;

    if (data.service) {
      this.form.patchValue({
        categoryId: data.service.categoryId,
        name: data.service.name,
        description: data.service.description ?? '',
        basePrice: data.service.basePrice,
        estimatedDurationMinutes: data.service.estimatedDurationMinutes ?? null,
        isActive: data.service.isActive,
      });
    } else if (data.defaultCategoryId) {
      this.form.patchValue({ categoryId: data.defaultCategoryId });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.dialogRef.close({
      categoryId: value.categoryId,
      name: value.name.trim(),
      description: value.description.trim() || null,
      basePrice: Number(value.basePrice),
      estimatedDurationMinutes: value.estimatedDurationMinutes
        ? Number(value.estimatedDurationMinutes)
        : null,
      isActive: value.isActive,
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
