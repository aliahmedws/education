import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { ImportStudentResultDto } from 'src/app/proxy/students/models';

@Injectable({ providedIn: 'root' })
export class StudentImportApi {
  constructor(private rest: RestService) {}

  importExcel(file: File): Observable<ImportStudentResultDto> {
    const formData = new FormData();
    formData.append('file', file, file.name); // MUST be "file"

    return this.rest.request<FormData, ImportStudentResultDto>(
      {
        method: 'POST',
        url: '/api/app/students/import-excel',
        body: formData,
      },
      { apiName: 'default' }
    );
  }
}
