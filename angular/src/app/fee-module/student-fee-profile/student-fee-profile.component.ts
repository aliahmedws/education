import { ListService, PagedResultDto } from '@abp/ng.core';
import { ConfirmationService, ToasterService, Confirmation } from '@abp/ng.theme.shared';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { FeeStructureLookupDto, FeeStructureService } from 'src/app/proxy/fee-module/fee-structures';
import { StudentFeeProfileDto, GetStudentFeeProfileListInput, StudentFeeProfileService, CreateUpdateStudentFeeProfileDto } from 'src/app/proxy/fee-module/student-fee-profiles';
import { StudentLookupDto, StudentService } from 'src/app/proxy/students';

@Component({
  selector: 'app-student-fee-profile',
  standalone: false,
  templateUrl: './student-fee-profile.component.html',
  styleUrl: './student-fee-profile.component.scss',
  providers: [ListService]
})
export class StudentFeeProfileComponent implements OnInit{
 profiles = { items: [], totalCount: 0 } as PagedResultDto<StudentFeeProfileDto>;

  showFilter = false;

  isModalOpen = false;
  form!: FormGroup;
  selected = {} as StudentFeeProfileDto;

  filters = {} as GetStudentFeeProfileListInput;

  studentOptions: StudentLookupDto[] = [];
  feeStructureOptions: FeeStructureLookupDto[] = [];

  public readonly list = inject(ListService);
  private readonly service = inject(StudentFeeProfileService);
  private readonly studentService = inject(StudentService);
  private readonly feeStructureService = inject(FeeStructureService);

  private readonly fb = inject(FormBuilder);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);

  ngOnInit(): void {
    const streamCreator = (query: GetStudentFeeProfileListInput) =>
      this.service.getList({ ...query, ...this.filters });

    this.list.hookToQuery(streamCreator).subscribe(res => (this.profiles = res));

    this.loadStudents();
    this.loadFeeStructures();
  }

  private loadStudents(): void {
    // adjust name if your method differs
    this.studentService.getStudentLookup().subscribe(res => {
      this.studentOptions = res || [];
    });
  }

  private loadFeeStructures(): void {
    this.feeStructureService.getFeeStructureLookup().subscribe(res => {
      this.feeStructureOptions = res || [];
    });
  }

  create(): void {
    this.selected = {} as StudentFeeProfileDto;
    this.buildForm();
    this.isModalOpen = true;
  }

  edit(id: string): void {
    this.service.get(id).subscribe(dto => {
      this.selected = dto;
      this.buildForm();
      this.isModalOpen = true;
    });
  }

  save(): void {
    if (this.form.invalid) return;

    const input = this.form.value as CreateUpdateStudentFeeProfileDto;

    if (this.selected.id) {
      this.service.update(this.selected.id, input).subscribe(() => {
        this.toaster.success('::UpdatedSuccessfully');
        this.closeModalAndRefresh();
      });
    } else {
      this.service.create(input).subscribe(() => {
        this.toaster.success('::CreatedSuccessfully');
        this.closeModalAndRefresh();
      });
    }
  }

  delete(id: string): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.service.delete(id).subscribe(() => {
          this.toaster.success('::DeletedSuccessfully');
          this.list.get();
        });
      }
    });
  }

  clearFilters(): void {
    this.filters = {} as GetStudentFeeProfileListInput;
    this.list.get();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      studentId: [this.selected.studentId || null, Validators.required],
      feeStructureId: [this.selected.feeStructureId || null, Validators.required],
      effectiveFrom: [
        this.toDateInput(this.selected.effectiveFrom) || this.toDateInput(new Date()),
        Validators.required,
      ],
      effectiveTo: [this.toDateInput(this.selected.effectiveTo)],
      isActive: [this.selected.id ? this.selected.isActive : true],
    });
  }

  private closeModalAndRefresh(): void {
    this.isModalOpen = false;
    this.form.reset();
    this.list.get();
  }

  // ---- UI helpers ----

  studentLabel(s: StudentLookupDto): string {
    const name = `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim();
    return s.admissionNo ? `${name} (${s.admissionNo})` : name;
  }

  feeStructureLabel(f: FeeStructureLookupDto): string {
    return f.displayName ?? f.id;
  }

  studentNameById(id: string): string {
    const s = this.studentOptions.find(x => x.id === id);
    return s ? this.studentLabel(s) : id;
  }

  feeStructureNameById(id: string): string {
    const f = this.feeStructureOptions.find(x => x.id === id);
    return f ? this.feeStructureLabel(f) : id;
  }

  private toDateInput(value: any): string | null {
    if (!value) return null;
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return null;
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }
}
