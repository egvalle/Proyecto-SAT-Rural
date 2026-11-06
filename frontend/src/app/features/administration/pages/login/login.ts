import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';

import { AuthService } from '../../../../core/services/auth.service';

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
  oauthProviders = { google: false, github: false };

  constructor() {
    this.authService.getOAuthProviders().subscribe({
      next: providers => this.oauthProviders = providers,
      error: () => {}
    });
  }

  signInWith(provider: 'google' | 'github'): void {
    window.location.assign(this.authService.oauthUrl(provider));
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.authService.login({ username: this.username, password: this.password }, this.rememberSession)
      .pipe(
        switchMap(() => {
          return this.authService.createBinnacle({
            description: 'Ingreso a panel',
            idMovimentType: 1,
            user: this.username,
            dateHour: new Date().toISOString()
          }).pipe(catchError(() => of(null)));
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: error => {
          this.isLoading = false;
          this.errorMessage = error.error?.message ?? (error.status === 401
              ? 'El correo o la contraseña no son correctos.'
              : 'No fue posible iniciar sesión. Intenta nuevamente.');
        }
      });
  }

}
