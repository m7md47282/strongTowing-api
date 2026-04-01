/* eslint-disable no-undef */
// Keep firebase.initializeApp() in sync with src/app/config/firebase-web.config.ts
importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: 'AIzaSyAkUPoixCf1a-ZPzjopb8TY8sAVgF96R68',
  authDomain: 'strongtowing-9905a.firebaseapp.com',
  projectId: 'strongtowing-9905a',
  storageBucket: 'strongtowing-9905a.firebasestorage.app',
  messagingSenderId: '993462038412',
  appId: '1:993462038412:web:2d8cfefaebf15aa8ab30a9',
  measurementId: 'G-DTYW5KKVJZ'
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
  const title = payload.notification?.title || 'Strong Towing';
  const options = {
    body: payload.notification?.body || '',
    icon: '/favicon.ico',
    data: payload.data || {}
  };
  self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
    clientList.forEach(function (client) {
      client.postMessage({ type: 'FCM_NOTIFICATION', payload: payload });
    });
  });
  return self.registration.showNotification(title, options);
});
