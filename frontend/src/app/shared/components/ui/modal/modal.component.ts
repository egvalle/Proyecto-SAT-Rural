import { CommonModule } from '@angular/common';

import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output
} from '@angular/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './modal.component.html',
  styles: ``
})
export class ModalComponent {

  @Input() isOpen = false;

  @Output() close =
    new EventEmitter<void>();

  @Input() className = '';

  @Input() showCloseButton = true;

  @Input() isFullscreen = false;

  constructor(
    private readonly el: ElementRef
  ) {}

  ngOnInit(): void {
    this.updateBodyScroll();
  }

  ngOnChanges(): void {
    this.updateBodyScroll();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  private updateBodyScroll(): void {
    document.body.style.overflow =
      this.isOpen
        ? 'hidden'
        : '';
  }

  onBackdropClick(
    event: MouseEvent
  ): void {

    if (!this.isFullscreen) {
      this.close.emit();
    }
  }

  onContentClick(
    event: MouseEvent
  ): void {
    event.stopPropagation();
  }

  @HostListener(
    'document:keydown.escape'
  )
  onEscape(): void {

    if (this.isOpen) {
      this.close.emit();
    }
  }
}