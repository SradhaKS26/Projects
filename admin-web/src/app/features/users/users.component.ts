import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';

interface UserListItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [MatTableModule, MatCardModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly users = signal<UserListItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly displayedColumns = ['name', 'email', 'roles', 'isActive'];

  ngOnInit(): void {
    this.http
      .get<ApiResponse<UserListItem[]>>(`${environment.apiBaseUrl}/users`)
      .subscribe({
        next: (response) => this.users.set(response.data ?? []),
        error: (err) =>
          this.error.set(err?.error?.message ?? 'Unable to load users. Ensure you have ManageUsers permission.'),
      });
  }
}
