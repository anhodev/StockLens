import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';
import { BodyType } from '../core/models';

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

/** Side-profile geometry per body style: roof/greenhouse, body shell, and wheel placement. */
interface Silhouette {
  /** Cabin/roof outline. */
  roof: string;
  /** Main body shell. */
  body: string;
  /** Window panes. */
  windows: string[];
  /** [cx, cy, r] for each wheel. */
  wheels: [number, number, number][];
}

const SILHOUETTES: Record<BodyType, Silhouette> = {
  // Low three-box profile with a notchback rear.
  Sedan: {
    roof: 'M74 44 L98 25 L150 25 L174 44 Z',
    body: 'M32 62 Q32 44 54 44 L188 44 Q206 44 210 60 L212 70 Q213 80 202 80 L44 80 Q32 80 32 68 Z',
    windows: ['M96 42 L104 29 L120 29 L120 42 Z', 'M126 42 L126 29 L146 29 L158 42 Z'],
    wheels: [[80, 82, 17], [168, 82, 17]],
  },
  // Taller, boxier greenhouse and a squared-off tail.
  Suv: {
    roof: 'M62 38 L84 20 L166 20 L184 38 Z',
    body: 'M30 58 Q30 38 52 38 L186 38 Q206 38 210 56 L212 70 Q213 80 202 80 L42 80 Q30 80 30 68 Z',
    windows: ['M84 36 L92 24 L118 24 L118 36 Z', 'M124 36 L124 24 L158 24 L170 36 Z'],
    wheels: [[76, 80, 19], [168, 80, 19]],
  },
  // Forward cab plus an open bed: the cab sits over the front axle, bed rails run to the tail.
  Truck: {
    roof: 'M58 36 L78 19 L128 19 L142 36 Z',
    body: 'M28 58 Q28 36 50 36 L142 36 L142 52 L206 52 Q212 52 212 60 L212 70 Q213 80 202 80 L40 80 Q28 80 28 68 Z',
    windows: ['M78 34 L86 23 L106 23 L106 34 Z', 'M112 34 L112 23 L126 23 L134 34 Z'],
    wheels: [[72, 80, 20], [176, 80, 20]],
  },
  // Short rear overhang, steeply raked tailgate.
  Hatchback: {
    roof: 'M72 42 L96 24 L146 24 L172 46 Z',
    body: 'M32 62 Q32 42 54 42 L172 42 Q186 42 186 56 L186 70 Q186 80 176 80 L44 80 Q32 80 32 68 Z',
    windows: ['M94 40 L102 28 L118 28 L118 40 Z', 'M124 40 L124 28 L144 28 L158 40 Z'],
    wheels: [[78, 82, 17], [156, 82, 17]],
  },
  // Long hood, low fastback roofline, two windows.
  Coupe: {
    roof: 'M78 45 L106 26 L146 26 L182 48 Z',
    body: 'M32 62 Q32 45 56 45 L188 45 Q208 45 211 61 L212 70 Q213 80 202 80 L44 80 Q32 80 32 68 Z',
    windows: ['M102 43 L112 30 L132 30 L132 43 Z', 'M138 43 L138 30 L150 30 L168 43 Z'],
    wheels: [[82, 82, 17], [172, 82, 17]],
  },
  // Tall slab side, long wheelbase, sliding-door greenhouse.
  Van: {
    roof: 'M56 34 L76 18 L176 18 L192 34 Z',
    body: 'M28 56 Q28 34 50 34 L190 34 Q208 34 210 54 L212 70 Q213 80 202 80 L40 80 Q28 80 28 68 Z',
    windows: ['M76 32 L84 22 L108 22 L108 32 Z', 'M114 32 L114 22 L142 22 L142 32 Z', 'M148 32 L148 22 L172 22 L180 32 Z'],
    wheels: [[74, 80, 18], [174, 80, 18]],
  },
  // Estate roofline carried flat all the way to the tailgate.
  Wagon: {
    roof: 'M70 40 L94 23 L176 23 L186 40 Z',
    body: 'M30 60 Q30 40 52 40 L186 40 Q202 40 204 56 L206 70 Q207 80 196 80 L42 80 Q30 80 30 68 Z',
    windows: ['M92 38 L100 27 L118 27 L118 38 Z', 'M124 38 L124 27 L148 27 L148 38 Z', 'M154 38 L154 27 L172 27 L178 38 Z'],
    wheels: [[78, 82, 18], [166, 82, 18]],
  },
};

/**
 * Vehicle image for cards/detail. Uses a real photo when `imageUrl` is provided,
 * otherwise renders a self-contained SVG silhouette matching the vehicle's body
 * style and tinted by its paint colour — no external network dependency.
 */
@Component({
  selector: 'app-car-image',
  imports: [NgOptimizedImage],
  template: `
    @if (imageUrl) {
      <img [ngSrc]="imageUrl" fill priority alt="{{ make }} {{ model }}" />
    } @else {
      <svg viewBox="0 0 240 120" preserveAspectRatio="xMidYMid meet" role="img"
           [attr.aria-label]="make + ' ' + model + ' (' + bodyStyle() + ')'">
        <!-- soft contact shadow, tuned for the light card plate -->
        <ellipse cx="120" cy="99" rx="88" ry="6" fill="rgba(16,17,26,0.14)" />

        <!-- cabin / roof -->
        <path [attr.d]="shape().roof" [attr.fill]="body()" stroke="rgba(16,17,26,0.10)" stroke-width="1" />

        <!-- body (outlined so pale colours stay legible on a white card) -->
        <path [attr.d]="shape().body" [attr.fill]="body()" stroke="rgba(16,17,26,0.10)" stroke-width="1" />

        <!-- windows -->
        @for (w of shape().windows; track $index) {
          <path [attr.d]="w" fill="#38414f" />
        }

        <!-- headlight + taillight -->
        <circle [attr.cx]="lightX()" cy="60" r="3.4" fill="#fbbf24" />
        <rect x="30" y="57" width="5" height="6" rx="1.5" fill="#ef4444" />

        <!-- wheels -->
        @for (w of shape().wheels; track $index) {
          <g>
            <circle [attr.cx]="w[0]" [attr.cy]="w[1]" [attr.r]="w[2]" fill="#23272f" />
            <circle [attr.cx]="w[0]" [attr.cy]="w[1]" [attr.r]="w[2] * 0.44" fill="#9aa1ad" />
          </g>
        }
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

  private readonly _bodyType = signal<BodyType>('Sedan');
  @Input() set bodyType(value: BodyType | null | undefined) {
    this._bodyType.set(value ?? 'Sedan');
  }

  /** Read-only view of the resolved body style, for the template. */
  readonly bodyStyle = this._bodyType.asReadonly();

  readonly body = computed(() => COLOR_MAP[this._color()] ?? '#64748b');
  readonly highlight = computed(() => '#ffffff');

  /** Falls back to the sedan profile for any body style without bespoke geometry. */
  readonly shape = computed(() => SILHOUETTES[this._bodyType()] ?? SILHOUETTES.Sedan);

  /** Trucks/wagons have a shorter nose, so the headlight sits slightly further back. */
  readonly lightX = computed(() => (this._bodyType() === 'Wagon' ? 199 : 205));
}
