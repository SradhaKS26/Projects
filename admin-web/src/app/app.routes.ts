import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { AdminShellComponent } from './features/layout/admin-shell.component';
import { PlaceholderPage } from './features/placeholder/placeholder.page';
import { UsersComponent } from './features/users/users.component';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    component: LoginComponent,
  },
  {
    path: '',
    canActivate: [authGuard],
    component: AdminShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'users', component: UsersComponent },
      {
        path: 'my-application',
        loadComponent: () =>
          import('./features/providers/my-application.component').then(
            (m) => m.MyApplicationComponent,
          ),
      },
      {
        path: 'providers',
        canActivate: [permissionGuard('ManageProviders')],
        loadComponent: () =>
          import('./features/providers/providers.component').then((m) => m.ProvidersComponent),
      },
      {
        path: 'providers/:id',
        canActivate: [permissionGuard('ManageProviders')],
        loadComponent: () =>
          import('./features/providers/provider-review.component').then(
            (m) => m.ProviderReviewComponent,
          ),
      },
      {
        path: 'services',
        loadComponent: () =>
          import('./features/catalog/services/services.component').then(
            (m) => m.ServicesComponent,
          ),
      },
      {
        path: 'service-categories',
        loadComponent: () =>
          import('./features/catalog/categories/service-categories.component').then(
            (m) => m.ServiceCategoriesComponent,
          ),
      },
      {
        path: 'service-requests',
        component: PlaceholderPage,
        data: { title: 'Service Requests' },
      },
      {
        path: 'roles',
        component: PlaceholderPage,
        data: { title: 'Roles & Permissions' },
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
