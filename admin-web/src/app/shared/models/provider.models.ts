export type ProviderApprovalStatus = 'pending' | 'approved' | 'rejected' | 'suspended';
export type AvailabilityStatus = 'unavailable' | 'available' | 'busy';
export type ProviderServiceStatus = 'pending' | 'approved' | 'rejected';

export interface ProviderListItem {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  approvalStatus: ProviderApprovalStatus;
  isActive: boolean;
  availabilityStatus: AvailabilityStatus;
  appliedServices: string[];
  documentCount: number;
  hasMissingRequiredDocuments: boolean;
  createdAt: string;
  reviewedAt?: string | null;
}

export interface ProviderServiceItem {
  serviceId: string;
  serviceName: string;
  categoryId: string;
  categoryName: string;
  basePrice: number;
  status: ProviderServiceStatus;
  appliedAt: string;
  reviewedAt?: string | null;
}

export interface ProviderDocument {
  id: string;
  documentRequirementId: string;
  requirementName: string;
  categoryId: string;
  categoryName: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

export interface MissingDocumentRequirement {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string | null;
}

export interface ProviderProfile {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  description?: string | null;
  approvalStatus: ProviderApprovalStatus;
  isActive: boolean;
  availabilityStatus: AvailabilityStatus;
  rating: number;
  reviewReason?: string | null;
  reviewedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  services: ProviderServiceItem[];
  documents: ProviderDocument[];
  missingRequiredDocuments: MissingDocumentRequirement[];
}

export interface CategoryDocumentRequirement {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string | null;
  isRequired: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface DocumentRequirementPayload {
  name: string;
  description?: string | null;
  isRequired: boolean;
  sortOrder: number;
}
