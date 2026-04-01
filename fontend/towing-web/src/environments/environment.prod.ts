import { firebaseWebConfig, firebaseVapidPublicKey } from '../app/config/firebase-web.config';

export const environment = {
  production: true,
  apiUrl: 'https://api.strongtowing.net/api',
  appName: 'Strong Towing Services',
  version: '1.0.0',
  mapsApiKey: 'AIzaSyAYdqFdOkq5GUbbe_c3ZNOV-RtjvK7lXA8',
  firebase: {
    ...firebaseWebConfig,
    vapidKey: firebaseVapidPublicKey
  }
};
