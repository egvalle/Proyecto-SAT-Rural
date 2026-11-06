import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MonitoringService } from '../../services/monitoring.service';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-event-simulator',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './event-simulator.html',
  styleUrl: './event-simulator.scss'
})
export class EventSimulator {
  scenario = 'HEAVY_RAIN';
  running = false;
  message = 'El simulador está en espera. Selecciona un escenario para demostrar el comportamiento del sistema.';

  constructor(
    private readonly monitoringService: MonitoringService,
    readonly authService: AuthService,
    private readonly changeDetector: ChangeDetectorRef
  ) {}

  get canWrite(): boolean {
    return this.authService.canWrite();
  }

  run(): void {
    if (this.running) return;
    if (!this.canWrite) {
      this.message = 'No posee permisos para modificar la información.';
      return;
    }

    this.running = true;
    this.monitoringService.startSimulation(this.scenario).subscribe({
      next: () => {
        this.message = `Escenario global activo: ${this.getScenarioName()}. Pulsa «Detener simulación» al terminar.`;
        this.running = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.message = 'No fue posible iniciar el escenario.';
        this.running = false;
        this.changeDetector.detectChanges();
      }
    });
  }

  reset(): void {
    if (this.running) return;
    if (!this.canWrite) {
      this.message = 'No posee permisos para modificar la información.';
      return;
    }

    this.running = true;
    this.monitoringService.resetSimulation().subscribe({
      next: () => {
        this.message = 'Simulación detenida. No se generarán nuevas lecturas simuladas.';
        this.running = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.message = 'No fue posible reiniciar el escenario.';
        this.running = false;
        this.changeDetector.detectChanges();
      }
    });
  }

  private getScenarioName(): string {
    const scenarios: Record<string, string> = {
      HEAVY_RAIN: 'Lluvia intensa',
      STORM: 'Tormenta',
      FLOOD: 'Riesgo de inundación',
      DROUGHT: 'Sequía',
      FROST: 'Helada',
      FOREST_FIRE: 'Incendio forestal'
    };
    return scenarios[this.scenario] ?? this.scenario;
  }
}
