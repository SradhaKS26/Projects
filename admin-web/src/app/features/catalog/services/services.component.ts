import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CurrencyPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApiService } from '../../../core/catalog/catalog-api.service';
import { AuthService } from '../../../core/authentication/auth.service';
import {
  CatalogService,
  CatalogServicePayload,
  ServiceCategory,
} from '../../../shared/models/catalog.models';
import { ServiceDialogComponent } from './service-dialog.component';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent implements OnInit {
  private readonly api = inject(CatalogApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  readonly canManage = computed(() => this.auth.hasPermission('ManageServices'));
  readonly services = signal<CatalogService[]>([]);
  readonly categories = signal<ServiceCategory[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly displayedColumns = computed(() => {
    const columns = ['name', 'category', 'basePrice', 'duration', 'status'];
    if (this.canManage()) {
      columns.push('actions');
    }
    return columns;
  });

  readonly filters = this.fb.nonNullable.group({
    search: [''],
    categoryId: [''],
    includeInactive: [false],
  });

  constructor() {
    this.filters.controls.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.loadServices());

    this.filters.controls.categoryId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadServices());

    this.filters.controls.includeInactive.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadServices());
  }

  ngOnInit(): void {
    if (this.canManage()) {
      this.filters.controls.includeInactive.setValue(true, { emitEvent: false });
    }

    this.api.getCategories(this.canManage()).subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.categories.set([]),
    });

    this.loadServices();
  }

  loadServices(): void {
    this.loading.set(true);
    this.error.set(null);

    const { search, categoryId, includeInactive } = this.filters.getRawValue();

    this.api.getServices({ search, categoryId: categoryId || null, includeInactive }).subscribe({
      next: (services) => {
        this.services.set(services);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Unable to load services.'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    if (this.categories().length === 0) {
      this.notify('Create a service category first.');
      return;
    }

    this.dialog
      .open(ServiceDialogComponent, {
        data: {
          service: null,
          categories: this.categories(),
          defaultCategoryId: this.filters.controls.categoryId.value || null,
        },
      })
      .afterClosed()
      .subscribe((payload?: CatalogServicePayload) => {
        if (!payload) {
          return;
        }

        this.api.createService(payload).subscribe({
          next: () => {
            this.notify('Service created.');
            this.loadServices();
          },
          error: (err) => this.notify(this.messageFrom(err, 'Unable to create service.')),
        });
      });
  }

  openEdit(service: CatalogService): void {
    this.dialog
      .open(ServiceDialogComponent, {
        data: { service, categories: this.categories() },
      })
      .afterClosed()
      .subscribe((payload?: CatalogServicePayload) => {
        if (!payload) {
          return;
        }

        this.api.updateService(service.id, payload).subscribe({
          next: () => {
            this.notify('Service updated.');
            this.loadServices();
          },
          error: (err) => this.notify(this.messageFrom(err, 'Unable to update service.')),
        });
      });
  }

  remove(service: CatalogService): void {
    const confirmed = confirm(
      `Remove "${service.name}"? If it has past requests it will be deactivated instead of deleted.`,
    );

    if (!confirmed) {
      return;
    }

    this.api.deleteService(service.id).subscribe({
      next: () => {
        this.notify('Service removed.');
        this.loadServices();
      },
      error: (err) => this.notify(this.messageFrom(err, 'Unable to remove service.')),
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 4000 });
  }

  private messageFrom(err: unknown, fallback: string): string {
    const apiMessage = (err as { error?: { message?: string } })?.error?.message;
    return apiMessage?.trim() ? apiMessage : fallback;
  }
}
