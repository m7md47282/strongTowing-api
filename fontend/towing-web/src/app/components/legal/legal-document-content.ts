export type LegalPageId =
  | 'sms-consent'
  | 'privacy-policy'
  | 'terms-of-service'
  | 'cancellation-policy';

export interface LegalSection {
  heading: string;
  paragraphs?: string[];
  bullets?: string[];
}

export interface LegalDocumentContent {
  pageTitle: string;
  documentTitle: string;
  metaDescription: string;
  sections: LegalSection[];
}

export const LEGAL_DOCUMENTS: Record<LegalPageId, LegalDocumentContent> = {
  'sms-consent': {
    pageTitle: 'SMS Consent Policy | Strong Towing',
    documentTitle: 'SMS Consent Policy',
    metaDescription:
      'SMS consent policy for Strong Towing towing and roadside assistance services.',
    sections: [
      {
        heading: '',
        paragraphs: [
          'By requesting towing or roadside assistance services from Strong Towing, you agree to receive SMS messages related to your service request.',
          'These messages may include:',
        ],
        bullets: [
          'Driver ETA',
          'Service updates',
          'Job status notifications',
          'Follow-up messages for customer feedback',
        ],
      },
      {
        heading: '',
        paragraphs: [
          'Consent is obtained verbally during phone calls with our dispatch team.',
          'Message frequency may vary.',
          'You can opt out at any time by replying STOP.',
          'For assistance, call +17032008836.',
          'We do not send marketing or promotional messages.',
        ],
      },
    ],
  },
  'privacy-policy': {
    pageTitle: 'Privacy Policy | Strong Towing',
    documentTitle: 'Privacy Policy – Strong Towing',
    metaDescription:
      'Privacy policy for Strong Towing towing and roadside assistance services.',
    sections: [
      {
        heading: '',
        paragraphs: [
          'Strong Towing respects your privacy and is committed to protecting your personal information.',
        ],
      },
      {
        heading: 'Information We Collect:',
        bullets: ['Name', 'Phone number', 'Service location', 'Vehicle details'],
      },
      {
        heading: 'How We Use Information:',
        bullets: [
          'To provide towing and roadside services',
          'To communicate service updates and ETAs',
          'To improve our services',
        ],
      },
      {
        heading: 'SMS Communication:',
        paragraphs: [
          'By using our services, you may receive SMS messages related to your request. You can opt out at any time by replying STOP.',
        ],
      },
      {
        heading: 'Data Protection:',
        paragraphs: [
          'We do not sell or share your personal information with third parties for marketing purposes.',
        ],
      },
      {
        heading: 'Third-Party Sharing:',
        paragraphs: [
          'Information may be shared only when necessary to complete the service (dispatch systems or service partners).',
        ],
      },
      {
        heading: 'Security:',
        paragraphs: ['We take reasonable measures to protect your information.'],
      },
      {
        heading: 'Contact:',
        paragraphs: ['info@strongtowing.services', '+17032008836'],
      },
    ],
  },
  'terms-of-service': {
    pageTitle: 'Terms of Service | Strong Towing',
    documentTitle: 'Terms of Service – Strong Towing',
    metaDescription:
      'Terms of service for Strong Towing towing and roadside assistance.',
    sections: [
      {
        heading: '',
        paragraphs: ['By requesting our services, you agree to the following terms:'],
      },
      {
        heading: 'Services:',
        paragraphs: [
          'Strong Towing provides towing and roadside assistance services.',
        ],
      },
      {
        heading: 'Service Conditions:',
        bullets: [
          'Service availability may vary',
          'ETA is an estimate and not guaranteed',
        ],
      },
      {
        heading: 'Payment:',
        paragraphs: ['Payment is due upon completion unless otherwise agreed.'],
      },
      {
        heading: 'Customer Responsibility:',
        paragraphs: [
          'Customers must provide accurate information and ensure safe access to the vehicle.',
        ],
      },
      {
        heading: 'Liability:',
        paragraphs: [
          'Strong Towing is not responsible for pre-existing damage or mechanical issues.',
        ],
      },
      {
        heading: 'SMS Communication:',
        paragraphs: [
          'You agree to receive service-related messages. Reply STOP to opt out.',
        ],
      },
    ],
  },
  'cancellation-policy': {
    pageTitle: 'Cancellation Policy | Strong Towing',
    documentTitle: 'Cancellation Policy – Strong Towing',
    metaDescription:
      'Cancellation policy for Strong Towing towing and roadside services.',
    sections: [
      {
        heading: '',
        paragraphs: [
          'If a service is canceled after dispatch, a cancellation fee may apply.',
        ],
      },
      {
        heading: 'Cancellation Fees:',
        bullets: [
          'If driver is en route: up to 30% of service cost',
          'If driver has arrived: full or partial charge may apply',
        ],
      },
      {
        heading: 'Delays:',
        paragraphs: [
          'We are not responsible for delays caused by traffic, weather, or unforeseen issues.',
        ],
      },
      {
        heading: 'No-Show:',
        paragraphs: [
          'If the customer is not present at the service location, charges may still apply.',
        ],
      },
    ],
  },
};
