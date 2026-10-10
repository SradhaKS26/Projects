import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../core/authentication/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatCardModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
}
