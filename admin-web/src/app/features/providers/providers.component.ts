import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProviderApiService } from '../../core/providers/provider-api.service';
import {
  ProviderApprovalStatus,
  ProviderListItem,
} from '../../shared/models/provider.models';

@Component({
  selector: 'app-providers',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
  ],
  templateUrl: './providers.component.html',
  styleUrl: './providers.component.scss',
})
export class ProvidersComponent implements OnInit {
  private readonly api = inject(ProviderApiService);
  private readonly fb = inject(FormBuilder);

  readonly providers = signal<ProviderListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly displayedColumns = [
    'name',
    'email',
    'services',
    'approvalStatus',
    'active',
    'documents',
    'actions',
  ];

  readonly filters = this.fb.nonNullable.group({
    search: [''],
    approvalStatus: ['' as ProviderApprovalStatus | ''],
  });

  constructor() {
    this.filters.controls.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());

    this.filters.controls.approvalStatus.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.load());
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const { search, approvalStatus } = this.filters.getRawValue();

    this.api.list({ search, approvalStatus }).subscribe({
      next: (providers) => {
        this.providers.set(providers);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Unable to load providers.');
        this.loading.set(false);
      },
    });
  }
}
