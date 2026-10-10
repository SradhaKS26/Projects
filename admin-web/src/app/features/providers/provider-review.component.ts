import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProviderApiService } from '../../core/providers/provider-api.service';
import { ProviderProfile } from '../../shared/models/provider.models';
import { ReasonDialogComponent } from './reason-dialog.component';

@Component({
  selector: 'app-provider-review',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressBarModule,
  ],
  templateUrl: './provider-review.component.html',
  styleUrl: './provider-review.component.scss',
})
export class ProviderReviewComponent implements OnInit {
  private readonly api = inject(ProviderApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly profile = signal<ProviderProfile | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Provider not found.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.api.getById(id).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Unable to load provider.');
        this.loading.set(false);
      },
    });
  }

  approve(): void {
    const profile = this.profile();
    if (!profile) {
      return;
    }

    this.api.approve(profile.id).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.notify('Provider approved. Pending service applications are now registered.');
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to approve provider.'),
    });
  }

  reject(): void {
    const profile = this.profile();
    if (!profile) {
      return;
    }

    this.dialog
      .open(ReasonDialogComponent, {
        data: { title: 'Reject application', action: 'Reject', required: true },
      })
      .afterClosed()
      .subscribe((reason?: string | null) => {
        if (!reason) {
          return;
        }

        this.api.reject(profile.id, reason).subscribe({
          next: (updated) => {
            this.profile.set(updated);
            this.notify('Provider rejected.');
          },
          error: (err) => this.notify(err?.error?.message ?? 'Unable to reject provider.'),
        });
      });
  }

  suspend(): void {
    const profile = this.profile();
    if (!profile) {
      return;
    }

    this.dialog
      .open(ReasonDialogComponent, {
        data: { title: 'Suspend provider', action: 'Suspend', required: true },
      })
      .afterClosed()
      .subscribe((reason?: string | null) => {
        if (!reason) {
          return;
        }

        this.api.suspend(profile.id, reason).subscribe({
          next: (updated) => {
            this.profile.set(updated);
            this.notify('Provider suspended.');
          },
          error: (err) => this.notify(err?.error?.message ?? 'Unable to suspend provider.'),
        });
      });
  }

  reinstate(): void {
    const profile = this.profile();
    if (!profile) {
      return;
    }

    this.api.reinstate(profile.id).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.notify('Provider reinstated.');
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to reinstate provider.'),
    });
  }

  toggleActive(): void {
    const profile = this.profile();
    if (!profile) {
      return;
    }

    this.api.setActive(profile.id, !profile.isActive).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.notify(updated.isActive ? 'Provider activated.' : 'Provider deactivated.');
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to update active flag.'),
    });
  }

  missingNames(provider: ProviderProfile): string {
    return provider.missingRequiredDocuments.map((d) => d.name).join(', ');
  }

  openDocument(documentId: string, fileName: string): void {
    this.api.downloadDocument(documentId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.target = '_blank';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.notify('Unable to download document.'),
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 4000 });
  }
}
