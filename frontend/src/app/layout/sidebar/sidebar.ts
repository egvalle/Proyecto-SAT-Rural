import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

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

  constructor(readonly authService: AuthService) {}

  close(): void {
    this.closeSidebar.emit();
  }
}