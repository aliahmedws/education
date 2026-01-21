import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import Chart from 'chart.js/auto';
import { CheckFeesDashboardDto, CheckFeesDashboardInput, StudentMonthlyFeeLineService } from 'src/app/proxy/fee-module/student-monthly-fee-lines';

@Component({
  selector: 'app-check-fees-dashboard',
  standalone: false,
  templateUrl: './check-fees-dashboard.component.html',
  styleUrls: ['./check-fees-dashboard.component.scss'],
})
export class CheckFeesDashboardComponent implements AfterViewInit, OnDestroy {
  private fb = inject(FormBuilder);
  private service = inject(StudentMonthlyFeeLineService);

  loading = false;
  data?: CheckFeesDashboardDto;

  @ViewChild('feeHeadBarCanvas', { static: true }) feeHeadBarCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('collectionDoughnutCanvas', { static: true }) collectionDoughnutCanvas!: ElementRef<HTMLCanvasElement>;

  private feeHeadBarChart?: Chart;
  private collectionDoughnutChart?: Chart;

  form = this.fb.group({
    month: [this.toMonthInputValue(new Date()), [Validators.required]], // store as "YYYY-MM"
    gradeLevel: [null],
    section: [null],
    shift: [null],
    term: [null],
    studentId: [null],
  });

  ngAfterViewInit(): void {
    this.initCharts();
    this.search();
  }

  ngOnDestroy(): void {
    this.feeHeadBarChart?.destroy();
    this.collectionDoughnutChart?.destroy();
  }

  search(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();

    // Convert "YYYY-MM" -> Date (month start), avoid timezone shifting
    const monthDate = this.parseMonthValue(v.month);

    const input: CheckFeesDashboardInput = {
      month: monthDate as any,
      gradeLevel: v.gradeLevel,
      section: v.section,
      shift: v.shift,
      term: v.term,
      studentId: v.studentId,
      skipCount: 0,
      maxResultCount: 50,
    } as any;

    this.loading = true;

    this.service.getDashboard(input)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          this.data = res;
          this.updateCharts(res);
        }
      });
  }

  clear(): void {
    this.form.reset({
      month: this.toMonthInputValue(new Date()),
      gradeLevel: null,
      section: null,
      shift: null,
      term: null,
      studentId: null,
    });
    this.search();
  }

  // ---------------- Charts ----------------

  private initCharts(): void {
    this.feeHeadBarChart?.destroy();
    this.collectionDoughnutChart?.destroy();

    // BAR: Fee heads
    this.feeHeadBarChart = new Chart(this.feeHeadBarCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels: [],
        datasets: [
          { label: 'Net', data: [] },
          { label: 'Paid', data: [] },
          { label: 'Pending', data: [] },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: true } },
        scales: { y: { beginAtZero: true } },
      },
    });

    // DOUGHNUT: Collected vs Pending
    this.collectionDoughnutChart = new Chart(this.collectionDoughnutCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Collected', 'Pending'],
        datasets: [{ data: [0, 0] }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: true } },
      },
    });
  }

  private updateCharts(res: CheckFeesDashboardDto): void {
    // Fee head bar: top 10 by pending
    const feeHeads = (res.byFeeHead ?? [])
      .slice()
      .sort((a, b) => Number(b.pending ?? 0) - Number(a.pending ?? 0))
      .slice(0, 10);

    const labels = feeHeads.map(x => (x.feeHeadName && x.feeHeadName.trim()) ? x.feeHeadName : '—');
    const net = feeHeads.map(x => Number(x.net ?? 0));
    const paid = feeHeads.map(x => Number(x.paid ?? 0));
    const pending = feeHeads.map(x => Number(x.pending ?? 0));

    if (this.feeHeadBarChart) {
      this.feeHeadBarChart.data.labels = labels;
      this.feeHeadBarChart.data.datasets[0].data = net;
      this.feeHeadBarChart.data.datasets[1].data = paid;
      this.feeHeadBarChart.data.datasets[2].data = pending;
      this.feeHeadBarChart.update();
    }

    // Doughnut
    const collected = Number(res.totalPaid ?? 0);
    const pend = Number(res.totalPending ?? 0);

    if (this.collectionDoughnutChart) {
      this.collectionDoughnutChart.data.datasets[0].data = [collected, pend];
      this.collectionDoughnutChart.update();
    }
  }

  // ---------------- Helpers ----------------

  private parseMonthValue(v: any): Date {
    // v is expected: "YYYY-MM"
    if (!v || typeof v !== 'string' || v.length < 7) {
      const now = new Date();
      return new Date(now.getFullYear(), now.getMonth(), 1);
    }
    const [yy, mm] = v.split('-').map(x => Number(x));
    return new Date(yy, (mm ?? 1) - 1, 1);
  }

  private toMonthInputValue(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    return `${year}-${month}`;
  }
}
