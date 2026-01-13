import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    // Home / Dashboard
    {
      path: '/',
      name: '::Menu:Home',
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/dashboards',
      name: '::Menu:Dashboard',
      iconClass: 'fas fa-chart-line',
      order: 2,
      layout: eLayoutType.application,
    },

    {
      name: '::Menu:Students',
      iconClass: 'fas fa-user-graduate',
      order: 10,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StudentMenu',
    },
    {
      path: '/students',
      name: '::Menu:StudentsList',
      parentName: '::Menu:Students',
      iconClass: 'fas fa-list',
      order: 1,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StudentMenu.StudentList',
    },
    {
      path: '/student-attendance',
      name: '::Menu:StudentAttendance',
      parentName: '::Menu:Students',
      iconClass: 'fas fa-clipboard-check',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StudentMenu.StudentAttendance',
    },
    {
      path: '/student-attendance-insights',
      name: '::Menu:StudentAttendanceInsights',
      parentName: '::Menu:Students',
      iconClass: 'fas fa-chart-bar',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StudentMenu.StudentAttendanceInsights',
    },

    // -------------------------
    // Staff (Parent)
    // -------------------------
    {
      name: '::Menu:Staff',
      iconClass: 'fas fa-user-tie',
      order: 20,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StaffMenu',
    },
    {
      path: '/staffs',
      name: '::Menu:StaffList',
      parentName: '::Menu:Staff',
      iconClass: 'fas fa-list',
      order: 1,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StaffMenu.StaffList',
    },
    {
      path: '/staff-attendances',
      name: '::Menu:StaffAttendance',
      parentName: '::Menu:Staff',
      iconClass: 'fas fa-clipboard-check',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StaffMenu.StaffAttendance',
    },
    {
      path: '/staff-attendance-insights',
      name: '::Menu:StaffAttendanceInsights',
      parentName: '::Menu:Staff',
      iconClass: 'fas fa-chart-bar',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'EHub.StaffMenu.StaffAttendanceInsights',
    },
    // Example future routes:
    // {
    //   path: '/staff-attendance',
    //   name: '::Menu:StaffAttendance',
    //   parentName: '::Menu:Staff',
    //   iconClass: 'fas fa-clipboard-check',
    //   order: 2,
    //   layout: eLayoutType.application,
    // },

    // -------------------------
    // Academics (optional parent for subjects)
    // -------------------------
    {
      name: '::Menu:Academics',
      iconClass: 'fas fa-book',
      order: 30,
      layout: eLayoutType.application,
    },
    {
      path: '/subjects',
      name: '::Menu:Subjects',
      parentName: '::Menu:Academics',
      iconClass: 'fas fa-book-open',
      order: 1,
      layout: eLayoutType.application,
    },
  ]);
}
