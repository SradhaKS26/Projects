import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProviderApiService } from '../../../core/providers/provider-api.service';
import { ServiceCategory } from '../../../shared/models/catalog.models';
import { CategoryDocumentRequirement } from '../../../shared/models/provider.models';

export interface DocumentRequirementsDialogData {
  category: ServiceCategory;
}

@Component({
  selector: 'app-document-requirements-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatProgressBarModule,
  ],
  templateUrl: './document-requirements-dialog.component.html',
  styleUrl: './document-requirements-dialog.component.scss',
})
export class DocumentRequirementsDialogComponent implements OnInit {
  private readonly api = inject(ProviderApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<DocumentRequirementsDialogComponent>);
  readonly data: DocumentRequirementsDialogData;

  readonly requirements = signal<CategoryDocumentRequirement[]>([]);
  readonly loading = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: [''],
    isRequired: [true],
    sortOrder: [0],
  });

  constructor(@Inject(MAT_DIALOG_DATA) data: DocumentRequirementsDialogData) {
    this.data = data;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getDocumentRequirements(this.data.category.id).subscribe({
      next: (requirements) => {
        this.requirements.set(requirements);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Unable to load document requirements.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  edit(requirement: CategoryDocumentRequirement): void {
    this.editingId.set(requirement.id);
    this.form.patchValue({
      name: requirement.name,
      description: requirement.description ?? '',
      isRequired: requirement.isRequired,
      sortOrder: requirement.sortOrder,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', description: '', isRequired: true, sortOrder: 0 });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      isRequired: value.isRequired,
      sortOrder: Number(value.sortOrder) || 0,
    };

    const editingId = this.editingId();
    const request = editingId
      ? this.api.updateDocumentRequirement(editingId, payload)
      : this.api.createDocumentRequirement(this.data.category.id, payload);

    request.subscribe({
      next: () => {
        this.cancelEdit();
        this.load();
      },
      error: (err) =>
        this.snackBar.open(err?.error?.message ?? 'Unable to save requirement.', 'Dismiss', {
          duration: 4000,
        }),
    });
  }

  remove(requirement: CategoryDocumentRequirement): void {
    if (!confirm(`Remove "${requirement.name}"?`)) {
      return;
    }

    this.api.deleteDocumentRequirement(requirement.id).subscribe({
      next: () => this.load(),
      error: (err) =>
        this.snackBar.open(err?.error?.message ?? 'Unable to remove requirement.', 'Dismiss', {
          duration: 4000,
        }),
    });
  }
}
