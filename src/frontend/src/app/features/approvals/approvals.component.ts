import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="approvals-container">
      <header class="header">
        <h1>Human-in-the-Loop Approval Queue</h1>
        <p class="subtitle">High-risk remediation actions requiring authorized review</p>
      </header>

      <div class="approval-list" *ngIf="pendingApprovals.length > 0; else noApprovals">
        <mat-card class="approval-card" *ngFor="let item of pendingApprovals">
          <mat-card-header>
            <mat-card-title>{{ item.action }}</mat-card-title>
            <mat-card-subtitle>Required Level: {{ item.approval_level | uppercase }} · Risk: {{ item.risk_level }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>Incident: {{ item.incident_id }}</p>
            <p>Requested: {{ item.created_at | date:'medium' }}</p>
          </mat-card-content>
          <mat-card-actions align="end">
            <button mat-raised-button color="primary" (click)="approve(item.id)">
              <mat-icon>check</mat-icon> Approve
            </button>
            <button mat-button color="warn" (click)="deny(item.id)">
              <mat-icon>close</mat-icon> Deny
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <ng-template #noApprovals>
        <div class="empty-state">
          <mat-icon class="check-icon">verified</mat-icon>
          <h3>No Pending Approvals</h3>
          <p>All remediation actions have been evaluated or processed.</p>
        </div>
      </ng-template>
    </div>
  `,
  styles: [`
    .approvals-container { padding: 24px; max-width: 900px; margin: 0 auto; }
    .approval-card { background: #1e293b; color: #f8fafc; border: 1px solid #334155; margin-bottom: 16px; }
    .empty-state { text-align: center; padding: 48px; color: #94a3b8; }
    .check-icon { font-size: 48px; width: 48px; height: 48px; color: #22c55e; margin-bottom: 16px; }
  `]
})
export class ApprovalsComponent implements OnInit {
  pendingApprovals: any[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchApprovals();
  }

  fetchApprovals(): void {
    this.http.get<any>('/api/v1/approvals/pending').subscribe({
      next: (res) => (this.pendingApprovals = res.items || []),
      error: () => (this.pendingApprovals = [])
    });
  }

  approve(id: string): void {
    this.http.post(`/api/v1/approvals/${id}/approve`, { notes: 'Approved via web UI' }).subscribe(() => {
      this.pendingApprovals = this.pendingApprovals.filter(a => a.id !== id);
    });
  }

  deny(id: string): void {
    this.http.post(`/api/v1/approvals/${id}/deny`, { reason: 'Denied via web UI' }).subscribe(() => {
      this.pendingApprovals = this.pendingApprovals.filter(a => a.id !== id);
    });
  }
}
