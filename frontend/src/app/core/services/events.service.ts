import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Event } from '../models/event.model';

export interface EventFilters {
  communityId?: number;
  eventType?: string;
  level?: string;
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EventsService {

  private readonly apiUrl = '/api/events';

  constructor(private http: HttpClient) {
  }

  getEvents(filters?: EventFilters): Observable<Event[]> {

    let params = new HttpParams();

    if (filters?.communityId) {
      params = params.set(
        'communityId',
        filters.communityId.toString()
      );
    }

    if (filters?.eventType) {
      params = params.set(
        'eventType',
        filters.eventType
      );
    }

    if (filters?.level) {
      params = params.set(
        'level',
        filters.level
      );
    }

    if (filters?.from) {
      params = params.set(
        'from',
        filters.from
      );
    }

    if (filters?.to) {
      params = params.set(
        'to',
        filters.to
      );
    }

    return this.http.get<Event[]>(
      this.apiUrl,
      { params }
    );
  }

  getEvent(id: number): Observable<Event> {
    return this.http.get<Event>(
      `${this.apiUrl}/${id}`
    );
  }
}