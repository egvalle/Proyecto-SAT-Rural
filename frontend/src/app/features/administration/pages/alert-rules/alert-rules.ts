import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AlertRuleService } from '../../../../core/services/alert-rule.service';
import { AuthService } from '../../../../core/services/auth.service';

import {
  AlertRule,
  CreateAlertRuleRequest
} from '../../../../core/models/alert-rule';

import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';
import { ModalComponent } from '../../../../shared/components/ui/modal/modal.component';

@Component({
  selector: 'app-alert-rules',
  standalone: true,
  imports: [
    FormsModule,
    ComponentCardComponent,
    ModalComponent
  ],
  templateUrl: './alert-rules.html',
  styleUrl: './alert-rules.scss'
})
export class AlertRules implements OnInit {

  // =========================================================
  // DATOS
  // =========================================================

  rules: AlertRule[] = [];

  isLoading = false;
  isSaving = false;
  isDeleting = false;

  listError = '';
  saveError = '';
  saveMessage = '';

  // =========================================================
  // ROL
  // =========================================================

  isAdmin = false;

  // =========================================================
  // MODAL
  // =========================================================

  isModalOpen = false;

  editingRuleId: number | null = null;

  // =========================================================
  // FORMULARIO
  // =========================================================

  sensorType = '';
  operator = '>=';
  thresholdValue: number | null = null;
  riskLevel = '';
  phenomenon = '';
  description = '';

  // =========================================================
  // OPCIONES
  // =========================================================

  readonly sensorTypes = [
    {
      value: 'TEMPERATURE',
      label: 'Temperatura'
    },
    {
      value: 'HUMIDITY',
      label: 'Humedad'
    },
    {
      value: 'WIND_SPEED',
      label: 'Velocidad del viento'
    },
    {
      value: 'RAINFALL',
      label: 'Precipitación'
    },
    {
      value: 'RIVER_LEVEL',
      label: 'Nivel del río'
    }
  ];

  readonly operators = [
    {
      value: '>=',
      label: 'Mayor o igual que'
    },
    {
      value: '<=',
      label: 'Menor o igual que'
    }
  ];

  readonly riskLevels = [
    {
      value: 'YELLOW',
      label: 'Amarillo'
    },
    {
      value: 'ORANGE',
      label: 'Naranja'
    },
    {
      value: 'RED',
      label: 'Rojo'
    }
  ];

  constructor(
    private readonly alertRuleService: AlertRuleService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAdmin =
      this.authService.getRole()?.toUpperCase() === 'ADMIN';

    this.loadRules();
  }

  // =========================================================
  // LISTADO
  // =========================================================

  loadRules(): void {
    this.isLoading = true;
    this.listError = '';

    this.alertRuleService.getAlertRules().subscribe({
      next: (rules) => {
        this.rules = rules;
        this.isLoading = false;
      },

      error: (error) => {
        console.error(
          'Error al consultar las reglas de alertas:',
          error
        );

        this.listError =
          'No fue posible consultar las reglas de alertas.';

        this.isLoading = false;
      }
    });
  }

  // =========================================================
  // MODAL
  // =========================================================

  openCreateModal(): void {
    this.resetForm();

    this.editingRuleId = null;
    this.saveError = '';
    this.saveMessage = '';

    this.isModalOpen = true;
  }

  openEditModal(rule: AlertRule): void {
    this.editingRuleId = rule.id;

    this.sensorType = rule.sensorType;
    this.operator = rule.operator;
    this.thresholdValue = rule.thresholdValue;
    this.riskLevel = rule.riskLevel;
    this.phenomenon = rule.phenomenon;
    this.description = rule.description;

    this.saveError = '';
    this.saveMessage = '';

    this.isModalOpen = true;
  }

  closeModal(): void {
    if (this.isSaving) {
      return;
    }

    this.isModalOpen = false;
    this.resetForm();
  }

  // =========================================================
  // GUARDAR
  // =========================================================

  saveRule(): void {
    this.saveError = '';
    this.saveMessage = '';

    const validationError = this.validateForm();

    if (validationError) {
      this.saveError = validationError;
      return;
    }

    const request: CreateAlertRuleRequest = {
      sensorType: this.sensorType,
      operator: this.operator,
      thresholdValue: Number(this.thresholdValue),
      riskLevel: this.riskLevel,
      phenomenon: this.phenomenon.trim(),
      description: this.description.trim()
    };

    this.isSaving = true;

    if (this.editingRuleId === null) {
      this.createRule(request);
    } else {
      this.updateRule(
        this.editingRuleId,
        request
      );
    }
  }

  private createRule(
    request: CreateAlertRuleRequest
  ): void {

    this.alertRuleService.createAlertRule(request).subscribe({
      next: () => {
        this.isSaving = false;

        this.saveMessage =
          'Regla de alerta creada correctamente.';

        this.loadRules();

        setTimeout(() => {
          this.closeModal();
        }, 500);
      },

      error: (error) => {
        console.error(
          'Error al crear regla:',
          error
        );

        this.saveError =
          this.getApiErrorMessage(
            error,
            'No fue posible crear la regla de alerta.'
          );

        this.isSaving = false;
      }
    });
  }

  private updateRule(
    id: number,
    request: CreateAlertRuleRequest
  ): void {

    this.alertRuleService.updateAlertRule(
      id,
      request
    ).subscribe({
      next: () => {
        this.isSaving = false;

        this.saveMessage =
          'Regla de alerta actualizada correctamente.';

        this.loadRules();

        setTimeout(() => {
          this.closeModal();
        }, 500);
      },

      error: (error) => {
        console.error(
          'Error al actualizar regla:',
          error
        );

        this.saveError =
          this.getApiErrorMessage(
            error,
            'No fue posible actualizar la regla de alerta.'
          );

        this.isSaving = false;
      }
    });
  }

  // =========================================================
  // ELIMINAR
  // =========================================================

  deleteRule(rule: AlertRule): void {

    if (!this.isAdmin || this.isDeleting) {
      return;
    }

    const confirmed = window.confirm(
      `¿Deseas eliminar la regla de "${rule.phenomenon}"?`
    );

    if (!confirmed) {
      return;
    }

    this.isDeleting = true;
    this.listError = '';

    this.alertRuleService.deleteAlertRule(
      rule.id
    ).subscribe({
      next: () => {
        this.isDeleting = false;

        this.rules = this.rules.filter(
          item => item.id !== rule.id
        );
      },

      error: (error) => {
        console.error(
          'Error al eliminar regla:',
          error
        );

        this.isDeleting = false;

        this.listError =
          this.getApiErrorMessage(
            error,
            'No fue posible eliminar la regla de alerta.'
          );
      }
    });
  }

  // =========================================================
  // VALIDACIÓN
  // =========================================================

  private validateForm(): string | null {

    if (!this.sensorType) {
      return 'Selecciona el tipo de sensor.';
    }

    if (!this.operator) {
      return 'Selecciona el operador.';
    }

    if (
      this.thresholdValue === null ||
      this.thresholdValue === undefined ||
      Number.isNaN(Number(this.thresholdValue))
    ) {
      return 'Ingresa un valor de umbral válido.';
    }

    if (
      this.sensorType !== 'TEMPERATURE' &&
      Number(this.thresholdValue) < 0
    ) {
      return 'El umbral no puede ser negativo para este tipo de sensor.';
    }

    if (!this.riskLevel) {
      return 'Selecciona el nivel de riesgo.';
    }

    if (!this.phenomenon.trim()) {
      return 'Ingresa el fenómeno asociado.';
    }

    if (!this.description.trim()) {
      return 'Ingresa una descripción.';
    }

    return null;
  }

  // =========================================================
  // UTILIDADES
  // =========================================================

  resetForm(): void {
    this.sensorType = '';
    this.operator = '>=';
    this.thresholdValue = null;
    this.riskLevel = '';
    this.phenomenon = '';
    this.description = '';
    this.editingRuleId = null;
  }

  getSensorTypeLabel(type: string): string {
    const option = this.sensorTypes.find(
      item => item.value === type
    );

    return option?.label ?? type;
  }

  getRiskLevelLabel(level: string): string {
    const option = this.riskLevels.find(
      item => item.value === level
    );

    return option?.label ?? level;
  }

  getRiskBadgeClass(level: string): string {
    switch (level) {
      case 'YELLOW':
        return 'text-bg-warning';

      case 'ORANGE':
        return 'text-bg-orange';

      case 'RED':
        return 'text-bg-danger';

      default:
        return 'text-bg-secondary';
    }
  }

  private getApiErrorMessage(
    error: any,
    fallback: string
  ): string {

    return (
      error?.error?.message ??
      error?.error?.title ??
      error?.message ??
      fallback
    );
  }
}