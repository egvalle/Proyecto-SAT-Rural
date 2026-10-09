import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: '../login/login.scss',
})
export class Register {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  fullName = '';
  username = '';
  password = '';
  confirmPassword = '';

  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onRegister(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.fullName.trim() || !this.username.trim()) {
      this.errorMessage = 'Completa todos los campos obligatorios.';
      return;
    }

    if (this.password.length < 8) {
      this.errorMessage =
        'La contraseña debe contener al menos 8 caracteres.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Las contraseñas no coinciden.';
      return;
    }

    this.isLoading = true;

    this.authService.register({
      username: this.username.trim().toLowerCase(),
      password: this.password,
      fullName: this.fullName.trim(),
      roleId: 2
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage =
          'Usuario creado correctamente. Ya puedes iniciar sesión.';
        this.password = '';
        this.confirmPassword = '';
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;

        if (error.status === 409) {
          this.errorMessage =
            'Este correo o nombre de usuario ya está registrado.';
        } else {
          this.errorMessage =
            error.error?.message ??
            'No fue posible crear el usuario. Intenta nuevamente.';
        }
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}