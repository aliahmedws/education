using EHub.FeeModule.FeeStructureItems;
using EHub.FeeModule.LateFeePolicies;
using EHub.FeeModule.StudentFeeDiscounts;
using EHub.FeeModule.StudentFeeProfiles;
using EHub.FeeModule.StudentMonthlyFees;
using EHub.Students;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace EHub.FeeModule.StudentMonthlyFeeLines;

[RemoteService(IsEnabled = false)]
public class StudentMonthlyFeeLineAppService : ApplicationService, IStudentMonthlyFeeLineAppService
{
    private readonly IStudentMonthlyFeeLineRepository _repo;
    private readonly IStudentRepository _studentRepo;
    private readonly StudentMonthlyFeeManager _monthlyFeeManager;
    private readonly StudentMonthlyFeeLineManager _lineManager;
    private readonly IStudentFeeProfileRepository _profileRepo;
    private readonly IStudentMonthlyFeeRepository _monthlyFeeRepo;
    private readonly IStudentFeeDiscountRepository _discountRepo;
    private readonly ILateFeePolicyRepository _lateFeePolicyRepo;
    IRepository<FeeStructureItem, Guid> _feeStructureItemRepo;

    public StudentMonthlyFeeLineAppService(
        IStudentMonthlyFeeLineRepository repo,
        IStudentRepository studentRepo,
        StudentMonthlyFeeManager monthlyFeeManager,
        StudentMonthlyFeeLineManager lineManager,
        IStudentFeeProfileRepository profileRepo,
        IStudentMonthlyFeeRepository monthlyFeeRepo,
        IStudentFeeDiscountRepository discountRepo,
        ILateFeePolicyRepository lateFeePolicyRepo,
        IRepository<FeeStructureItem, Guid> feeStructureItemRepo)
    {
        _repo = repo;
        _studentRepo = studentRepo;
        _monthlyFeeManager = monthlyFeeManager;
        _lineManager = lineManager;
        _profileRepo = profileRepo;
        _monthlyFeeRepo = monthlyFeeRepo;
        _discountRepo = discountRepo;
        _lateFeePolicyRepo = lateFeePolicyRepo;
        _feeStructureItemRepo = feeStructureItemRepo;
    }

    public async Task<StudentMonthlyFeeLineDto> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return ObjectMapper.Map<StudentMonthlyFeeLine, StudentMonthlyFeeLineDto>(entity!);
    }

    public async Task<PagedResultDto<StudentMonthlyFeeLineDto>> GetListAsync(GetStudentMonthlyFeeLineListInput input)
    {
        if (input.Sorting.IsNullOrWhiteSpace())
            input.Sorting = nameof(StudentMonthlyFeeLine.CreationTime) + " DESC";

        var total = await _repo.GetCountAsync(input.StudentMonthlyFeeId, input.FeeHeadId);

        var list = await _repo.GetListAsync(
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            input.StudentMonthlyFeeId,
            input.FeeHeadId);

        var items = list.Select(x => ObjectMapper.Map<StudentMonthlyFeeLine, StudentMonthlyFeeLineDto>(x)).ToList();
        return new PagedResultDto<StudentMonthlyFeeLineDto>(total, items);
    }

    public async Task<StudentMonthlyFeeLineDto> CreateAsync(CreateUpdateStudentMonthlyFeeLineDto input)
    {
        var entity = await _lineManager.CreateAsync(
            input.StudentMonthlyFeeId,
            input.FeeHeadId,
            input.ExpectedAmount,
            input.DiscountAmount,
            input.AdjustmentAmount,
            input.LateFeeAmount,
            input.PaidAmount);

        entity = await _repo.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<StudentMonthlyFeeLine, StudentMonthlyFeeLineDto>(entity);
    }

    public async Task UpdateAsync(Guid id, CreateUpdateStudentMonthlyFeeLineDto input)
    {
        var entity = await _repo.GetAsync(id);

        var t = typeof(StudentMonthlyFeeLine);

        t.GetProperty(nameof(StudentMonthlyFeeLine.StudentMonthlyFeeId))?.SetValue(entity, input.StudentMonthlyFeeId);
        t.GetProperty(nameof(StudentMonthlyFeeLine.FeeHeadId))?.SetValue(entity, input.FeeHeadId);

        t.GetProperty(nameof(StudentMonthlyFeeLine.ExpectedAmount))?.SetValue(entity, input.ExpectedAmount);
        entity.ChangeDiscount(input.DiscountAmount);
        entity.ChangeAdjustment(input.AdjustmentAmount);
        entity.ChangeLateFee(input.LateFeeAmount);
        entity.ApplyPayment(input.PaidAmount);

        await _repo.UpdateAsync(entity, autoSave: true);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
    }

    /// <summary>
    /// Calculate discount amount for a student and fee head for a specific month
    /// </summary>
    // Fixed CalculateAmountsAsync method in StudentMonthlyFeeLineAppService

    public async Task<CalculatedAmountsDto> CalculateAmountsAsync(CalculateFeeLineAmountsInput input)
    {
        var result = new CalculatedAmountsDto
        {
            ExpectedAmount = 0,
            DiscountAmount = 0,
            LateFeeAmount = 0
        };

        var monthlyFee = await _monthlyFeeRepo.GetAsync(input.StudentMonthlyFeeId);
        var student = await _studentRepo.GetAsync(monthlyFee.StudentId);

        // Get student's fee structure
        var profile = await _profileRepo.FirstOrDefaultAsync(p =>
            p.StudentId == student.Id &&
            p.IsActive &&
            p.EffectiveFrom <= monthlyFee.Month &&
            (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= monthlyFee.Month)
        );

        // ✅ NEW: Get Expected Amount from FeeStructureItem
        if (profile != null && input.FeeHeadId != Guid.Empty)
        {
            var structureItem = await _feeStructureItemRepo.FirstOrDefaultAsync(x =>
                x.FeeStructureId == profile.FeeStructureId &&
                x.FeeHeadId == input.FeeHeadId
            );

            if (structureItem != null)
            {
                result.ExpectedAmount = structureItem.MonthlyAmount;
            }
        }

        // 1. Calculate Discount
        var discount = await _discountRepo.GetApplicableDiscountAsync(
            monthlyFee.StudentId,
            input.FeeHeadId,
            monthlyFee.Month);

        if (discount != null && discount.IsActive && discount.ApprovedByStaffId != null)
        {
            result.DiscountAmount = discount.DiscountType switch
            {
                DiscountType.Percent => result.ExpectedAmount * (discount.Value / 100m),
                DiscountType.Fixed => discount.Value,
                _ => 0
            };
        }

        // 2. Calculate Late Fee
        if (monthlyFee.DueDate.HasValue && DateTime.Now > monthlyFee.DueDate.Value)
        {
            var policy = await _lateFeePolicyRepo.FindApplicablePolicyAsync(
                (int?)student.GradeLevel,
                (int?)student.Section,
                (int?)student.Shift,
                (int?)student.Term);

            if (policy != null && policy.IsActive)
            {
                var daysLate = (DateTime.Now - monthlyFee.DueDate.Value).Days;

                if (daysLate > policy.GraceDays)
                {
                    var effectiveDaysLate = daysLate - policy.GraceDays;
                    var netAmount = result.ExpectedAmount - result.DiscountAmount;

                    result.LateFeeAmount = policy.Type switch
                    {
                        LateFeeType.FixedOnce => policy.Value,
                        LateFeeType.FixedPerDay => policy.Value * effectiveDaysLate,
                        _ => 0
                    };
                }
            }
        }

        result.NetAmount = result.ExpectedAmount - result.DiscountAmount + result.LateFeeAmount;

        return result;
    }
    public async Task<BulkGenerateStudentMonthlyFeeResultDto> BulkGenerateAsync(BulkGenerateStudentMonthlyFeeDto input)
    {
        var month = new DateTime(input.Month.Year, input.Month.Month, 1);

        var studentsQ = await _studentRepo.GetQueryableAsync();

        studentsQ = studentsQ
            .WhereIf(input.GradeLevel.HasValue, x => x.GradeLevel == input.GradeLevel)
            .WhereIf(input.Section.HasValue, x => x.Section == input.Section)
            .WhereIf(input.Shift.HasValue, x => x.Shift == input.Shift)
            .WhereIf(input.Term.HasValue, x => x.Term == input.Term);

        var students = studentsQ.Select(x => new { x.Id }).ToList();

        var result = new BulkGenerateStudentMonthlyFeeResultDto
        {
            TotalStudents = students.Count
        };

        if (!students.Any())
            return result;

        foreach (var s in students)
        {
            var monthlyFee = await _monthlyFeeManager.CreateAsync(
                s.Id,
                month,
                input.DueDate,
                input.Remarks,
                input.SkipIfExists);

            if (monthlyFee == null)
            {
                result.Skipped++;
                continue;
            }

            await _monthlyFeeRepo.InsertAsync(monthlyFee, autoSave: false);
            result.Created++;

            var profile = await _profileRepo.FirstOrDefaultAsync(p =>
                p.StudentId == s.Id &&
                p.IsActive &&
                p.EffectiveFrom <= month &&
                (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= month)
            );

            if (profile == null)
            {
                result.MissingFeeProfile++;
                continue;
            }

            (int linesCreated, int linesSkipped) = await _lineManager.GenerateLinesFromFeeStructureAsync(
                monthlyFee.Id,
                profile.FeeStructureId,
                skipIfExists: input.SkipIfExists
            );

            result.LinesCreated += linesCreated;
            result.LinesSkipped += linesSkipped;
        }

        await CurrentUnitOfWork!.SaveChangesAsync();
        return result;
    }
}