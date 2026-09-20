import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ReasonDialogData {
  title: string;
  action: string;
  required: boolean;
}

@Component({
  selector: 'app-reason-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Reason</mat-label>
          <textarea matInput formControlName="reason" rows="4"></textarea>
          @if (form.controls.reason.invalid && form.controls.reason.touched) {
            <mat-error>A reason is required.</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" (click)="dialogRef.close()">Cancel</button>
        <button mat-flat-button color="primary" type="submit">{{ data.action }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [
    `
      .full-width {
        width: 100%;
        min-width: 320px;
      }
    `,
  ],
})
export class ReasonDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly dialogRef = inject<MatDialogRef<ReasonDialogComponent, string | null>>(MatDialogRef);
  readonly data: ReasonDialogData;

  readonly form = this.fb.nonNullable.group({
    reason: [''],
  });

  constructor(@Inject(MAT_DIALOG_DATA) data: ReasonDialogData) {
    this.data = data;
    if (data.required) {
      this.form.controls.reason.addValidators([Validators.required, Validators.minLength(3)]);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const reason = this.form.controls.reason.value.trim();
    this.dialogRef.close(reason || null);
  }
}
