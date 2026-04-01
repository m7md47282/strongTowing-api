import { firebaseWebConfig, firebaseVapidPublicKey } from '../app/config/firebase-web.config';

export const environment = {
  production: false,
  // apiUrl: 'http://66.179.188.32:8080/api',
  apiUrl: 'https://api.strongtowing.net/api',
  // apiUrl: 'http://localhost:5155/api',
  appName: 'Strong Towing Services',
  version: '1.0.0',
  /** Browser-restricted Maps JavaScript key; set locally or via CI — do not commit production secrets */
  mapsApiKey: 'AIzaSyAYdqFdOkq5GUbbe_c3ZNOV-RtjvK7lXA8',
  firebase: {
    ...firebaseWebConfig,
    vapidKey: firebaseVapidPublicKey
  }
};
