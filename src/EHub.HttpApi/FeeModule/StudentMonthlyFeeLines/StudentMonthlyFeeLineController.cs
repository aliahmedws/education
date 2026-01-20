using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace EHub.FeeModule.StudentMonthlyFeeLines;

[RemoteService(IsEnabled = true)]
[Area("app")]
[ControllerName("StudentMonthlyFeeLine")]
[Route("api/fee-module/student-monthly-fee-lines")]
public class StudentMonthlyFeeLineController : AbpController, IStudentMonthlyFeeLineAppService
{
    private readonly IStudentMonthlyFeeLineAppService _appService;

    public StudentMonthlyFeeLineController(IStudentMonthlyFeeLineAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<PagedResultDto<StudentMonthlyFeeLineDto>> GetListAsync(GetStudentMonthlyFeeLineListInput input)
        => _appService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<StudentMonthlyFeeLineDto> GetAsync(Guid id)
        => _appService.GetAsync(id);

    [HttpPost]
    public Task<StudentMonthlyFeeLineDto> CreateAsync(CreateUpdateStudentMonthlyFeeLineDto input)
        => _appService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task UpdateAsync(Guid id, CreateUpdateStudentMonthlyFeeLineDto input)
        => _appService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => _appService.DeleteAsync(id);
}
