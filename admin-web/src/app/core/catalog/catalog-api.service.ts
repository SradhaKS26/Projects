import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import {
  CatalogService,
  CatalogServicePayload,
  ServiceCategory,
  ServiceCategoryPayload,
  ServiceFilters,
} from '../../shared/models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);
  private readonly categoriesUrl = `${environment.apiBaseUrl}/service-categories`;
  private readonly servicesUrl = `${environment.apiBaseUrl}/services`;

  getCategories(includeInactive = false): Observable<ServiceCategory[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http
      .get<ApiResponse<ServiceCategory[]>>(this.categoriesUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  createCategory(payload: ServiceCategoryPayload): Observable<ServiceCategory> {
    return this.http
      .post<ApiResponse<ServiceCategory>>(this.categoriesUrl, payload)
      .pipe(map((response) => response.data as ServiceCategory));
  }

  updateCategory(id: string, payload: ServiceCategoryPayload): Observable<ServiceCategory> {
    return this.http
      .put<ApiResponse<ServiceCategory>>(`${this.categoriesUrl}/${id}`, payload)
      .pipe(map((response) => response.data as ServiceCategory));
  }

  disableCategory(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.categoriesUrl}/${id}`)
      .pipe(map(() => undefined));
  }

  getServices(filters: ServiceFilters = {}): Observable<CatalogService[]> {
    let params = new HttpParams().set('includeInactive', filters.includeInactive ?? false);

    if (filters.categoryId) {
      params = params.set('categoryId', filters.categoryId);
    }

    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    return this.http
      .get<ApiResponse<CatalogService[]>>(this.servicesUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  createService(payload: CatalogServicePayload): Observable<CatalogService> {
    return this.http
      .post<ApiResponse<CatalogService>>(this.servicesUrl, payload)
      .pipe(map((response) => response.data as CatalogService));
  }

  updateService(id: string, payload: CatalogServicePayload): Observable<CatalogService> {
    return this.http
      .put<ApiResponse<CatalogService>>(`${this.servicesUrl}/${id}`, payload)
      .pipe(map((response) => response.data as CatalogService));
  }

  deleteService(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.servicesUrl}/${id}`)
      .pipe(map(() => undefined));
  }
}
