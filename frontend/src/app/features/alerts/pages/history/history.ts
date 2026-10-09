import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  EventsService,
  EventFilters
} from '../../../../core/services/events.service';

import { Event } from '../../../../core/models/event.model';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule
  ],
  templateUrl: './history.html',
  styleUrl: './history.scss',
})
export class History implements OnInit {

  events: Event[] = [];

  loading = true;
  error = false;

  communityId: number | null = null;
  eventType = '';
  level = '';

  dateFrom = '';
  dateTo = '';

  readonly eventTypes = [
    'Inundación',
    'Sequía',
    'Tormenta',
    'Helada',
    'Incendio forestal'
  ];

  readonly levels = [
    'YELLOW',
    'ORANGE',
    'RED'
  ];

  constructor(
    private eventsService: EventsService
  ) {
  }

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {

    this.loading = true;
    this.error = false;

    const filters: EventFilters = {};

    if (this.communityId !== null) {
      filters.communityId = this.communityId;
    }

    if (this.eventType) {
      filters.eventType = this.eventType;
    }

    if (this.level) {
      filters.level = this.level;
    }

    if (this.dateFrom) {
      filters.from = this.dateFrom;
    }

    if (this.dateTo) {
      filters.to = this.dateTo;
    }

    this.eventsService
      .getEvents(filters)
      .subscribe({
        next: (events) => {
          this.events = events;
          this.loading = false;
        },
        error: () => {
          this.events = [];
          this.loading = false;
          this.error = true;
        }
      });
  }

  clearFilters(): void {

    this.communityId = null;
    this.eventType = '';
    this.level = '';
    this.dateFrom = '';
    this.dateTo = '';

    this.loadEvents();
  }

  getLevelLabel(level: string | null): string {

    if (!level) {
      return 'Sin nivel';
    }

    switch (level) {
      case 'YELLOW':
        return 'Amarillo';

      case 'ORANGE':
        return 'Naranja';

      case 'RED':
        return 'Rojo';

      default:
        return level;
    }
  }

  getLevelClass(level: string | null): string {

    switch (level) {
      case 'YELLOW':
        return 'bg-warning text-dark';

      case 'ORANGE':
        return 'bg-orange text-white';

      case 'RED':
        return 'bg-danger text-white';

      default:
        return 'bg-secondary';
    }
  }

  getStatusLabel(isActive: boolean | null): string {

    if (isActive === null) {
      return 'Sin estado';
    }

    return isActive
      ? 'Activa'
      : 'Resuelta';
  }

  getStatusClass(isActive: boolean | null): string {

    if (isActive === null) {
      return 'bg-secondary';
    }

    return isActive
      ? 'bg-danger-subtle text-danger'
      : 'bg-success-subtle text-success';
  }
}