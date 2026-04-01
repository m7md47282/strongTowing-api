import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { AuthService } from './auth.service';

const MAX_ITEMS = 50;

export interface InAppNotification {
  id: string;
  title: string;
  body: string;
  data?: Record<string, string>;
  read: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class InAppNotificationsService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly auth = inject(AuthService);

  private readonly notificationsSubject = new BehaviorSubject<InAppNotification[]>([]);

  readonly notifications$ = this.notificationsSubject.asObservable();
  readonly vm$ = this.notifications$.pipe(
    map((notifications) => ({
      notifications,
      unread: notifications.filter((n) => !n.read).length
    }))
  );

  private swListenerAttached = false;

  /** Call once from app shell (e.g. AppComponent). */
  start(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.auth.currentUser$.subscribe((user) => {
      if (!user) {
        this.notificationsSubject.next([]);
        return;
      }
      this.loadForUser(user.id);
    });

    if (!this.swListenerAttached) {
      this.swListenerAttached = true;
      navigator.serviceWorker.addEventListener('message', (event: MessageEvent) => {
        if (event.data?.type === 'FCM_NOTIFICATION') {
          this.addFromFcmPayload(event.data.payload);
        }
      });
    }
  }

  addFromFcmPayload(payload: unknown): void {
    if (!payload || typeof payload !== 'object') {
      return;
    }

    const p = payload as { notification?: { title?: string; body?: string }; data?: Record<string, string> };
    const n = p.notification;
    const dataRaw = p.data;

    const title = n?.title?.trim() || 'Notification';
    const body = n?.body?.trim() || '';
    const data = dataRaw ? { ...dataRaw } : undefined;

    const item: InAppNotification = {
      id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`,
      title,
      body,
      data,
      read: false,
      createdAt: new Date().toISOString()
    };

    const next = [item, ...this.notificationsSubject.value].slice(0, MAX_ITEMS);
    this.notificationsSubject.next(next);
    this.persist();
  }

  markRead(id: string): void {
    const list = this.notificationsSubject.value.map((n) =>
      n.id === id ? { ...n, read: true } : n
    );
    this.notificationsSubject.next(list);
    this.persist();
  }

  markAllRead(): void {
    const list = this.notificationsSubject.value.map((n) => ({ ...n, read: true }));
    this.notificationsSubject.next(list);
    this.persist();
  }

  private loadForUser(userId: string): void {
    try {
      const raw = localStorage.getItem(storageKey(userId));
      const parsed = raw ? (JSON.parse(raw) as InAppNotification[]) : [];
      this.notificationsSubject.next(Array.isArray(parsed) ? parsed : []);
    } catch {
      this.notificationsSubject.next([]);
    }
  }

  private persist(): void {
    const user = this.auth.getCurrentUser();
    if (!user) {
      return;
    }
    try {
      localStorage.setItem(storageKey(user.id), JSON.stringify(this.notificationsSubject.value));
    } catch {
      // ignore quota / private mode
    }
  }
}

function storageKey(userId: string): string {
  return `stongTowing_inAppNotifications_${userId}`;
}
