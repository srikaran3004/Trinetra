import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class IncidentHubService {
  private hubConnection?: signalR.HubConnection;

  private incidentCreatedSubject = new Subject<any>();
  private statusChangedSubject = new Subject<any>();
  private approvalRequiredSubject = new Subject<any>();

  public incidentCreated$: Observable<any> = this.incidentCreatedSubject.asObservable();
  public incidentStatusChanged$: Observable<any> = this.statusChangedSubject.asObservable();
  public approvalRequired$: Observable<any> = this.approvalRequiredSubject.asObservable();

  constructor() {
    this.startConnection();
  }

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/incidents')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('Connected to Trinetra SignalR IncidentHub'))
      .catch(err => console.warn('SignalR connection failed (backend may be starting):', err));

    this.registerEventHandlers();
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('IncidentCreated', (data) => {
      this.incidentCreatedSubject.next(data);
    });

    this.hubConnection.on('IncidentStatusChanged', (data) => {
      this.statusChangedSubject.next(data);
    });

    this.hubConnection.on('ApprovalRequired', (data) => {
      this.approvalRequiredSubject.next(data);
    });
  }

  public joinIncident(incidentId: string): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('JoinIncidentGroup', incidentId);
    }
  }

  public leaveIncident(incidentId: string): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('LeaveIncidentGroup', incidentId);
    }
  }
}
