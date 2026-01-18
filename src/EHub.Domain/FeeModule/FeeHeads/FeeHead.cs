using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace EHub.FeeModule.FeeHeads;

public class FeeHead : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string Name { get; private set; }

    public bool IsActive { get; private set; } = true;

    private FeeHead()
    {
    }

    public FeeHead(
        Guid id,
        string name,
        bool isActive = true
    ) : base(id)
    {
        SetName(name);
        IsActive = isActive;
    }

    public FeeHead ChangeName(string name)
    {
        SetName(name);
        return this;
    }

    public FeeHead Activate()
    {
        IsActive = true;
        return this;
    }

    public FeeHead Deactivate()
    {
        IsActive = false;
        return this;
    }

    private void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(Name), maxLength: FeeModuleConsts.NameMaxLength);
    }
}

