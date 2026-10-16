import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { IncidentHubService } from '../../core/services/incident-hub.service';

@Component({
  selector: 'app-incidents-list',
  standalone: true,
  imports: [CommonModule, RouterModule, MatTableModule, MatButtonModule, MatChipsModule],
  template: `
    <div class="incidents-container">
      <header class="header">
        <h1>Incidents</h1>
        <p class="subtitle">Monitored services and autonomous investigations</p>
      </header>

      <table mat-table [dataSource]="incidents" class="mat-elevation-z2 incident-table">
        <ng-container matColumnDef="severity">
          <th mat-header-cell *matHeaderCellDef> Severity </th>
          <td mat-cell *matCellDef="let inc">
            <span class="badge" [ngClass]="inc.severity">{{ inc.severity | uppercase }}</span>
          </td>
        </ng-container>

        <ng-container matColumnDef="title">
          <th mat-header-cell *matHeaderCellDef> Title </th>
          <td mat-cell *matCellDef="let inc">
            <a [routerLink]="['/incidents', inc.id]" class="title-link">{{ inc.title }}</a>
          </td>
        </ng-container>

        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef> Status </th>
          <td mat-cell *matCellDef="let inc"> {{ inc.status }} </td>
        </ng-container>

        <ng-container matColumnDef="triggered_at">
          <th mat-header-cell *matHeaderCellDef> Triggered </th>
          <td mat-cell *matCellDef="let inc"> {{ inc.triggeredAt | date:'short' }} </td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef> Actions </th>
          <td mat-cell *matCellDef="let inc">
            <a mat-button color="primary" [routerLink]="['/incidents', inc.id]">Investigate</a>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>
    </div>
  `,
  styles: [`
    .incidents-container { padding: 24px; }
    .incident-table { width: 100%; background: #1e293b; color: #f8fafc; border-radius: 8px; overflow: hidden; }
    .badge { padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 0.75rem; }
    .badge.critical { background: #dc2626; color: #fff; }
    .badge.high { background: #ea580c; color: #fff; }
    .badge.medium { background: #d97706; color: #fff; }
    .badge.low { background: #2563eb; color: #fff; }
    .title-link { color: #38bdf8; text-decoration: none; font-weight: 500; }
    .title-link:hover { text-decoration: underline; }
  `]
})
export class IncidentsListComponent implements OnInit {
  displayedColumns = ['severity', 'title', 'status', 'triggered_at', 'actions'];
  incidents: any[] = [];

  constructor(
    private http: HttpClient,
    private hubService: IncidentHubService
  ) {}

  ngOnInit(): void {
    this.fetchIncidents();

    // Listen for real-time incidents from SignalR
    this.hubService.incidentCreated$.subscribe((newIncident) => {
      this.incidents = [newIncident, ...this.incidents];
    });

    this.hubService.incidentStatusChanged$.subscribe((change) => {
      this.incidents = this.incidents.map(i => i.id === change.incidentId ? { ...i, status: change.status } : i);
    });
  }

  fetchIncidents(): void {
    this.http.get<any>('/api/v1/incidents?limit=50').subscribe({
      next: (res) => (this.incidents = res.items || []),
      error: () => {
        this.incidents = [
          {
            id: 'mock-1',
            severity: 'critical',
            title: 'High Error Rate in auth-api',
            status: 'investigating',
            triggeredAt: new Date()
          }
        ];
      }
    });
  }
}
