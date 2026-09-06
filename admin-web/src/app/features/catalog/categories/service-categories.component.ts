import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CatalogApiService } from '../../../core/catalog/catalog-api.service';
import {
  ServiceCategory,
  ServiceCategoryPayload,
} from '../../../shared/models/catalog.models';
import { CategoryDialogComponent } from './category-dialog.component';

@Component({
  selector: 'app-service-categories',
  standalone: true,
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSlideToggleModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './service-categories.component.html',
  styleUrl: './service-categories.component.scss',
})
export class ServiceCategoriesComponent implements OnInit {
  private readonly api = inject(CatalogApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly categories = signal<ServiceCategory[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly includeInactive = signal(true);
  readonly displayedColumns = ['name', 'description', 'serviceCount', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getCategories(this.includeInactive()).subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Unable to load service categories.'));
        this.loading.set(false);
      },
    });
  }

  toggleInactive(showInactive: boolean): void {
    this.includeInactive.set(showInactive);
    this.load();
  }

  openCreate(): void {
    this.dialog
      .open(CategoryDialogComponent, { data: { category: null } })
      .afterClosed()
      .subscribe((payload?: ServiceCategoryPayload) => {
        if (!payload) {
          return;
        }

        this.api.createCategory(payload).subscribe({
          next: () => {
            this.notify('Category created.');
            this.load();
          },
          error: (err) => this.notify(this.messageFrom(err, 'Unable to create category.')),
        });
      });
  }

  openEdit(category: ServiceCategory): void {
    this.dialog
      .open(CategoryDialogComponent, { data: { category } })
      .afterClosed()
      .subscribe((payload?: ServiceCategoryPayload) => {
        if (!payload) {
          return;
        }

        this.api.updateCategory(category.id, payload).subscribe({
          next: () => {
            this.notify('Category updated.');
            this.load();
          },
          error: (err) => this.notify(this.messageFrom(err, 'Unable to update category.')),
        });
      });
  }

  disable(category: ServiceCategory): void {
    const confirmed = confirm(
      `Deactivate "${category.name}"? Its ${category.serviceCount} service(s) will also be deactivated.`,
    );

    if (!confirmed) {
      return;
    }

    this.api.disableCategory(category.id).subscribe({
      next: () => {
        this.notify('Category deactivated.');
        this.load();
      },
      error: (err) => this.notify(this.messageFrom(err, 'Unable to deactivate category.')),
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
