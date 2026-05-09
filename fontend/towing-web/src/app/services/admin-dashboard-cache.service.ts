import { Injectable } from '@angular/core';
import { AdminDashboardSummary } from './job.service';
import { PaymentStatistics } from './payment.service';
import { User } from '../models/user.model';

const STORAGE_KEY = 'towing.adminDashboard.cache.v1';

export interface AdminDashboardCacheEnvelope {
  version: 1;
  userId: string;
  dayKey: string;
  cachedAt: number;
  summary: AdminDashboardSummary;
  paymentStats: PaymentStatistics;
  drivers: User[];
}

@Injectable({ providedIn: 'root' })
export class AdminDashboardCacheService {
  todayDayKey(): string {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  readForUser(userId: string): AdminDashboardCacheEnvelope | null {
    if (!userId) return null;
    const day = this.todayDayKey();
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const data = JSON.parse(raw) as AdminDashboardCacheEnvelope;
      if (data?.version !== 1) return null;
      if (data.userId !== userId) return null;
      if (data.dayKey !== day) return null;
      if (!data.summary) return null;
      return data;
    } catch {
      return null;
    }
  }

  save(envelope: AdminDashboardCacheEnvelope): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(envelope));
    } catch (e) {
      console.warn('Admin dashboard cache write failed:', e);
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }
}
