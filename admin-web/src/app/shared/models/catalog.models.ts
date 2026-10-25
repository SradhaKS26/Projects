export type FulfillmentKind = 'onSite' | 'ride' | 'parcel';

export interface ServiceCategory {
  id: string;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  isActive: boolean;
  serviceCount: number;
  createdAt: string;
  updatedAt?: string | null;
  fulfillmentKind: FulfillmentKind;
}

export interface ServiceCategoryPayload {
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  isActive: boolean;
  fulfillmentKind: FulfillmentKind;
}

export interface CatalogService {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string | null;
  basePrice: number;
  isActive: boolean;
  estimatedDurationMinutes?: number | null;
  createdAt: string;
  updatedAt?: string | null;
  fulfillmentKind?: FulfillmentKind;
}

export interface CatalogServicePayload {
  categoryId: string;
  name: string;
  description?: string | null;
  basePrice: number;
  estimatedDurationMinutes?: number | null;
  isActive: boolean;
}

export interface ServiceFilters {
  categoryId?: string | null;
  search?: string | null;
  includeInactive?: boolean;
}
