import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
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
        path: 'providers',
        component: PlaceholderPage,
        data: { title: 'Providers' },
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
