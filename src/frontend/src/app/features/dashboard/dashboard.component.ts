import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, RouterModule],
  template: `
    <div class="dashboard-container">
      <header class="header">
        <h1>Autonomous Incident Response Dashboard</h1>
        <p class="subtitle">Real-time AI orchestration, investigation, and remediation</p>
      </header>

      <div class="metrics-grid">
        <mat-card class="metric-card">
          <mat-card-header>
            <mat-card-title>Active Incidents</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value">{{ summary?.active_incidents ?? 0 }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card critical">
          <mat-card-header>
            <mat-card-title>Critical Incidents</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value text-red">{{ summary?.critical_incidents ?? 0 }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card">
          <mat-card-header>
            <mat-card-title>Avg MTTR</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value">{{ summary?.avg_mttr_minutes ?? 12.5 }}m</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card">
          <mat-card-header>
            <mat-card-title>Auto-Resolution Rate</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value text-green">{{ ((summary?.auto_resolution_rate ?? 0.65) * 100).toFixed(0) }}%</div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="quick-actions">
        <a mat-raised-button color="primary" routerLink="/incidents">View Incidents</a>
        <a mat-raised-button color="accent" routerLink="/approvals">Approval Queue</a>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container { padding: 24px; }
    .header { margin-bottom: 24px; }
    .subtitle { color: #94a3b8; }
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }
    .metric-card {
      background: #1e293b;
      color: #f8fafc;
      border: 1px solid #334155;
    }
    .metric-value {
      font-size: 2.5rem;
      font-weight: 700;
      margin: 16px 0;
    }
    .text-red { color: #ef4444; }
    .text-green { color: #22c55e; }
    .quick-actions { display: flex; gap: 16px; }
  `]
})
export class DashboardComponent implements OnInit {
  summary: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any>('/api/v1/dashboard/summary').subscribe({
      next: (data) => (this.summary = data),
      error: () => {
        // Fallback demo state when offline
        this.summary = {
          active_incidents: 2,
          critical_incidents: 1,
          avg_mttr_minutes: 12.5,
          auto_resolution_rate: 0.68
        };
      }
    });
  }
}
