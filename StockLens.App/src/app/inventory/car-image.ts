import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';

/** Maps common paint-colour names to a display hex used to tint the SVG body. */
const COLOR_MAP: Record<string, string> = {
  white: '#e8eaed',
  silver: '#c3c8d0',
  grey: '#7c8698',
  gray: '#7c8698',
  black: '#2b3442',
  blue: '#3b82f6',
  red: '#ef4444',
  green: '#22c55e',
  yellow: '#eab308',
  orange: '#f97316',
};

/**
 * Vehicle image for cards/detail. Uses a real photo when `imageUrl` is provided,
 * otherwise renders a self-contained SVG car silhouette tinted by the paint colour —
 * no external network dependency.
 */
@Component({
  selector: 'app-car-image',
  imports: [NgOptimizedImage],
  template: `
    @if (imageUrl) {
      <img [ngSrc]="imageUrl" fill priority alt="{{ make }} {{ model }}" />
    } @else {
      <svg viewBox="0 0 240 120" preserveAspectRatio="xMidYMid meet" role="img"
           [attr.aria-label]="make + ' ' + model">
        <!-- ground shadow -->
        <ellipse cx="120" cy="99" rx="92" ry="7" fill="rgba(0,0,0,0.28)" />

        <!-- cabin / roof -->
        <path d="M74 44 L98 24 L150 24 L174 44 Z" [attr.fill]="body()" />
        <!-- windows -->
        <path d="M96 42 L104 29 L120 29 L120 42 Z" fill="#0b1220" opacity="0.85" />
        <path d="M126 42 L126 29 L146 29 L158 42 Z" fill="#0b1220" opacity="0.85" />

        <!-- body -->
        <path d="M32 62 Q32 44 54 44 L188 44 Q206 44 210 60 L212 70
                 Q213 80 202 80 L44 80 Q32 80 32 68 Z" [attr.fill]="body()" />
        <!-- highlight strip -->
        <path d="M40 63 L204 63" [attr.stroke]="highlight()" stroke-width="2" opacity="0.35" />

        <!-- headlight + taillight -->
        <circle cx="205" cy="60" r="3.4" fill="#fde68a" />
        <rect x="33" y="57" width="5" height="6" rx="1.5" fill="#f87171" />

        <!-- wheels -->
        <circle cx="80" cy="82" r="17" fill="#12161f" />
        <circle cx="80" cy="82" r="7.5" fill="#4b5563" />
        <circle cx="168" cy="82" r="17" fill="#12161f" />
        <circle cx="168" cy="82" r="7.5" fill="#4b5563" />
      </svg>
    }
  `,
  styles: [`
    :host { display: block; width: 100%; height: 100%; position: relative; }
    svg { width: 100%; height: 100%; display: block; }
    img { object-fit: cover; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarImageComponent {
  @Input() make = '';
  @Input() model = '';
  @Input() imageUrl?: string | null;

  private readonly _color = signal('');
  @Input() set color(value: string | null | undefined) {
    this._color.set((value ?? '').trim().toLowerCase());
  }

  readonly body = computed(() => COLOR_MAP[this._color()] ?? '#64748b');
  readonly highlight = computed(() => '#ffffff');
}
