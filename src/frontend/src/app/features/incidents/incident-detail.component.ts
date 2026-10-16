import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { IncidentHubService } from '../../core/services/incident-hub.service';

@Component({
  selector: 'app-incident-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="detail-container" *ngIf="incident">
      <div class="nav-bar">
        <a mat-button routerLink="/incidents">
          <mat-icon>arrow_back</mat-icon> Back to Incidents
        </a>
      </div>

      <header class="detail-header">
        <div class="title-row">
          <h1>{{ incident.title }}</h1>
          <span class="badge" [ngClass]="incident.severity">{{ incident.severity | uppercase }}</span>
        </div>
        <p class="meta">
          Status: <strong>{{ incident.status }}</strong> · 
          Triggered: {{ incident.triggeredAt | date:'medium' }} · 
          Source: {{ incident.source }}
        </p>
      </header>

      <div class="content-grid">
        <mat-card class="card rca-card" *ngIf="incident.rootCause">
          <mat-card-header>
            <mat-card-title>Root Cause Analysis</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>{{ incident.rootCause }}</p>
            <p *ngIf="incident.rootCauseConfidence">
              Confidence: <strong>{{ (incident.rootCauseConfidence * 100).toFixed(0) }}%</strong>
            </p>
          </mat-card-content>
        </mat-card>

        <mat-card class="card remediation-card" *ngIf="incident.proposedRemediation">
          <mat-card-header>
            <mat-card-title>Proposed Remediation</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>{{ incident.proposedRemediation }}</p>
            <p>Risk Level: <strong>{{ incident.remediationRiskLevel }}</strong></p>
          </mat-card-content>
        </mat-card>
      </div>

      <section class="timeline-section">
        <h2>Investigation Timeline & Audit Trail</h2>
        <div class="timeline">
          <div class="timeline-item" *ngFor="let evt of incident.events">
            <div class="timestamp">{{ evt.timestamp | date:'shortTime' }}</div>
            <div class="content">
              <h4>{{ evt.eventType }} <span *ngIf="evt.agentName">({{ evt.agentName }})</span></h4>
              <p>{{ evt.actionDescription }}</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .detail-container { padding: 24px; max-width: 1200px; margin: 0 auto; }
    .nav-bar { margin-bottom: 16px; }
    .title-row { display: flex; align-items: center; gap: 16px; }
    .badge { padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 0.85rem; }
    .badge.critical { background: #dc2626; color: #fff; }
    .badge.high { background: #ea580c; color: #fff; }
    .content-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; margin: 24px 0; }
    .card { background: #1e293b; color: #f8fafc; border: 1px solid #334155; }
    .timeline { border-left: 2px solid #38bdf8; margin-left: 16px; padding-left: 16px; }
    .timeline-item { margin-bottom: 16px; position: relative; }
    .timestamp { font-size: 0.8rem; color: #94a3b8; }
  `]
})
export class IncidentDetailComponent implements OnInit {
  incident: any = null;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private hubService: IncidentHubService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.http.get<any>(`/api/v1/incidents/${id}`).subscribe({
        next: (data) => {
          this.incident = data;
          this.hubService.joinIncident(id);
        },
        error: () => {
          this.incident = {
            id,
            title: 'High Error Rate in auth-api',
            severity: 'high',
            status: 'investigating',
            source: 'prometheus',
            triggeredAt: new Date(),
            rootCause: 'Database connection pool exhaustion',
            rootCauseConfidence: 0.88,
            proposedRemediation: 'Kill long running queries blocking connection pool',
            remediationRiskLevel: 'medium',
            events: [
              {
                eventType: 'incident_created',
                agentName: 'IncidentCoordinatorAgent',
                actionDescription: 'Incident triggered via webhook',
                timestamp: new Date()
              }
            ]
          };
        }
      });
    }
  }
}
