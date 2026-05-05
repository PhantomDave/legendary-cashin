import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-section-header',
  imports: [ButtonModule],
  templateUrl: './section-header.component.html',
  styleUrl: './section-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
  readonly ctaText = input<string>('');
  readonly ctaClicked = output<void>();
}
