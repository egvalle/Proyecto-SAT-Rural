import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class Sidebar {
  @Input() open = false;

  @Output() closeSidebar = new EventEmitter<void>();

  close(): void {
    this.closeSidebar.emit();
  }
}