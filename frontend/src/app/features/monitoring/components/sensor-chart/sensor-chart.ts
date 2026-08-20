import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild
} from '@angular/core';

import {
  Chart,
  ChartConfiguration,
  registerables
} from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-sensor-chart',
  standalone: true,
  imports: [],
  templateUrl: './sensor-chart.html',
  styleUrl: './sensor-chart.scss'
})
export class SensorChart implements AfterViewInit, OnDestroy {

  @ViewChild('sensorChart')
  sensorChart?: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;

  ngAfterViewInit(): void {
    if (!this.sensorChart) {
      console.error('No se encontró el canvas #sensorChart');
      return;
    }

    this.createChart();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private createChart(): void {

    if (!this.sensorChart) {
      return;
    }

    const config: ChartConfiguration = {
      type: 'line',

      data: {
        labels: [
          '16:00',
          '17:00',
          '18:00',
          '19:00',
          '20:00',
          '21:00',
          '22:00'
        ],

        datasets: [
          {
            label: 'Temperatura (°C)',
            data: [25, 26, 26.5, 27, 27.8, 27.5, 27.4],
            tension: 0.4
          },
          {
            label: 'Lluvia (mm/h)',
            data: [0, 0.5, 1.2, 2.8, 4.1, 3.7, 3.2],
            tension: 0.4
          },
          {
            label: 'Nivel del río (%)',
            data: [34, 35, 36, 37, 39, 41, 42],
            tension: 0.4
          }
        ]
      },

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
    };

    this.chart = new Chart(
      this.sensorChart.nativeElement,
      config
    );
  }
}