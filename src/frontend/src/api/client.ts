/**
 * Trinetra Typed REST API Client
 *
 * Enforces Zod boundary parsing on every network response per Brief §10.
 */
import {
  type IncidentListResponse,
  IncidentListResponseSchema,
  type IncidentDetail,
  IncidentDetailSchema,
  type CreateIncidentPayload,
  type CreateIncidentResponse,
  CreateIncidentResponseSchema,
  type IncidentEventsResponse,
  IncidentEventsResponseSchema,
  type IncidentRemediationResponse,
  IncidentRemediationResponseSchema,
  type PendingApprovalsResponse,
  PendingApprovalsResponseSchema,
  type ApprovePayload,
  type ApproveResponse,
  ApproveResponseSchema,
  type RejectPayload,
  type RejectResponse,
  RejectResponseSchema,
  type AgentsResponse,
  AgentsResponseSchema,
  type RunbooksResponse,
  RunbooksResponseSchema,
  type Runbook,
  RunbookSchema,
  type DashboardMetrics,
  DashboardMetricsSchema,
  ApiErrorResponseSchema,
} from './types'

export class ApiClientError extends Error {
  readonly status: number
  readonly code: string
  readonly requestId?: string

  constructor(message: string, status: number, code: string, requestId?: string) {
    super(message)
    this.name = 'ApiClientError'
    this.status = status
    this.code = code
    this.requestId = requestId
  }
}

export interface RequestOptions extends RequestInit {
  token?: string | null
  params?: Record<string, string | number | boolean | string[] | undefined>
}

export class ApiClient {
  private baseUrl: string

  constructor(baseUrl: string = import.meta.env.VITE_API_BASE_URL || '/v1') {
    this.baseUrl = baseUrl.replace(/\/$/, '')
  }

  private buildUrl(
    path: string,
    params?: Record<string, string | number | boolean | string[] | undefined>,
  ): string {
    const cleanPath = path.startsWith('/') ? path : `/${path}`
    const url = new URL(`${this.baseUrl}${cleanPath}`, window.location.origin)

    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined) {
          if (Array.isArray(value)) {
            url.searchParams.append(key, value.join(','))
          } else {
            url.searchParams.append(key, String(value))
          }
        }
      })
    }

    return url.pathname + url.search
  }

  private async request<T>(
    path: string,
    schema: { parse: (data: unknown) => T },
    options: RequestOptions = {},
  ): Promise<T> {
    const { token, params, headers: customHeaders, ...fetchOptions } = options
    const url = this.buildUrl(path, params)

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...(customHeaders as Record<string, string>),
    }

    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }

    const response = await fetch(url, {
      ...fetchOptions,
      headers,
    })

    const text = await response.text()
    let data: unknown
    try {
      data = text ? JSON.parse(text) : {}
    } catch {
      data = { raw: text }
    }

    if (!response.ok) {
      // Try to parse standard Doc 04 error schema
      const errorParsed = ApiErrorResponseSchema.safeParse(data)
      if (errorParsed.success) {
        throw new ApiClientError(
          errorParsed.data.error.message,
          errorParsed.data.error.status || response.status,
          errorParsed.data.error.code,
          errorParsed.data.error.request_id,
        )
      }

      throw new ApiClientError(
        typeof data === 'object' && data && 'message' in data
          ? String(data.message)
          : response.statusText || 'An unexpected API error occurred',
        response.status,
        'HTTP_ERROR',
      )
    }

    try {
      return schema.parse(data)
    } catch (parseError) {
      throw new ApiClientError(
        `API schema validation failed for ${path}: ${parseError instanceof Error ? parseError.message : String(parseError)}`,
        500,
        'SCHEMA_VALIDATION_ERROR',
      )
    }
  }

  // Incidents
  async getIncidents(
    params?: {
      status?: string[]
      severity?: string[]
      limit?: number
      offset?: number
      sort_by?: string
    },
    token?: string | null,
  ): Promise<IncidentListResponse> {
    return this.request('/incidents', IncidentListResponseSchema, {
      method: 'GET',
      params,
      token,
    })
  }

  async getIncidentById(id: string, token?: string | null): Promise<IncidentDetail> {
    return this.request(`/incidents/${encodeURIComponent(id)}`, IncidentDetailSchema, {
      method: 'GET',
      token,
    })
  }

  async createIncident(
    payload: CreateIncidentPayload,
    token?: string | null,
  ): Promise<CreateIncidentResponse> {
    return this.request('/incidents', CreateIncidentResponseSchema, {
      method: 'POST',
      body: JSON.stringify(payload),
      token,
    })
  }

  async getIncidentEvents(
    id: string,
    params?: {
      event_type?: string
      limit?: number
      offset?: number
    },
    token?: string | null,
  ): Promise<IncidentEventsResponse> {
    return this.request(
      `/incidents/${encodeURIComponent(id)}/events`,
      IncidentEventsResponseSchema,
      {
        method: 'GET',
        params,
        token,
      },
    )
  }

  async getIncidentRemediation(
    id: string,
    token?: string | null,
  ): Promise<IncidentRemediationResponse> {
    return this.request(
      `/incidents/${encodeURIComponent(id)}/remediation`,
      IncidentRemediationResponseSchema,
      {
        method: 'GET',
        token,
      },
    )
  }

  // Approvals
  async getPendingApprovals(
    params?: { limit?: number; sort_by?: string },
    token?: string | null,
  ): Promise<PendingApprovalsResponse> {
    return this.request('/approvals/pending', PendingApprovalsResponseSchema, {
      method: 'GET',
      params,
      token,
    })
  }

  async approveRemediation(
    approvalId: string,
    payload: ApprovePayload,
    token?: string | null,
  ): Promise<ApproveResponse> {
    return this.request(
      `/approvals/${encodeURIComponent(approvalId)}/approve`,
      ApproveResponseSchema,
      {
        method: 'POST',
        body: JSON.stringify(payload),
        token,
      },
    )
  }

  async rejectRemediation(
    approvalId: string,
    payload: RejectPayload,
    token?: string | null,
  ): Promise<RejectResponse> {
    return this.request(
      `/approvals/${encodeURIComponent(approvalId)}/reject`,
      RejectResponseSchema,
      {
        method: 'POST',
        body: JSON.stringify(payload),
        token,
      },
    )
  }

  // Agents
  async getAgents(token?: string | null): Promise<AgentsResponse> {
    return this.request('/agents', AgentsResponseSchema, {
      method: 'GET',
      token,
    })
  }

  // Runbooks
  async getRunbooks(
    params?: {
      tags?: string[]
      services?: string[]
      search?: string
      limit?: number
      offset?: number
    },
    token?: string | null,
  ): Promise<RunbooksResponse> {
    return this.request('/runbooks', RunbooksResponseSchema, {
      method: 'GET',
      params,
      token,
    })
  }

  async getRunbookById(id: string, token?: string | null): Promise<Runbook> {
    return this.request(`/runbooks/${encodeURIComponent(id)}`, RunbookSchema, {
      method: 'GET',
      token,
    })
  }

  // Dashboard & Metrics
  async getDashboardMetrics(token?: string | null): Promise<DashboardMetrics> {
    return this.request('/dashboard/metrics', DashboardMetricsSchema, {
      method: 'GET',
      token,
    })
  }
}

export const apiClient = new ApiClient()
