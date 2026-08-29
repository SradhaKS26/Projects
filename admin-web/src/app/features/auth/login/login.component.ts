import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/authentication/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['admin@servicemanagement.local', [Validators.required, Validators.email]],
    password: ['ChangeMe!Admin123', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: async () => {
        this.loading.set(false);
        if (!this.auth.isAuthenticated()) {
          this.error.set('Login succeeded but session was not stored. Check browser storage settings.');
          return;
        }
        await this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.loading.set(false);
        const apiMessage =
          err?.error?.message ||
          err?.error?.errors?.[0] ||
          err?.message ||
          'Login failed. Check API connectivity and credentials.';
        this.error.set(apiMessage);
      },
    });
  }
}
