import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PlaceholderComponent } from './placeholder.component';

@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `
    <app-placeholder
      [title]="title() || 'Coming soon'"
      description="Placeholder for a later MVP phase. Navigation is ready."
    />
  `,
})
export class PlaceholderPage {
  private readonly route = inject(ActivatedRoute);
  readonly title = toSignal(this.route.data.pipe(map((d) => d['title'] as string | undefined)));
}
