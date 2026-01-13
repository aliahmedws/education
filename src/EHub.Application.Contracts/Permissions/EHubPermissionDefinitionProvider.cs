using EHub.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace EHub.Permissions;

public class EHubPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(EHubPermissions.GroupName);

        var studentMainPermission =
          myGroup.AddPermission(EHubPermissions.StudentMenuItems.StudentMain, L("Permission:StudentMain"));


        studentMainPermission.AddChild(EHubPermissions.StudentMenuItems.StudentList, L("Permission:StudentList"));
        studentMainPermission.AddChild(EHubPermissions.StudentMenuItems.StudentAttendance, L("Permission:StudentAttendance"));
        studentMainPermission.AddChild(EHubPermissions.StudentMenuItems.StudentAttendanceInsights, L("Permission:StudentAttendanceInsights"));

        var studentsPermission = myGroup.AddPermission(EHubPermissions.Students.Default, L("Permission:Students"));
        studentsPermission.AddChild(EHubPermissions.Students.Create, L("Permission:Students.Create"));
        studentsPermission.AddChild(EHubPermissions.Students.Edit, L("Permission:Students.Edit"));
        studentsPermission.AddChild(EHubPermissions.Students.Delete, L("Permission:Students.Delete"));

        var staffMainPermission =
            myGroup.AddPermission(EHubPermissions.StaffMenuItems.StaffMain, L("Permission:StaffMain"));
        staffMainPermission.AddChild(EHubPermissions.StaffMenuItems.StaffList, L("Permission:StaffList"));
        staffMainPermission.AddChild(EHubPermissions.StaffMenuItems.StaffAttendance, L("Permission:StaffAttendance"));
        staffMainPermission.AddChild(EHubPermissions.StaffMenuItems.StaffAttendanceInsights, L("Permission:StaffAttendanceInsights"));

        var staffsPermission = myGroup.AddPermission(EHubPermissions.Staffs.Default, L("Permission:Staffs"));
        staffsPermission.AddChild(EHubPermissions.Staffs.Create, L("Permission:Staffs.Create"));
        staffsPermission.AddChild(EHubPermissions.Staffs.Edit, L("Permission:Staffs.Edit"));
        staffsPermission.AddChild(EHubPermissions.Staffs.Delete, L("Permission:Staffs.Delete"));


    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<EHubResource>(name);
    }
}
