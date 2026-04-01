import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { initializeApp, type FirebaseApp } from 'firebase/app';
import { getMessaging, getToken, isSupported, onMessage, type Messaging } from 'firebase/messaging';
import { firstValueFrom } from 'rxjs';
import { firebaseWebConfig } from '../config/firebase-web.config';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { InAppNotificationsService } from './in-app-notifications.service';

/**
 * Registers the browser FCM token with the API only. All notification sends go through the backend (Firebase Admin).
 */
@Injectable({ providedIn: 'root' })
export class PushNotificationsService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly inAppNotifications = inject(InAppNotificationsService);

  private firebaseApp: FirebaseApp | null = null;
  private messaging: Messaging | null = null;
  private lastFcmToken: string | null = null;
  private foregroundListenerAttached = false;

  /** Call once from the app shell after DI is ready. */
  start(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.auth.currentUser$.subscribe((user) => {
      if (!user) {
        void this.onSignedOut();
        return;
      }
      void this.onSignedIn();
    });
  }

  private getOrCreateFirebaseApp(): FirebaseApp {
    if (!this.firebaseApp) {
      this.firebaseApp = initializeApp(firebaseWebConfig);
    }
    return this.firebaseApp;
  }

  private getOrCreateMessaging(): Messaging | null {
    try {
      if (!this.messaging) {
        this.messaging = getMessaging(this.getOrCreateFirebaseApp());
      }
      return this.messaging;
    } catch {
      return null;
    }
  }

  private async onSignedIn(): Promise<void> {
    try {
      if (!(await isSupported())) {
        return;
      }

      if (!('Notification' in window) || !('serviceWorker' in navigator)) {
        return;
      }

      if (Notification.permission === 'denied') {
        return;
      }

      if (Notification.permission === 'default') {
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') {
          return;
        }
      }

      const registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
      const messaging = this.getOrCreateMessaging();
      if (!messaging) {
        return;
      }

      const token = await getToken(messaging, {
        vapidKey: environment.firebase.vapidKey,
        serviceWorkerRegistration: registration
      });

      if (!token) {
        return;
      }

      this.lastFcmToken = token;
      await firstValueFrom(this.api.post<void>('notifications/fcm/register', { token }));

      if (!this.foregroundListenerAttached) {
        this.foregroundListenerAttached = true;
        onMessage(messaging, (payload) => {
          this.inAppNotifications.addFromFcmPayload(payload);
        });
      }
    } catch (err) {
      console.warn('[FCM] registration skipped', err);
    }
  }

  private async onSignedOut(): Promise<void> {
    const token = this.lastFcmToken;
    this.lastFcmToken = null;
    if (!token) {
      return;
    }
    try {
      await firstValueFrom(this.api.post<void>('notifications/fcm/unregister', { token }));
    } catch {
      // Best-effort: token may already be invalid
    }
  }
}
