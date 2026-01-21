import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface BulkGenerateStudentMonthlyFeeResultDto {
  totalStudents: number;
  created: number;
  skipped: number;
  linesCreated: number;
  linesSkipped: number;
  missingFeeProfile: number;
}

export interface CalculateFeeLineAmountsInput {
  studentMonthlyFeeId?: string;
  feeHeadId?: string;
}

export interface CalculatedAmountsDto {
  expectedAmount: number;
  discountAmount: number;
  lateFeeAmount: number;
  netAmount: number;
}

export interface CreateUpdateStudentMonthlyFeeLineDto {
  studentMonthlyFeeId: string;
  feeHeadId: string;
  expectedAmount: number;
  discountAmount: number;
  adjustmentAmount: number;
  lateFeeAmount: number;
  paidAmount: number;
}

export interface GetStudentMonthlyFeeLineListInput extends PagedAndSortedResultRequestDto {
  studentMonthlyFeeId?: string;
  feeHeadId?: string;
}

export interface StudentMonthlyFeeLineDto extends EntityDto<string> {
  tenantId?: string;
  studentMonthlyFeeId?: string;
  feeHeadId?: string;
  expectedAmount: number;
  discountAmount: number;
  adjustmentAmount: number;
  lateFeeAmount: number;
  paidAmount: number;
  netAmount: number;
  outstandingAmount: number;
  feeHeadName?: string;
}
