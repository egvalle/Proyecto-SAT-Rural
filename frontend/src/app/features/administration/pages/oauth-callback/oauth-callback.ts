import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-oauth-callback',
  template: `
    <main class="oauth-callback">
      <h1>Iniciando sesión</h1>
      <p>{{ message }}</p>
      @if (failed) {
        <a href="/login">Volver al inicio de sesión</a>
      }
    </main>
  `,
  styles: [`
    .oauth-callback { min-height: 100vh; display: grid; align-content: center;
      justify-items: center; gap: 1rem; padding: 2rem; text-align: center;
      color: #173b35; background: #f6f4ed; }
    h1, p { margin: 0; }
    a { color: #1d5848; }
  `]
})
export class OAuthCallback {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  message = 'Validando tu acceso...';
  failed = false;

  constructor() {
    const code = new URL(window.location.href).searchParams.get('code');
    window.history.replaceState({}, '', '/oauth/callback');
    if (!code) {
      this.message = 'No se recibió un código de acceso.';
      this.failed = true;
      return;
    }
    this.auth.exchangeOAuthCode(code).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => {
        this.message = 'No fue posible iniciar sesión. Intenta nuevamente.';
        this.failed = true;
      }
    });
  }
}
