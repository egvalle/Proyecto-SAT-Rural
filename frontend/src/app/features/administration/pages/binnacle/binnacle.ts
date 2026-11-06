import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { AuthService } from '../../../../core/services/auth.service';
import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';

interface ActivityEntry {
  key: string;
  source: 'Bitácora' | 'Auditoría';
  user: string;
  action: string;
  entity: string;
  entityId: string;
  description: string;
  date: string;
}

@Component({
  selector: 'app-binnacle',
  imports: [DatePipe, FormsModule, ComponentCardComponent],
  templateUrl: './binnacle.html',
  styleUrl: './binnacle.scss',
})
export class Binnacle implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  entries: ActivityEntry[] = [];
  isLoading = true;
  errorMessage = '';
  userFilter = '';
  actionFilter = '';
  entityFilter = '';
  dateFrom = '';
  dateTo = '';

  get filteredEntries(): ActivityEntry[] {
    const user = this.userFilter.trim().toLowerCase();
    const action = this.actionFilter.trim().toLowerCase();
    const entity = this.entityFilter.trim().toLowerCase();
    return this.entries.filter(entry => {
      const date = this.localDate(entry.date);
      return (!user || entry.user.toLowerCase().includes(user))
        && (!action || entry.action.toLowerCase().includes(action))
        && (!entity || entry.entity.toLowerCase().includes(entity))
        && (!this.dateFrom || date >= this.dateFrom)
        && (!this.dateTo || date <= this.dateTo);
    });
  }

  ngOnInit(): void {
    this.loadBinnacle();
  }

  loadBinnacle(): void {
    this.isLoading = true;
    this.errorMessage = '';
    forkJoin({
      binnacle: this.authService.getBinnacle(),
      audit: this.authService.getAuditLogs()
    }).subscribe({
      next: ({ binnacle, audit }) => {
        this.entries = [
          ...binnacle.map(item => ({
            key: `binnacle-${item.id}`,
            source: 'Bitácora' as const,
            user: item.user,
            action: `Movimiento ${item.idMovimentType}`,
            entity: '',
            entityId: '',
            description: item.description,
            date: item.dateHour
          })),
          ...audit.map(item => ({
            key: `audit-${item.id}`,
            source: 'Auditoría' as const,
            user: item.username,
            action: item.action,
            entity: item.entityName,
            entityId: item.entityId,
            description: item.details,
            date: item.createdAt
          }))
        ].sort((a, b) => Date.parse(b.date) - Date.parse(a.date));
        this.isLoading = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'No fue posible cargar ambos historiales. Intenta de nuevo.';
        this.changeDetector.detectChanges();
      }
    });
  }

  clearFilters(): void {
    this.userFilter = '';
    this.actionFilter = '';
    this.entityFilter = '';
    this.dateFrom = '';
    this.dateTo = '';
  }

  private localDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
