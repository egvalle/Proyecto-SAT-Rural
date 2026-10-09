import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';
import { AdminUser } from '../../../../core/models/user';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-user',
  imports: [FormsModule, ComponentCardComponent],
  templateUrl: './user.html',
  styleUrl: './user.scss',
})
export class User {
  private readonly authService = inject(AuthService);

  searchText = '';
  selectedRoleId = '';
  users: AdminUser[] = [];
  filteredUsers: AdminUser[] = [];
  isLoading = false;
  hasSearched = false;
  errorMessage = '';

  searchUsers(): void {
    this.isLoading = true;
    this.hasSearched = true;
    this.errorMessage = '';

    const roleId =
      this.selectedRoleId === '' ? undefined : Number(this.selectedRoleId);

    this.authService.getAdminUsers(roleId).subscribe({
      next: users => {
        this.users = users;
        this.applyTextFilter();
        this.isLoading = false;
      },
      error: () => {
        this.users = [];
        this.filteredUsers = [];
        this.errorMessage = 'No se pudo consultar la lista de usuarios.';
        this.isLoading = false;
      }
    });
  }

  applyTextFilter(): void {
    const query = this.searchText.trim().toLocaleLowerCase();
    this.filteredUsers = this.users.filter(user =>
      user.fullName.toLocaleLowerCase().includes(query) ||
      user.username.toLocaleLowerCase().includes(query)
    );
  }
}
