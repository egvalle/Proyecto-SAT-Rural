import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild
} from '@angular/core';

import {
  Chart,
  ChartConfiguration,
  registerables
} from 'chart.js';

import { SensorHistory } from '../../models/monitoring-history.model';

Chart.register(...registerables);

@Component({
  selector: 'app-sensor-chart',
  standalone: true,
  imports: [],
  templateUrl: './sensor-chart.html',
  styleUrl: './sensor-chart.scss'
})
export class SensorChart
  implements AfterViewInit, OnChanges, OnDestroy {

  @Input() history: SensorHistory[] = [];

  @ViewChild('sensorChart')
  sensorChart?: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;

  private viewInitialized = false;

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.renderChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['history'] &&
      this.viewInitialized
    ) {
      this.renderChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private renderChart(): void {

    if (!this.sensorChart) {
      return;
    }

    const temperature =
      this.history.find(
        x => x.type === 'TEMPERATURE'
      );

    const rainfall =
      this.history.find(
        x => x.type === 'RAINFALL'
      );

    const river =
      this.history.find(
        x => x.type === 'RIVER_LEVEL'
      );

    const source =
      temperature?.readings ??
      rainfall?.readings ??
      river?.readings ??
      [];

    const labels = source.map(
      x =>
        new Date(x.recordedAt)
          .toLocaleTimeString(
            [],
            {
              hour: '2-digit',
              minute: '2-digit',
              second: '2-digit'
            }
          )
    );

    const data = {
      labels,

      datasets: [
        {
          label: 'Temperatura °C',
          data:
            temperature?.readings
              .map(x => x.value) ?? [],
          tension: 0.35
        },
        {
          label: 'Lluvia mm/h',
          data:
            rainfall?.readings
              .map(x => x.value) ?? [],
          tension: 0.35
        },
        {
          label: 'Nivel río %',
          data:
            river?.readings
              .map(x => x.value) ?? [],
          tension: 0.35
        }
      ]
    };

    if (this.chart) {

      this.chart.data = data;
      this.chart.update();

      return;
    }

    this.chart = new Chart(
      this.sensorChart.nativeElement,
      {
        type: 'line',

        data,

        options: {
          responsive: true,
          maintainAspectRatio: false,

          interaction: {
            mode: 'index',
            intersect: false
          },

          plugins: {
            legend: {
              position: 'bottom'
            }
          },

          scales: {
            y: {
              beginAtZero: true
            }
          }
        }
      }
    );
  }
}