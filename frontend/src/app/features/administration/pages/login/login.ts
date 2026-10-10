import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { switchMap } from 'rxjs';

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

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    this.isLoading = true;
    this.errorMessage = '';
    let loginSucceeded = false;

    this.authService.login({ username: this.username, password: this.password }, this.rememberSession)
      .pipe(
        switchMap(() => {
          loginSucceeded = true;
          return this.authService.createBinnacle({
            description: 'Ingreso a panel',
            idMovimentType: 1,
            user: this.username,
            dateHour: new Date().toISOString()
          });
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: error => {
          this.isLoading = false;
          this.errorMessage = loginSucceeded
            ? error.error?.message ?? 'No fue posible guardar el ingreso en la bitácora.'
            : error.error?.message ?? (error.status === 401
              ? 'El username o password no son correctos.'
              : 'No fue posible iniciar sesión. Intenta nuevamente.');
        }
      });
  }

}
