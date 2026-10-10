import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
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
  editingUserId: number | null = null;
  editFullName = '';
  editPassword = '';
  editRoleId = '';
  isSaving = false;
  saveError = '';
  saveMessage = '';
  updatingUserId: number | null = null;

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

  editUser(user: AdminUser): void {
    this.editingUserId = user.id;
    this.editFullName = user.fullName;
    this.editPassword = '';
    this.editRoleId = String(user.roleId);
    this.saveError = '';
    this.saveMessage = '';
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelEdit(): void {
    this.editingUserId = null;
    this.editFullName = '';
    this.editPassword = '';
    this.editRoleId = '';
    this.saveError = '';
    this.saveMessage = '';
  }

  saveUser(): void {
    if (this.editingUserId === null) {
      return;
    }

    const fullName = this.editFullName.trim();
    if (!fullName || !this.editRoleId) {
      this.saveError = 'El nombre y el rol son obligatorios.';
      return;
    }

    if (this.editPassword && this.editPassword.length < 8) {
      this.saveError = 'La contraseña debe contener al menos 8 caracteres.';
      return;
    }

    this.isSaving = true;
    this.saveError = '';
    this.saveMessage = '';

    this.authService.updateAdminUser(this.editingUserId, {
      fullName,
      roleId: Number(this.editRoleId),
      ...(this.editPassword ? { password: this.editPassword } : {})
    }).subscribe({
      next: updatedUser => {
        this.users = this.users.map(user =>
          user.id === updatedUser.id ? updatedUser : user
        );
        this.applyTextFilter();
        this.isSaving = false;
        this.cancelEdit();
        this.saveMessage = 'Usuario actualizado correctamente.';
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.saveError =
          error.error?.message ?? 'No se pudo actualizar el usuario.';
      }
    });
  }

  toggleUserStatus(user: AdminUser): void {
    this.updatingUserId = user.id;
    this.errorMessage = '';

    this.authService.changeAdminUserStatus(user.id, !user.isActive).subscribe({
      next: updatedUser => {
        this.users = this.users.map(current =>
          current.id === updatedUser.id ? updatedUser : current
        );
        this.applyTextFilter();
        this.updatingUserId = null;
      },
      error: (error: HttpErrorResponse) => {
        this.updatingUserId = null;
        this.errorMessage =
          error.error?.message ?? 'No se pudo cambiar el estado del usuario.';
      }
    });
  }
}
