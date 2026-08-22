import { Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [],
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header {
  @Output() toggleSidebar = new EventEmitter<void>();

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  toggleMenu(): void {
    this.toggleSidebar.emit();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}