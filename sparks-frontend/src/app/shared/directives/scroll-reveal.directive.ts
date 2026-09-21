import { Directive, ElementRef, HostBinding, Input, OnDestroy, OnInit } from '@angular/core';

/**
 * Applies a fade/slide-in transition the first time the host element scrolls
 * into view. Pairs with the `.reveal` / `.reveal--visible` CSS in styles.scss.
 *
 * Usage: <div spReveal>...</div> or <div [spReveal]="120"> for a staggered delay (ms).
 */
@Directive({
  selector: '[spReveal]',
  standalone: true,
})
export class ScrollRevealDirective implements OnInit, OnDestroy {
  @Input('spReveal') delay: number | '' = 0;

  @HostBinding('class.reveal') readonly baseClass = true;
  @HostBinding('class.reveal--visible') visible = false;

  private observer?: IntersectionObserver;

  constructor(private el: ElementRef<HTMLElement>) {}

  ngOnInit(): void {
    if (typeof IntersectionObserver === 'undefined') {
      this.visible = true;
      return;
    }

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            const delayMs = this.delay === '' ? 0 : this.delay;
            window.setTimeout(() => (this.visible = true), delayMs);
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15, rootMargin: '0px 0px -60px 0px' }
    );
    this.observer.observe(this.el.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
