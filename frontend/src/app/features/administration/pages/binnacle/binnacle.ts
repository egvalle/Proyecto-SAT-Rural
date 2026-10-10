import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';

import { AuthService, BinnacleEntry } from '../../../../core/services/auth.service';
import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';

@Component({
  selector: 'app-binnacle',
  imports: [DatePipe, ComponentCardComponent],
  templateUrl: './binnacle.html',
  styleUrl: './binnacle.scss',
})
export class Binnacle implements OnInit {
  private readonly authService = inject(AuthService);

  entries: BinnacleEntry[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadBinnacle();
  }

  private loadBinnacle(): void {
    this.authService.getBinnacle().subscribe({
      next: entries => {
        this.entries = entries;
        this.isLoading = false;
      },
      error: error => {
        this.isLoading = false;
        this.errorMessage = error.error?.message ?? 'No fue posible cargar la bitácora.';
      }
    });
  }
}
