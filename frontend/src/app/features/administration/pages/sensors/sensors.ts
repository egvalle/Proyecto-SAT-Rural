import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentCardComponent } from '../../../..//../app/shared/components/component-card/component-card.component';
import { AuthService } from '../../../../core/services/auth.service';
import { SensorListItem, SensorService } from '../../../../core/services/sensor.service';


@Component({
  selector: 'app-sensors',
  imports: [
    ComponentCardComponent,
    FormsModule
  ],
  templateUrl: './sensors.html',
  styleUrl: './sensors.scss',
})
export class Sensors {
  readonly communityId = 1;
  sensorCode = '';
  sensorName = '';
  sensorType = 'TEMPERATURE';
  customSensorType = '';
  useCustomType = false;
  sensorStatus = 'Activo';
  isSaving = false;
  saveMessage = '';
  saveError = '';
  sensorSearch = 'TEMP';
  sensorActive = 'true';
  sensors: SensorListItem[] = [];
  isLoadingSensors = false;
  sensorListError = '';

  constructor(
    private readonly authService: AuthService,
    private readonly sensorService: SensorService
  ) {}

  filterSensors(): void {
    this.sensorListError = '';
    this.isLoadingSensors = true;

    this.sensorService.getSensors(
      this.sensorSearch.trim(),
      this.sensorActive === 'true',
      1,
      10
    ).subscribe({
      next: sensors => {
        this.sensors = sensors;
        this.isLoadingSensors = false;
      },
      error: () => {
        this.sensors = [];
        this.isLoadingSensors = false;
        this.sensorListError = 'No se pudo consultar la lista de sensores.';
      }
    });
  }

  getSensorTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      TEMPERATURE: 'Temperatura',
      HUMIDITY: 'Humedad',
      FLOW: 'Caudal'
    };

    return labels[type] ?? type;
  }

  saveSensor(): void {
    this.saveMessage = '';
    this.saveError = '';

    if (this.authService.getRole() !== 'ADMIN') {
      this.saveError = 'No posee los permisos suficientes.';
      return;
    }

    if (!this.sensorCode.trim() || !this.sensorName.trim() || !this.sensorType || !this.sensorStatus) {
      this.saveError = 'Completa los datos obligatorios del sensor.';
      return;
    }

    this.isSaving = true;
    this.sensorService.createSensor({
      communityId: this.communityId,
      code: this.sensorCode.trim(),
      name: this.sensorName.trim(),
      type: this.sensorType,
      unit: '°C',
      isActive: this.sensorStatus === 'Activo'
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.saveMessage = 'Sensor guardado correctamente.';
        this.sensorCode = '';
        this.sensorName = '';
      },
      error: () => {
        this.isSaving = false;
        this.saveError = 'No se pudo guardar el sensor.';
      }
    });
  }
}
