import { useState, useCallback, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { ApprovalRequest } from '@/api/types'
import { useAuth, type UserRole } from '@/auth/auth-context'
import { useRealtimeSubscription } from '@/realtime/realtime-context'

export interface RoleCheckResult {
  allowed: boolean
  reason?: string
}

/**
 * Checks whether a user with the given role has sufficient authorization to
 * approve or deny an approval request at the specified level (Brief §7.6).
 */
export function canUserActOnApproval(
  userRole: UserRole | undefined,
  requiredLevel: 'lead' | 'operator' | 'admin',
): RoleCheckResult {
  if (!userRole) {
    return {
      allowed: false,
      reason: 'Sign in required to act on approval requests.',
    }
  }

  if (userRole === 'Viewer') {
    return {
      allowed: false,
      reason: 'Viewer role has read-only access. Operator or Lead required.',
    }
  }

  if (userRole === 'Admin') {
    return { allowed: true }
  }

  // Operator role
  if (requiredLevel === 'operator') {
    return { allowed: true }
  }

  if (requiredLevel === 'lead') {
    return {
      allowed: false,
      reason: 'Requires SRE Lead or Admin role.',
    }
  }

  return {
    allowed: false,
    reason: 'Requires Admin role for high-impact infrastructure changes.',
  }
}

export type ApprovalFilterStatus = 'pending' | 'all' | 'history'

export interface UseApprovalsOptions {
  statusFilter?: ApprovalFilterStatus
  searchQuery?: string
}

export const APPROVALS_QUERY_KEY = ['approvals', 'pending'] as const

export function useApprovals(options: UseApprovalsOptions = {}) {
  const { statusFilter = 'pending', searchQuery = '' } = options
  const { user, token } = useAuth()
  const queryClient = useQueryClient()

  // Track items mutated during current session so they stay visible during animation
  const [sessionMutatedIds, setSessionMutatedIds] = useState<Set<string>>(() => new Set())

  // Track optimistic action errors with rollbacks
  const [actionError, setActionError] = useState<{
    approvalId: string
    action: 'approve' | 'deny'
    message: string
  } | null>(null)

  // Simulation flag for testing optimistic rollback paths
  const [simulateFailure, setSimulateFailure] = useState(false)

  // Fetch pending approvals from /v1/approvals/pending
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: APPROVALS_QUERY_KEY,
    queryFn: async () => {
      const response = await apiClient.getPendingApprovals(undefined, token)
      return response.approvals
    },
    staleTime: 10_000,
  })

  // Realtime SignalR listener: Invalidate on new ApprovalRequired event
  useRealtimeSubscription('ApprovalRequired', () => {
    void queryClient.invalidateQueries({ queryKey: APPROVALS_QUERY_KEY })
  })

  // Realtime SignalR listener: Invalidate on IncidentStatusChanged
  useRealtimeSubscription('IncidentStatusChanged', () => {
    void queryClient.invalidateQueries({ queryKey: APPROVALS_QUERY_KEY })
  })

  // Optimistic Approve Mutation
  const approveMutation = useMutation({
    mutationFn: async ({ approvalId, notes }: { approvalId: string; notes?: string }) => {
      if (simulateFailure) {
        throw new Error('Simulated network failure: Unable to reach approval gateway.')
      }
      return apiClient.approveRemediation(
        approvalId,
        {
          approved_by_user_id: user?.id || 'usr-anonymous',
          notes,
        },
        token,
      )
    },
    onMutate: async ({ approvalId }) => {
      setActionError(null)
      setSessionMutatedIds((prev) => new Set(prev).add(approvalId))
      // Cancel outgoing refetches so they don't overwrite optimistic update
      await queryClient.cancelQueries({ queryKey: APPROVALS_QUERY_KEY })

      // Snapshot previous approvals list for rollback
      const previousApprovals =
        queryClient.getQueryData<ApprovalRequest[]>(APPROVALS_QUERY_KEY) || []

      // Optimistically update: mark approval as approved
      queryClient.setQueryData<ApprovalRequest[]>(APPROVALS_QUERY_KEY, (old = []) =>
        old.map((item) =>
          item.id === approvalId ? { ...item, status: 'approved' as const } : item,
        ),
      )

      return { previousApprovals, approvalId }
    },
    onError: (err, variables, context) => {
      setSessionMutatedIds((prev) => {
        const next = new Set(prev)
        next.delete(variables.approvalId)
        return next
      })
      // Roll back to previous snapshot immediately
      if (context?.previousApprovals) {
        queryClient.setQueryData(APPROVALS_QUERY_KEY, context.previousApprovals)
      }
      const message = err instanceof Error ? err.message : 'Approval failed. Reverted to pending.'
      setActionError({
        approvalId: variables.approvalId,
        action: 'approve',
        message,
      })
    },
    onSettled: () => {
      // Allow the 300ms animation to complete before revalidating
      setTimeout(() => {
        void queryClient.invalidateQueries({ queryKey: APPROVALS_QUERY_KEY })
      }, 800)
    },
  })

  // Optimistic Deny Mutation
  const denyMutation = useMutation({
    mutationFn: async ({ approvalId, reason }: { approvalId: string; reason: string }) => {
      if (simulateFailure) {
        throw new Error('Simulated network failure: Unable to record denial.')
      }
      return apiClient.rejectRemediation(
        approvalId,
        {
          denied_by_user_id: user?.id || 'usr-anonymous',
          reason,
        },
        token,
      )
    },
    onMutate: async ({ approvalId }) => {
      setActionError(null)
      setSessionMutatedIds((prev) => new Set(prev).add(approvalId))
      await queryClient.cancelQueries({ queryKey: APPROVALS_QUERY_KEY })

      const previousApprovals =
        queryClient.getQueryData<ApprovalRequest[]>(APPROVALS_QUERY_KEY) || []

      // Optimistically update: mark approval as denied
      queryClient.setQueryData<ApprovalRequest[]>(APPROVALS_QUERY_KEY, (old = []) =>
        old.map((item) => (item.id === approvalId ? { ...item, status: 'denied' as const } : item)),
      )

      return { previousApprovals, approvalId }
    },
    onError: (err, variables, context) => {
      setSessionMutatedIds((prev) => {
        const next = new Set(prev)
        next.delete(variables.approvalId)
        return next
      })
      if (context?.previousApprovals) {
        queryClient.setQueryData(APPROVALS_QUERY_KEY, context.previousApprovals)
      }
      const message = err instanceof Error ? err.message : 'Denial failed. Reverted to pending.'
      setActionError({
        approvalId: variables.approvalId,
        action: 'deny',
        message,
      })
    },
    onSettled: () => {
      setTimeout(() => {
        void queryClient.invalidateQueries({ queryKey: APPROVALS_QUERY_KEY })
      }, 800)
    },
  })

  // Filter and sort items: Queue sorted by timeout, soonest first (Brief §7.6)
  const filteredAndSortedApprovals = useMemo(() => {
    let items = [...(data || [])]

    // Status filter
    if (statusFilter === 'pending') {
      items = items.filter((a) => a.status === 'pending' || sessionMutatedIds.has(a.id))
    } else if (statusFilter === 'history') {
      items = items.filter((a) => a.status !== 'pending')
    }

    // Search query filter
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase().trim()
      items = items.filter(
        (a) =>
          a.incident_id.toLowerCase().includes(q) ||
          a.action.toLowerCase().includes(q) ||
          a.command.toLowerCase().includes(q) ||
          (a.notes && a.notes.toLowerCase().includes(q)),
      )
    }

    // Sort by timeout_at, soonest first per Brief §7.6
    items.sort((a, b) => new Date(a.timeout_at).getTime() - new Date(b.timeout_at).getTime())

    return items
  }, [data, statusFilter, searchQuery, sessionMutatedIds])

  // Aggregate queue metrics
  const metrics = useMemo(() => {
    const all = data || []
    const pending = all.filter((a) => a.status === 'pending')
    const now = Date.now()
    const expiringSoon = pending.filter(
      (a) => new Date(a.timeout_at).getTime() - now <= 5 * 60 * 1000,
    )
    const highRisk = pending.filter((a) => a.risk_level === 'high')

    return {
      total: all.length,
      pendingCount: pending.length,
      expiringSoonCount: expiringSoon.length,
      highRiskCount: highRisk.length,
    }
  }, [data])

  const clearActionError = useCallback(() => {
    setActionError(null)
  }, [])

  return {
    approvals: filteredAndSortedApprovals,
    rawApprovals: data || [],
    metrics,
    isLoading,
    isError,
    error,
    refetch,
    actionError,
    clearActionError,
    simulateFailure,
    setSimulateFailure,
    approve: approveMutation.mutateAsync,
    deny: denyMutation.mutateAsync,
    isApproving: approveMutation.isPending,
    isDenying: denyMutation.isPending,
  }
}
