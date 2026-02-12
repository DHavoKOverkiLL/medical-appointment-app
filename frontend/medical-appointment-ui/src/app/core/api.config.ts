const LOCAL_API_BASE_URL = 'https://localhost:7074';
const PROD_API_BASE_URL = 'https://medio-api.greenriver-343eb6db.westeurope.azurecontainerapps.io';

const hostname = typeof window === 'undefined' ? '' : window.location.hostname.toLowerCase();
const isLocalHost = hostname === 'localhost' || hostname === '127.0.0.1';

export const API_BASE_URL = isLocalHost ? LOCAL_API_BASE_URL : PROD_API_BASE_URL;
