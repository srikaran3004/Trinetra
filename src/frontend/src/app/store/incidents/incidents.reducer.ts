import { createReducer, on } from '@ngrx/store';
import * as IncidentActions from './incidents.actions';

export interface IncidentsState {
  items: any[];
  loading: boolean;
  error: string | null;
}

export const initialState: IncidentsState = {
  items: [],
  loading: false,
  error: null
};

export const incidentsReducer = createReducer(
  initialState,
  on(IncidentActions.loadIncidents, (state) => ({
    ...state,
    loading: true,
    error: null
  })),
  on(IncidentActions.loadIncidentsSuccess, (state, { incidents }) => ({
    ...state,
    items: incidents,
    loading: false
  })),
  on(IncidentActions.loadIncidentsFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error
  })),
  on(IncidentActions.incidentCreatedRealtime, (state, { incident }) => ({
    ...state,
    items: [incident, ...state.items]
  })),
  on(IncidentActions.incidentStatusChangedRealtime, (state, { incidentId, status }) => ({
    ...state,
    items: state.items.map(item => item.id === incidentId ? { ...item, status } : item)
  }))
);
