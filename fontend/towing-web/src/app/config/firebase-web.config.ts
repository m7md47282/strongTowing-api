/**
 * Shared Firebase web app config (client + service worker must stay aligned).
 * Push delivery is only triggered by the API via Firebase Admin — the browser only registers FCM tokens.
 */
export const firebaseWebConfig = {
  apiKey: 'AIzaSyAkUPoixCf1a-ZPzjopb8TY8sAVgF96R68',
  authDomain: 'strongtowing-9905a.firebaseapp.com',
  projectId: 'strongtowing-9905a',
  storageBucket: 'strongtowing-9905a.firebasestorage.app',
  messagingSenderId: '993462038412',
  appId: '1:993462038412:web:2d8cfefaebf15aa8ab30a9',
  measurementId: 'G-DTYW5KKVJZ'
} as const;

/** Web Push / FCM VAPID public key (safe to ship in the client). */
export const firebaseVapidPublicKey =
  'BMt53vuSQzOa7pt9ui2AoY72QDp9593g2putjvtM6c5nZN1glhXM_0wvFOk0UrvkENIH3A63KUwihdkLMAKn7lI';
