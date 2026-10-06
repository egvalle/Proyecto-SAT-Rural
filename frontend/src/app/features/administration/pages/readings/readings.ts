import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';

import { Sensor } from '../../../../core/models/sensor';
import { Community } from '../../../../core/models/community';
import { SensorReading } from '../../../../core/models/sensor-reading';

import { SensorService } from '../../../../core/services/sensor.service';
import { CommunityService } from '../../../../core/services/community.service';
import { SensorReadingService } from '../../../../core/services/sensor-reading.service';

@Component({
  selector: 'app-readings',
  imports: [
    CommonModule,
    FormsModule,
    ComponentCardComponent
  ],
  templateUrl: './readings.html',
  styleUrl: './readings.scss'
})
export class Readings implements OnInit {

  readings: SensorReading[] = [];
  sensors: Sensor[] = [];
  communities: Community[] = [];

  isLoading = false;
  errorMessage = '';

  filterSensorId: number | null = null;
  filterCommunityId: number | null = null;
  filterFrom = '';
  filterTo = '';

  currentPage = 1;
  pageSize = 20;
  totalItems = 0;
  totalPages = 0;

  constructor(
    private readonly sensorReadingService: SensorReadingService,
    private readonly sensorService: SensorService,
    private readonly communityService: CommunityService
  ) {}

  ngOnInit(): void {
    this.loadCommunities();
    this.loadSensors();
    this.loadReadings();
  }

  loadCommunities(): void {
    this.communityService.getCommunities(
      '',
      '',
      '',
      undefined,
      1,
      100
    ).subscribe({
      next: response => {
        this.communities = response.items;
      },
      error: () => {
        this.communities = [];
      }
    });
  }

  loadSensors(): void {
    this.sensorService.getSensors(
      '',
      '',
      '',
      undefined,
      undefined,
      1,
      100
    ).subscribe({
      next: response => {
        this.sensors = response.items;
      },
      error: () => {
        this.sensors = [];
      }
    });
  }

  loadReadings(page = 1): void {
    this.isLoading = true;
    this.errorMessage = '';

    const sensorId =
      this.filterSensorId !== null
        ? Number(this.filterSensorId)
        : undefined;

    const communityId =
      this.filterCommunityId !== null
        ? Number(this.filterCommunityId)
        : undefined;

    const from = this.buildFromDate();
    const to = this.buildToDate();

    if (from && to && new Date(from) > new Date(to)) {
      this.errorMessage =
        'La fecha inicial no puede ser mayor que la fecha final.';
      this.isLoading = false;
      return;
    }

    this.sensorReadingService.getReadings(
      sensorId,
      communityId,
      from,
      to,
      page,
      this.pageSize
    ).subscribe({
      next: response => {
        this.readings = response.items;
        this.currentPage = response.page;
        this.pageSize = response.pageSize;
        this.totalItems = response.totalItems;
        this.totalPages = response.totalPages;
        this.isLoading = false;
      },
      error: error => {
        this.readings = [];
        this.totalItems = 0;
        this.totalPages = 0;
        this.isLoading = false;

        this.errorMessage =
          error?.error?.message ??
          'No se pudieron consultar las lecturas.';
      }
    });
  }

  filterReadings(): void {
    this.loadReadings(1);
  }

  clearFilters(): void {
    this.filterSensorId = null;
    this.filterCommunityId = null;
    this.filterFrom = '';
    this.filterTo = '';

    this.loadReadings(1);
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.loadReadings(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.loadReadings(this.currentPage + 1);
    }
  }

  getSensorTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      TEMPERATURE: 'Temperatura',
      HUMIDITY: 'Humedad',
      WIND_SPEED: 'Velocidad del viento',
      RAINFALL: 'Lluvia',
      RIVER_LEVEL: 'Nivel de río',
      RESERVOIR_LEVEL: 'Nivel de embalse',
      SMOKE_FIRE: 'Humo / incendio',
      OTHER_ENVIRONMENTAL: 'Otro ambiental'
    };

    return labels[type] ?? type;
  }

  private buildFromDate(): string | undefined {
    if (!this.filterFrom) {
      return undefined;
    }

    return `${this.filterFrom}T00:00:00`;
  }

  private buildToDate(): string | undefined {
    if (!this.filterTo) {
      return undefined;
    }

    return `${this.filterTo}T23:59:59`;
  }
}