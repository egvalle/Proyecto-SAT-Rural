import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';
import { ModalComponent } from '../../../../shared/components/ui/modal/modal.component';

import { Sensor } from '../../../../core/models/sensor';
import { Community } from '../../../../core/models/community';

import { SensorService } from '../../../../core/services/sensor.service';
import { CommunityService } from '../../../../core/services/community.service';

interface SensorTypeOption {
  value: string;
  label: string;
  units: string[];
  allowCustomUnit?: boolean;
}

@Component({
  selector: 'app-sensors',
  imports: [
    ComponentCardComponent,
    ModalComponent,
    FormsModule
  ],
  templateUrl: './sensors.html',
  styleUrl: './sensors.scss',
})
export class Sensors implements OnInit {

  sensors: Sensor[] = [];
  communities: Community[] = [];

  isLoadingSensors = false;
  isLoadingCommunities = false;
  isSaving = false;

  saveMessage = '';
  saveError = '';
  sensorListError = '';

  editingSensorId: number | null = null;

  sensorCommunityId: number | null = null;
  sensorCode = '';
  sensorName = '';
  sensorType = 'TEMPERATURE';
  sensorUnit = '°C';
  sensorLocation = '';
  sensorInstallationDate = '';
  sensorDescription = '';
  sensorIsActive = true;

  filterSearch = '';
  filterCode = '';
  filterType = '';
  filterCommunityId: number | null = null;
  filterStatus = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;

  // ============================================================
  // MODAL - NUEVA COMUNIDAD
  // ============================================================

  isCommunityModalOpen = false;
  isSavingCommunity = false;
  communityError = '';

  newCommunityName = '';
  newCommunityMunicipality = '';
  newCommunityDepartment = '';
  newCommunityCountry = 'Guatemala';
  newCommunityLatitude: number | null = null;
  newCommunityLongitude: number | null = null;
  newCommunityDescription = '';
  newCommunityIsActive = true;

  // ============================================================
  // MODAL - VALOR SIMULADO
  // ============================================================

  isSimulationModalOpen = false;
  selectedSimulationSensor: Sensor | null = null;

  simulationValue: number | null = null;

  isSavingSimulation = false;
  isResettingSimulation = false;

  simulationMessage = '';
  simulationError = '';

  readonly sensorTypes: SensorTypeOption[] = [
    {
      value: 'TEMPERATURE',
      label: 'Temperatura',
      units: ['°C', '°F']
    },
    {
      value: 'HUMIDITY',
      label: 'Humedad',
      units: ['%']
    },
    {
      value: 'WIND_SPEED',
      label: 'Velocidad del viento',
      units: ['km/h', 'm/s']
    },
    {
      value: 'RAINFALL',
      label: 'Lluvia',
      units: ['mm', 'mm/h']
    },
    {
      value: 'RIVER_LEVEL',
      label: 'Nivel de río',
      units: ['m', 'cm', '%']
    },
    {
      value: 'RESERVOIR_LEVEL',
      label: 'Nivel de embalse',
      units: ['m', 'cm', '%']
    },
    {
      value: 'SMOKE_FIRE',
      label: 'Humo / Incendio',
      units: ['ppm', '%']
    },
    {
      value: 'OTHER_ENVIRONMENTAL',
      label: 'Otro ambiental',
      units: [],
      allowCustomUnit: true
    }
  ];

  constructor(
    private readonly sensorService: SensorService,
    private readonly communityService: CommunityService
  ) {}

  ngOnInit(): void {
    this.loadCommunities();
    this.loadSensors();
  }

  // ============================================================
  // TIPOS Y UNIDADES
  // ============================================================

  get selectedSensorType(): SensorTypeOption | undefined {
    return this.sensorTypes.find(
      type => type.value === this.sensorType
    );
  }

  get availableUnits(): string[] {
    return this.selectedSensorType?.units ?? [];
  }

  get allowsCustomUnit(): boolean {
    return this.selectedSensorType?.allowCustomUnit === true;
  }

  // ============================================================
  // COMUNIDADES
  // ============================================================

  loadCommunities(
    selectCommunityId?: number
  ): void {

    this.isLoadingCommunities = true;

    this.communityService.getCommunities(
      '',
      '',
      '',
      true,
      1,
      100
    ).subscribe({
      next: response => {
        this.communities = response.items;
        this.isLoadingCommunities = false;

        if (selectCommunityId !== undefined) {
          this.sensorCommunityId = selectCommunityId;
          return;
        }

        if (
          this.sensorCommunityId === null &&
          this.communities.length > 0
        ) {
          this.sensorCommunityId =
            this.communities[0].id;
        }
      },

      error: () => {
        this.communities = [];
        this.isLoadingCommunities = false;
      }
    });
  }

  // ============================================================
  // LISTADO DE SENSORES
  // ============================================================

  loadSensors(page = 1): void {

    this.sensorListError = '';
    this.isLoadingSensors = true;

    const status =
      this.filterStatus === ''
        ? undefined
        : this.filterStatus === 'true';

    const communityId =
      this.filterCommunityId === null
        ? undefined
        : this.filterCommunityId;

    this.sensorService.getSensors(
      this.filterSearch,
      this.filterCode,
      this.filterType,
      communityId,
      status,
      page,
      this.pageSize
    ).subscribe({
      next: response => {
        this.sensors = response.items;
        this.currentPage = response.page;
        this.pageSize = response.pageSize;
        this.totalItems = response.totalItems;
        this.totalPages = response.totalPages;
        this.isLoadingSensors = false;
      },

      error: () => {
        this.sensors = [];
        this.totalItems = 0;
        this.totalPages = 0;
        this.isLoadingSensors = false;

        this.sensorListError =
          'No se pudo consultar la lista de sensores.';
      }
    });
  }

  filterSensors(): void {
    this.loadSensors(1);
  }

  clearFilters(): void {
    this.filterSearch = '';
    this.filterCode = '';
    this.filterType = '';
    this.filterCommunityId = null;
    this.filterStatus = '';

    this.loadSensors(1);
  }

  // ============================================================
  // TIPO DE SENSOR
  // ============================================================

  onSensorTypeChange(): void {

    const selectedType =
      this.selectedSensorType;

    if (!selectedType) {
      this.sensorUnit = '';
      return;
    }

    if (selectedType.units.length > 0) {
      this.sensorUnit =
        selectedType.units[0];
    } else {
      this.sensorUnit = '';
    }
  }

  // ============================================================
  // GUARDAR SENSOR
  // ============================================================

  saveSensor(): void {

    this.saveMessage = '';
    this.saveError = '';

    if (this.sensorCommunityId === null) {
      this.saveError =
        'Selecciona una comunidad.';
      return;
    }

    if (
      !this.sensorCode.trim() ||
      !this.sensorName.trim() ||
      !this.sensorType.trim() ||
      !this.sensorUnit.trim()
    ) {
      this.saveError =
        'Completa los campos obligatorios del sensor.';
      return;
    }

    this.isSaving = true;

    if (this.editingSensorId === null) {

      this.sensorService.createSensor({
        communityId:
          this.sensorCommunityId,

        code:
          this.sensorCode.trim(),

        name:
          this.sensorName.trim(),

        type:
          this.sensorType,

        unit:
          this.sensorUnit.trim(),

        location:
          this.sensorLocation.trim() || null,

        installationDate:
          this.sensorInstallationDate || null,

        description:
          this.sensorDescription.trim() || null,

        isActive:
          this.sensorIsActive

      }).subscribe({
        next: () => {
          this.isSaving = false;

          this.saveMessage =
            'Sensor registrado correctamente.';

          this.resetForm();
          this.loadSensors(1);
        },

        error: error => {
          this.isSaving = false;

          this.saveError =
            error?.error?.message ??
            'No se pudo registrar el sensor.';
        }
      });

      return;
    }

    this.sensorService.updateSensor(
      this.editingSensorId,
      {
        communityId:
          this.sensorCommunityId,

        code:
          this.sensorCode.trim(),

        name:
          this.sensorName.trim(),

        type:
          this.sensorType,

        unit:
          this.sensorUnit.trim(),

        location:
          this.sensorLocation.trim() || null,

        installationDate:
          this.sensorInstallationDate || null,

        description:
          this.sensorDescription.trim() || null
      }
    ).subscribe({
      next: () => {
        this.isSaving = false;

        this.saveMessage =
          'Sensor actualizado correctamente.';

        this.resetForm();

        this.loadSensors(
          this.currentPage
        );
      },

      error: error => {
        this.isSaving = false;

        this.saveError =
          error?.error?.message ??
          'No se pudo actualizar el sensor.';
      }
    });
  }

  // ============================================================
  // EDITAR SENSOR
  // ============================================================

  editSensor(sensor: Sensor): void {

    this.saveMessage = '';
    this.saveError = '';

    this.editingSensorId =
      sensor.id;

    this.sensorCommunityId =
      sensor.communityId;

    this.sensorCode =
      sensor.code;

    this.sensorName =
      sensor.name;

    this.sensorType =
      sensor.type;

    this.sensorUnit =
      sensor.unit;

    this.sensorLocation =
      sensor.location ?? '';

    this.sensorInstallationDate =
      sensor.installationDate
        ? sensor.installationDate.substring(0, 10)
        : '';

    this.sensorDescription =
      sensor.description ?? '';

    this.sensorIsActive =
      sensor.isActive;

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  cancelEdit(): void {
    this.resetForm();
    this.saveMessage = '';
    this.saveError = '';
  }

  // ============================================================
  // ESTADO DEL SENSOR
  // ============================================================

  toggleStatus(sensor: Sensor): void {

    this.sensorListError = '';

    this.sensorService.changeStatus(
      sensor.id,
      !sensor.isActive
    ).subscribe({
      next: updatedSensor => {

        sensor.isActive =
          updatedSensor.isActive;

        /*
         * Si el sensor fue desactivado y estaba
         * seleccionado en el modal de simulación,
         * cerramos el modal.
         */
        if (
          !updatedSensor.isActive &&
          this.selectedSimulationSensor?.id === sensor.id
        ) {
          this.closeSimulationModal();
        }
      },

      error: error => {
        this.sensorListError =
          error?.error?.message ??
          'No se pudo cambiar el estado del sensor.';
      }
    });
  }

  // ============================================================
  // SIMULACIÓN MANUAL
  // ============================================================

  openSimulationModal(
    sensor: Sensor
  ): void {

    if (!sensor.isActive) {
      this.sensorListError =
        'Solo los sensores activos pueden modificar su valor simulado.';
      return;
    }

    this.selectedSimulationSensor =
      sensor;

    this.simulationValue = null;

    this.simulationMessage = '';
    this.simulationError = '';

    this.isSimulationModalOpen = true;
  }

  closeSimulationModal(): void {

    if (
      this.isSavingSimulation ||
      this.isResettingSimulation
    ) {
      return;
    }

    this.isSimulationModalOpen = false;
    this.selectedSimulationSensor = null;
    this.simulationValue = null;
    this.simulationMessage = '';
    this.simulationError = '';
  }

  applySimulationValue(): void {

    this.simulationMessage = '';
    this.simulationError = '';

    if (
      this.selectedSimulationSensor === null
    ) {
      this.simulationError =
        'No se ha seleccionado un sensor.';
      return;
    }

    if (
      this.simulationValue === null ||
      this.simulationValue === undefined
    ) {
      this.simulationError =
        'Ingresa el valor que deseas simular.';
      return;
    }

    if (!Number.isFinite(
      Number(this.simulationValue)
    )) {
      this.simulationError =
        'Ingresa un valor numérico válido.';
      return;
    }

    this.isSavingSimulation = true;

    this.sensorService.setSimulationValue(
      this.selectedSimulationSensor.id,
      Number(this.simulationValue)
    ).subscribe({
      next: response => {

        this.isSavingSimulation = false;

        this.simulationMessage =
          response.message ??
          'Valor simulado actualizado correctamente.';
      },

      error: error => {

        this.isSavingSimulation = false;

        this.simulationError =
          error?.error?.message ??
          'No se pudo modificar el valor simulado.';
      }
    });
  }

  resetSimulationValue(): void {

    this.simulationMessage = '';
    this.simulationError = '';

    if (
      this.selectedSimulationSensor === null
    ) {
      this.simulationError =
        'No se ha seleccionado un sensor.';
      return;
    }

    this.isResettingSimulation = true;

    this.sensorService.resetSimulationValue(
      this.selectedSimulationSensor.id
    ).subscribe({
      next: response => {

        this.isResettingSimulation = false;

        this.simulationValue = null;

        this.simulationMessage =
          response.message ??
          'El sensor volvió al modo automático.';
      },

      error: error => {

        this.isResettingSimulation = false;

        this.simulationError =
          error?.error?.message ??
          'No se pudo restablecer la simulación automática.';
      }
    });
  }

  // ============================================================
  // PAGINACIÓN
  // ============================================================

  previousPage(): void {

    if (this.currentPage > 1) {
      this.loadSensors(
        this.currentPage - 1
      );
    }
  }

  nextPage(): void {

    if (
      this.currentPage <
      this.totalPages
    ) {
      this.loadSensors(
        this.currentPage + 1
      );
    }
  }

  // ============================================================
  // ETIQUETA DEL TIPO
  // ============================================================

  getSensorTypeLabel(
    type: string
  ): string {

    return this.sensorTypes.find(
      item => item.value === type
    )?.label ?? type;
  }

  // ============================================================
  // MODAL COMUNIDAD
  // ============================================================

  openCommunityModal(): void {
    this.communityError = '';
    this.isCommunityModalOpen = true;
  }

  closeCommunityModal(): void {

    if (this.isSavingCommunity) {
      return;
    }

    this.isCommunityModalOpen = false;
    this.communityError = '';
  }

  saveCommunity(): void {

    this.communityError = '';

    if (
      !this.newCommunityName.trim() ||
      !this.newCommunityMunicipality.trim() ||
      !this.newCommunityDepartment.trim() ||
      !this.newCommunityCountry.trim()
    ) {
      this.communityError =
        'Nombre, municipio, departamento y país son obligatorios.';
      return;
    }

    if (
      this.newCommunityLatitude !== null &&
      (
        this.newCommunityLatitude < -90 ||
        this.newCommunityLatitude > 90
      )
    ) {
      this.communityError =
        'La latitud debe estar entre -90 y 90.';
      return;
    }

    if (
      this.newCommunityLongitude !== null &&
      (
        this.newCommunityLongitude < -180 ||
        this.newCommunityLongitude > 180
      )
    ) {
      this.communityError =
        'La longitud debe estar entre -180 y 180.';
      return;
    }

    this.isSavingCommunity = true;

    this.communityService.createCommunity({
      name:
        this.newCommunityName.trim(),

      municipality:
        this.newCommunityMunicipality.trim(),

      department:
        this.newCommunityDepartment.trim(),

      country:
        this.newCommunityCountry.trim(),

      latitude:
        this.newCommunityLatitude,

      longitude:
        this.newCommunityLongitude,

      description:
        this.newCommunityDescription.trim() || null,

      isActive:
        this.newCommunityIsActive

    }).subscribe({
      next: community => {

        this.isSavingCommunity = false;
        this.isCommunityModalOpen = false;

        this.resetCommunityForm();

        this.loadCommunities(
          community.id
        );
      },

      error: error => {

        this.isSavingCommunity = false;

        this.communityError =
          error?.error?.message ??
          'No se pudo registrar la comunidad.';
      }
    });
  }

  // ============================================================
  // RESET FORMULARIOS
  // ============================================================

  private resetCommunityForm(): void {

    this.newCommunityName = '';
    this.newCommunityMunicipality = '';
    this.newCommunityDepartment = '';
    this.newCommunityCountry = 'Guatemala';
    this.newCommunityLatitude = null;
    this.newCommunityLongitude = null;
    this.newCommunityDescription = '';
    this.newCommunityIsActive = true;
    this.communityError = '';
  }

  private resetForm(): void {

    this.editingSensorId = null;

    this.sensorCode = '';
    this.sensorName = '';
    this.sensorType = 'TEMPERATURE';
    this.sensorUnit = '°C';
    this.sensorLocation = '';
    this.sensorInstallationDate = '';
    this.sensorDescription = '';
    this.sensorIsActive = true;

    if (
      this.communities.length > 0
    ) {
      this.sensorCommunityId =
        this.communities[0].id;
    } else {
      this.sensorCommunityId = null;
    }
  }
}