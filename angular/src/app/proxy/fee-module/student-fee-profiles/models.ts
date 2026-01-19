import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateStudentFeeProfileDto {
  studentId: string;
  feeStructureId: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
}

export interface GetStudentFeeProfileListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  studentId?: string;
  feeStructureId?: string;
  isActive?: boolean;
}

export interface StudentFeeProfileDto extends FullAuditedEntityDto<string> {
  tenantId?: string;
  studentId?: string;
  feeStructureId?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  isActive: boolean;
}
