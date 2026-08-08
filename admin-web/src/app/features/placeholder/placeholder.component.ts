import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <mat-card>
      <mat-card-title>{{ title() }}</mat-card-title>
      <mat-card-content>
        <p>{{ description() }}</p>
      </mat-card-content>
    </mat-card>
  `,
})
export class PlaceholderComponent {
  readonly title = input.required<string>();
  readonly description = input('This section will be implemented in a later MVP phase.');
}
