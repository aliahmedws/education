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
    // FIX: You need a separate manager for StudentMonthlyFee (header)
    private readonly StudentMonthlyFeeManager _monthlyFeeManager;  // For creating monthly fee headers
    private readonly StudentMonthlyFeeLineManager _lineManager;     // For creating lines
    private readonly IStudentFeeProfileRepository _profileRepo;
    private readonly IStudentMonthlyFeeRepository _monthlyFeeRepo;  // Repository for monthly fees

    public StudentMonthlyFeeLineAppService(
        IStudentMonthlyFeeLineRepository repo,
        IStudentRepository studentRepo,
        StudentMonthlyFeeManager monthlyFeeManager,        // Inject proper manager
        StudentMonthlyFeeLineManager lineManager,
        IStudentFeeProfileRepository profileRepo,
        IStudentMonthlyFeeRepository monthlyFeeRepo)       // Inject monthly fee repo
    {
        _repo = repo;
        _studentRepo = studentRepo;
        _monthlyFeeManager = monthlyFeeManager;
        _lineManager = lineManager;
        _profileRepo = profileRepo;
        _monthlyFeeRepo = monthlyFeeRepo;
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
            // FIX 1: Use the correct manager for creating monthly fee header
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

            // FIX 2: Explicitly type the tuple deconstruction
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