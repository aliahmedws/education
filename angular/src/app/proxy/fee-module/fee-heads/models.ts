import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateFeeHeadDto {
  name: string;
  isActive: boolean;
}

export interface FeeHeadDto extends FullAuditedEntityDto<string> {
  tenantId?: string;
  name?: string;
  isActive: boolean;
}

export interface GetFeeHeadListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}
