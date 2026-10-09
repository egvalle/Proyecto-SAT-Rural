import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { ComponentCardComponent } from '../../../..//../app/shared/components/component-card/component-card.component';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-user',
  imports: [FormsModule, ComponentCardComponent],
  templateUrl: './user.html',
  styleUrl: './user.scss',
})
export class User {
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

  // onSignIn(): void {
  //   this.isLoading = true;
  //   this.errorMessage = '';

  //   this.authService.user({ username: this.username, password: this.password }, this.rememberSession)
  //     .subscribe({
  //       next: () => {
  //         this.isLoading = false;
  //         this.router.navigate(['/dashboard']);
  //       },
  //       error: error => {
  //         this.isLoading = false;
  //         this.errorMessage = error.error?.message ?? (error.status === 401
  //           ? 'El username o password no son correctos.'
  //           : 'No fue posible iniciar sesión. Intenta nuevamente.');
  //       }
  //     });
  // }

}
