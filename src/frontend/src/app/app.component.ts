import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <div class="app-shell">
      <mat-toolbar color="primary" class="main-toolbar">
        <div class="brand">
          <mat-icon class="brand-icon">visibility</mat-icon>
          <span class="brand-name">Trinetra</span>
        </div>
        <span class="spacer"></span>
        <nav class="nav-links">
          <a mat-button routerLink="/dashboard" routerLinkActive="active-link">Dashboard</a>
          <a mat-button routerLink="/incidents" routerLinkActive="active-link">Incidents</a>
          <a mat-button routerLink="/approvals" routerLinkActive="active-link">Approvals</a>
        </nav>
      </mat-toolbar>

      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-shell { display: flex; flex-direction: column; min-height: 100vh; }
    .main-toolbar {
      background: #0f172a;
      border-bottom: 1px solid #1e293b;
      position: sticky;
      top: 0;
      z-index: 1000;
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 700; font-size: 1.25rem; }
    .brand-icon { color: #38bdf8; }
    .spacer { flex: 1 1 auto; }
    .nav-links a { color: #94a3b8; font-weight: 500; }
    .nav-links a.active-link { color: #38bdf8; font-weight: 700; }
    .main-content { flex: 1; }
  `]
})
export class AppComponent {
  title = 'Trinetra';
}
