import { booleanAttribute, Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard-navbar',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './dashboard-navbar.component.html',
})
export class DashboardNavbarComponent {
  @Input({ transform: booleanAttribute }) hideComponents = false;
  menuOpen = false;

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }
}
