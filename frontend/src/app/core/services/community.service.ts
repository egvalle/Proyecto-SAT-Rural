import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import {
  Community,
  CommunityPagedResult,
  CreateCommunityRequest,
  UpdateCommunityRequest
} from '../models/community';

@Injectable({
  providedIn: 'root'
})
export class CommunityService {

  private readonly communitiesUrl =
    `${environment.apiBaseUrl}/api/communities`;

  constructor(
    private readonly http: HttpClient
  ) {}

  getCommunities(
    search = '',
    municipality = '',
    department = '',
    isActive?: boolean,
    page = 1,
    pageSize = 10
  ): Observable<CommunityPagedResult> {

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search.trim()) {
      params = params.set(
        'search',
        search.trim()
      );
    }

    if (municipality.trim()) {
      params = params.set(
        'municipality',
        municipality.trim()
      );
    }

    if (department.trim()) {
      params = params.set(
        'department',
        department.trim()
      );
    }

    if (isActive !== undefined) {
      params = params.set(
        'isActive',
        isActive.toString()
      );
    }

    return this.http.get<CommunityPagedResult>(
      this.communitiesUrl,
      { params }
    );
  }

  getCommunity(
    id: number
  ): Observable<Community> {

    return this.http.get<Community>(
      `${this.communitiesUrl}/${id}`
    );
  }

  createCommunity(
    request: CreateCommunityRequest
  ): Observable<Community> {

    return this.http.post<Community>(
      this.communitiesUrl,
      request
    );
  }

  updateCommunity(
    id: number,
    request: UpdateCommunityRequest
  ): Observable<Community> {

    return this.http.put<Community>(
      `${this.communitiesUrl}/${id}`,
      request
    );
  }

  changeStatus(
    id: number,
    isActive: boolean
  ): Observable<Community> {

    return this.http.patch<Community>(
      `${this.communitiesUrl}/${id}/status`,
      { isActive }
    );
  }
}