import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import {
  CategoryDocumentRequirement,
  DocumentRequirementPayload,
  ProviderApprovalStatus,
  ProviderDocument,
  ProviderListItem,
  ProviderProfile,
} from '../../shared/models/provider.models';

@Injectable({ providedIn: 'root' })
export class ProviderApiService {
  private readonly http = inject(HttpClient);
  private readonly providersUrl = `${environment.apiBaseUrl}/providers`;

  list(filters: {
    approvalStatus?: ProviderApprovalStatus | '';
    search?: string;
  } = {}): Observable<ProviderListItem[]> {
    let params = new HttpParams();
    if (filters.approvalStatus) {
      params = params.set('approvalStatus', filters.approvalStatus);
    }
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    return this.http
      .get<ApiResponse<ProviderListItem[]>>(this.providersUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  getById(id: string): Observable<ProviderProfile> {
    return this.http
      .get<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}`)
      .pipe(map((response) => response.data as ProviderProfile));
  }

  getMine(): Observable<ProviderProfile> {
    return this.http
      .get<ApiResponse<ProviderProfile>>(`${this.providersUrl}/me`)
      .pipe(map((response) => response.data as ProviderProfile));
  }

  saveApplication(description: string | null, serviceIds: string[]): Observable<ProviderProfile> {
    return this.http
      .put<ApiResponse<ProviderProfile>>(`${this.providersUrl}/me/application`, {
        description,
        serviceIds,
      })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  setAvailability(availabilityStatus: string): Observable<ProviderProfile> {
    return this.http
      .put<ApiResponse<ProviderProfile>>(`${this.providersUrl}/me/availability`, {
        availabilityStatus,
      })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  uploadDocument(documentRequirementId: string, file: File): Observable<ProviderDocument> {
    const form = new FormData();
    form.append('documentRequirementId', documentRequirementId);
    form.append('file', file, file.name);

    return this.http
      .post<ApiResponse<ProviderDocument>>(`${this.providersUrl}/me/documents`, form)
      .pipe(map((response) => response.data as ProviderDocument));
  }

  deleteMyDocument(documentId: string): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.providersUrl}/me/documents/${documentId}`)
      .pipe(map(() => undefined));
  }

  approve(id: string, reason?: string | null): Observable<ProviderProfile> {
    return this.http
      .post<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}/approve`, { reason })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  reject(id: string, reason: string): Observable<ProviderProfile> {
    return this.http
      .post<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}/reject`, { reason })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  suspend(id: string, reason: string): Observable<ProviderProfile> {
    return this.http
      .post<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}/suspend`, { reason })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  reinstate(id: string): Observable<ProviderProfile> {
    return this.http
      .post<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}/reinstate`, {})
      .pipe(map((response) => response.data as ProviderProfile));
  }

  setActive(id: string, isActive: boolean): Observable<ProviderProfile> {
    return this.http
      .put<ApiResponse<ProviderProfile>>(`${this.providersUrl}/${id}/active`, { isActive })
      .pipe(map((response) => response.data as ProviderProfile));
  }

  downloadDocument(documentId: string): Observable<Blob> {
    return this.http.get(`${environment.apiBaseUrl}/provider-documents/${documentId}/file`, {
      responseType: 'blob',
    });
  }

  getDocumentRequirements(categoryId: string): Observable<CategoryDocumentRequirement[]> {
    return this.http
      .get<ApiResponse<CategoryDocumentRequirement[]>>(
        `${environment.apiBaseUrl}/service-categories/${categoryId}/document-requirements`,
      )
      .pipe(map((response) => response.data ?? []));
  }

  createDocumentRequirement(
    categoryId: string,
    payload: DocumentRequirementPayload,
  ): Observable<CategoryDocumentRequirement> {
    return this.http
      .post<ApiResponse<CategoryDocumentRequirement>>(
        `${environment.apiBaseUrl}/service-categories/${categoryId}/document-requirements`,
        payload,
      )
      .pipe(map((response) => response.data as CategoryDocumentRequirement));
  }

  updateDocumentRequirement(
    id: string,
    payload: DocumentRequirementPayload,
  ): Observable<CategoryDocumentRequirement> {
    return this.http
      .put<ApiResponse<CategoryDocumentRequirement>>(
        `${environment.apiBaseUrl}/document-requirements/${id}`,
        payload,
      )
      .pipe(map((response) => response.data as CategoryDocumentRequirement));
  }

  deleteDocumentRequirement(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${environment.apiBaseUrl}/document-requirements/${id}`)
      .pipe(map(() => undefined));
  }
}
