import { createAction, props } from '@ngrx/store';

export const loadIncidents = createAction('[Incidents] Load Incidents');
export const loadIncidentsSuccess = createAction(
  '[Incidents] Load Incidents Success',
  props<{ incidents: any[] }>()
);
export const loadIncidentsFailure = createAction(
  '[Incidents] Load Incidents Failure',
  props<{ error: string }>()
);

export const incidentCreatedRealtime = createAction(
  '[Incidents] Incident Created Realtime',
  props<{ incident: any }>()
);

export const incidentStatusChangedRealtime = createAction(
  '[Incidents] Incident Status Changed Realtime',
  props<{ incidentId: string; status: string }>()
);
