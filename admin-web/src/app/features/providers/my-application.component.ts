import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { CatalogApiService } from '../../core/catalog/catalog-api.service';
import { ProviderApiService } from '../../core/providers/provider-api.service';
import { CatalogService, ServiceCategory } from '../../shared/models/catalog.models';
import {
  AvailabilityStatus,
  CategoryDocumentRequirement,
  ProviderProfile,
} from '../../shared/models/provider.models';

@Component({
  selector: 'app-my-application',
  standalone: true,
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './my-application.component.html',
  styleUrl: './my-application.component.scss',
})
export class MyApplicationComponent implements OnInit {
  private readonly providers = inject(ProviderApiService);
  private readonly catalog = inject(CatalogApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<ProviderProfile | null>(null);
  readonly categories = signal<ServiceCategory[]>([]);
  readonly services = signal<CatalogService[]>([]);
  readonly requirements = signal<CategoryDocumentRequirement[]>([]);
  readonly selectedServiceIds = signal<string[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    description: [''],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      profile: this.providers.getMine(),
      categories: this.catalog.getCategories(false),
      services: this.catalog.getServices(),
    }).subscribe({
      next: ({ profile, categories, services }) => {
        this.profile.set(profile);
        this.categories.set(categories);
        this.services.set(services);
        this.form.controls.description.setValue(profile.description ?? '');
        this.selectedServiceIds.set(profile.services.map((s) => s.serviceId));
        this.loadRequirementsForSelection();
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Unable to load your application.');
        this.loading.set(false);
      },
    });
  }

  isSelected(serviceId: string): boolean {
    return this.selectedServiceIds().includes(serviceId);
  }

  toggleService(serviceId: string, checked: boolean): void {
    const current = new Set(this.selectedServiceIds());
    if (checked) {
      current.add(serviceId);
    } else {
      current.delete(serviceId);
    }
    this.selectedServiceIds.set([...current]);
    this.loadRequirementsForSelection();
  }

  saveApplication(): void {
    const ids = this.selectedServiceIds();
    if (ids.length === 0) {
      this.notify('Select at least one service.');
      return;
    }

    this.saving.set(true);
    this.providers.saveApplication(this.form.controls.description.value.trim() || null, ids).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.saving.set(false);
        this.notify('Application saved. An administrator will review your documents.');
        this.loadRequirementsForSelection();
      },
      error: (err) => {
        this.saving.set(false);
        this.notify(err?.error?.message ?? 'Unable to save application.');
      },
    });
  }

  upload(requirementId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    this.providers.uploadDocument(requirementId, file).subscribe({
      next: () => {
        this.notify('Document uploaded.');
        this.reload();
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to upload document.'),
    });
  }

  removeDocument(documentId: string): void {
    this.providers.deleteMyDocument(documentId).subscribe({
      next: () => {
        this.notify('Document removed.');
        this.reload();
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to remove document.'),
    });
  }

  setAvailability(status: AvailabilityStatus): void {
    this.providers.setAvailability(status).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.notify('Availability updated.');
      },
      error: (err) => this.notify(err?.error?.message ?? 'Unable to update availability.'),
    });
  }

  documentFor(requirementId: string) {
    return this.profile()?.documents.find((d) => d.documentRequirementId === requirementId);
  }

  servicesIn(categoryId: string): CatalogService[] {
    return this.services().filter((s) => s.categoryId === categoryId);
  }

  canEditApplication(): boolean {
    const status = this.profile()?.approvalStatus;
    return status === 'pending' || status === 'rejected';
  }

  private loadRequirementsForSelection(): void {
    const categoryIds = [
      ...new Set(
        this.services()
          .filter((s) => this.selectedServiceIds().includes(s.id))
          .map((s) => s.categoryId),
      ),
    ];

    if (categoryIds.length === 0) {
      this.requirements.set([]);
      return;
    }

    forkJoin(categoryIds.map((id) => this.providers.getDocumentRequirements(id))).subscribe({
      next: (groups) => this.requirements.set(groups.flat()),
      error: () => this.requirements.set([]),
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 4000 });
  }
}
