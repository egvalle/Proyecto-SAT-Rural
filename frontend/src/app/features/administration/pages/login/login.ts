import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';

import { AuthService, LoginResponse } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  username = '';
  password = '';
  showPassword = false;
  rememberSession = true;
  isLoading = false;
  errorMessage = '';

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    this.completeSignIn(
      this.authService.login(
        { username: this.username, password: this.password },
        this.rememberSession
      ),
      this.username
    );
  }

  onGoogleSignIn(): void {
    this.completeSignIn(this.authService.loginWithGoogle(this.rememberSession));
  }

  onGitHubSignIn(): void {
    this.completeSignIn(this.authService.loginWithGitHub(this.rememberSession));
  }

  private completeSignIn(
    loginRequest: Observable<LoginResponse>,
    fallbackUsername = this.username
  ): void {
    this.isLoading = true;
    this.errorMessage = '';

    loginRequest.subscribe({
      next: response => {
        this.isLoading = false;
        void this.router.navigate(['/dashboard'], { replaceUrl: true });

        // The audit entry is secondary: authentication must still open the dashboard.
        this.authService.createBinnacle({
          description: 'Ingreso a panel',
          idMovimentType: 1,
          user: response.user?.username ?? fallbackUsername,
          dateHour: new Date().toISOString()
        }).subscribe({ error: () => undefined });
      },
      error: error => {
        this.isLoading = false;
        this.errorMessage = error.error?.message ?? (error.status === 401
          ? 'El username o password no son correctos.'
          : error.code === 'auth/popup-closed-by-user'
            ? `Se canceló el inicio de sesión con ${error.provider === 'github.com' ? 'GitHub' : 'Google'}.`
            : 'No fue posible iniciar sesión. Intenta nuevamente.');
      }
    });
  }
}
