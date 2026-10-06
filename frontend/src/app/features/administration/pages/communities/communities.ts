import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ComponentCardComponent } from '../../../../shared/components/component-card/component-card.component';

import { Community } from '../../../../core/models/community';
import { CommunityService } from '../../../../core/services/community.service';

@Component({
  selector: 'app-communities',
  imports: [
    ComponentCardComponent,
    FormsModule
  ],
  templateUrl: './communities.html',
  styleUrl: './communities.scss'
})
export class Communities implements OnInit {

    get totalSensorsOnPage(): number {
  return this.communities.reduce(
    (total, community) => total + community.sensorCount,
    0
  );
}

  communities: Community[] = [];

  isLoading = false;
  isSaving = false;

  listError = '';
  saveError = '';
  saveMessage = '';

  editingCommunityId: number | null = null;

  communityName = '';
  communityMunicipality = '';
  communityDepartment = '';
  communityCountry = 'Guatemala';

  communityLatitude: number | null = null;
  communityLongitude: number | null = null;

  communityDescription = '';
  communityIsActive = true;

  filterSearch = '';
  filterMunicipality = '';
  filterDepartment = '';
  filterStatus = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;

  constructor(
    private readonly communityService: CommunityService
  ) {}

  ngOnInit(): void {
    this.loadCommunities();
  }

  loadCommunities(page = 1): void {

    this.isLoading = true;
    this.listError = '';

    const status =
      this.filterStatus === ''
        ? undefined
        : this.filterStatus === 'true';

    this.communityService.getCommunities(
      this.filterSearch,
      this.filterMunicipality,
      this.filterDepartment,
      status,
      page,
      this.pageSize
    ).subscribe({

      next: response => {

        this.communities = response.items;

        this.currentPage = response.page;
        this.pageSize = response.pageSize;
        this.totalItems = response.totalItems;
        this.totalPages = response.totalPages;

        this.isLoading = false;
      },

      error: () => {

        this.communities = [];

        this.totalItems = 0;
        this.totalPages = 0;

        this.isLoading = false;

        this.listError =
          'No se pudo consultar la lista de comunidades.';
      }
    });
  }

  filterCommunities(): void {
    this.loadCommunities(1);
  }

  clearFilters(): void {

    this.filterSearch = '';
    this.filterMunicipality = '';
    this.filterDepartment = '';
    this.filterStatus = '';

    this.loadCommunities(1);
  }

  saveCommunity(): void {

    this.saveMessage = '';
    this.saveError = '';

    if (
      !this.communityName.trim() ||
      !this.communityMunicipality.trim() ||
      !this.communityDepartment.trim() ||
      !this.communityCountry.trim()
    ) {

      this.saveError =
        'Nombre, municipio, departamento y país son obligatorios.';

      return;
    }

    if (
      this.communityLatitude !== null &&
      (
        this.communityLatitude < -90 ||
        this.communityLatitude > 90
      )
    ) {

      this.saveError =
        'La latitud debe estar entre -90 y 90.';

      return;
    }

    if (
      this.communityLongitude !== null &&
      (
        this.communityLongitude < -180 ||
        this.communityLongitude > 180
      )
    ) {

      this.saveError =
        'La longitud debe estar entre -180 y 180.';

      return;
    }

    this.isSaving = true;

    if (this.editingCommunityId === null) {

      this.communityService.createCommunity({

        name:
          this.communityName.trim(),

        municipality:
          this.communityMunicipality.trim(),

        department:
          this.communityDepartment.trim(),

        country:
          this.communityCountry.trim(),

        latitude:
          this.communityLatitude,

        longitude:
          this.communityLongitude,

        description:
          this.communityDescription.trim() || null,

        isActive:
          this.communityIsActive

      }).subscribe({

        next: () => {

          this.isSaving = false;

          this.saveMessage =
            'Comunidad registrada correctamente.';

          this.resetForm();
          this.loadCommunities(1);
        },

        error: error => {

          this.isSaving = false;

          this.saveError =
            error?.error?.message ??
            'No se pudo registrar la comunidad.';
        }
      });

      return;
    }

    this.communityService.updateCommunity(
      this.editingCommunityId,
      {

        name:
          this.communityName.trim(),

        municipality:
          this.communityMunicipality.trim(),

        department:
          this.communityDepartment.trim(),

        country:
          this.communityCountry.trim(),

        latitude:
          this.communityLatitude,

        longitude:
          this.communityLongitude,

        description:
          this.communityDescription.trim() || null
      }

    ).subscribe({

      next: () => {

        this.isSaving = false;

        this.saveMessage =
          'Comunidad actualizada correctamente.';

        this.resetForm();

        this.loadCommunities(
          this.currentPage
        );
      },

      error: error => {

        this.isSaving = false;

        this.saveError =
          error?.error?.message ??
          'No se pudo actualizar la comunidad.';
      }
    });
  }

  editCommunity(
    community: Community
  ): void {

    this.saveMessage = '';
    this.saveError = '';

    this.editingCommunityId =
      community.id;

    this.communityName =
      community.name;

    this.communityMunicipality =
      community.municipality;

    this.communityDepartment =
      community.department;

    this.communityCountry =
      community.country;

    this.communityLatitude =
      community.latitude;

    this.communityLongitude =
      community.longitude;

    this.communityDescription =
      community.description ?? '';

    this.communityIsActive =
      community.isActive;

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

  toggleStatus(
    community: Community
  ): void {

    this.listError = '';

    this.communityService.changeStatus(
      community.id,
      !community.isActive
    ).subscribe({

      next: updatedCommunity => {

        community.isActive =
          updatedCommunity.isActive;
      },

      error: error => {

        this.listError =
          error?.error?.message ??
          'No se pudo cambiar el estado de la comunidad.';
      }
    });
  }

  previousPage(): void {

    if (this.currentPage > 1) {

      this.loadCommunities(
        this.currentPage - 1
      );
    }
  }

  nextPage(): void {

    if (
      this.currentPage <
      this.totalPages
    ) {

      this.loadCommunities(
        this.currentPage + 1
      );
    }
  }

  private resetForm(): void {

    this.editingCommunityId = null;

    this.communityName = '';
    this.communityMunicipality = '';
    this.communityDepartment = '';
    this.communityCountry = 'Guatemala';

    this.communityLatitude = null;
    this.communityLongitude = null;

    this.communityDescription = '';
    this.communityIsActive = true;
  }
}