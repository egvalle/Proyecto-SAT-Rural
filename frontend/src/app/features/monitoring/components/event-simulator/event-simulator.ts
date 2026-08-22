import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MonitoringService }
  from '../../services/monitoring.service';

@Component({
  selector: 'app-event-simulator',
  standalone: true,
  imports: [
    FormsModule
  ],
  templateUrl: './event-simulator.html',
  styleUrl: './event-simulator.scss'
})
export class EventSimulator {

  scenario = 'NORMAL';

  running = false;

  message =
    'El simulador está en espera. Seleccione un escenario para demostrar el comportamiento del sistema.';

  constructor(
    private monitoringService: MonitoringService
  ) {
  }


  run(): void {

    this.running = true;

    this.monitoringService
      .startSimulation(this.scenario)
      .subscribe({

        next: () => {

          this.message =
            `Escenario activo: ${this.getScenarioName()}.`;

          this.running = false;

        },

        error: () => {

          this.message =
            'No fue posible iniciar el escenario.';

          this.running = false;

        }

      });

  }


  reset(): void {

    this.monitoringService
      .resetSimulation()
      .subscribe({

        next: () => {

          this.scenario = 'NORMAL';

          this.message =
            'Sistema reiniciado a condiciones normales.';

        }

      });

  }


  private getScenarioName(): string {

    const scenarios:
      Record<string, string> = {

        NORMAL:
          'Condiciones normales',

        HEAVY_RAIN:
          'Lluvia intensa',

        STORM:
          'Tormenta',

        FLOOD:
          'Riesgo de inundación',

        DROUGHT:
          'Sequía',

        FROST:
          'Helada',

        FOREST_FIRE:
          'Incendio forestal'
      };

    return scenarios[this.scenario]
      ?? this.scenario;
  }
}