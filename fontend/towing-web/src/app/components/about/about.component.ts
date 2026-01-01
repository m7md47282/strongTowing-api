import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface TeamMember {
  name: string;
  position: string;
  experience: string;
  image: string;
  skills: string[];
}

interface Certification {
  name: string;
  description: string;
  icon: string;
  year: string;
}

interface FleetFeature {
  title: string;
  description: string;
  icon: string;
}

interface FleetVehicle {
  name: string;
  description: string;
  image: string;
}

interface CommunityInitiative {
  title: string;
  description: string;
  image: string;
  year: string;
}

interface PhotoGallery {
  image: string;
  alt: string;
}

@Component({
  selector: 'app-about',
  imports: [CommonModule, RouterModule],
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit {
  teamMembers: TeamMember[] = [
    {
      name: 'Basem Jamaahna',
      position: 'Chief Executive Officer (CEO)',
      experience: '',
      image: '/images/team/default-avatar.png',
      skills: ['Leadership', 'Strategic Planning', 'Business Development']
    },
    {
      name: 'Adam Anderson',
      position: 'Dispatch Manager',
      experience: '',
      image: '/images/team/default-avatar.png',
      skills: ['Fleet Management', 'Operations', 'Coordination']
    },
    {
      name: 'Susan McCloney',
      position: 'Billing Specialist',
      experience: '',
      image: '/images/team/default-avatar.png',
      skills: ['Accounting', 'Insurance Claims', 'Customer Relations']
    },
    {
      name: 'Akram Al-Ansari',
      position: 'Sales Manager',
      experience: '',
      image: '/images/team/default-avatar.png',
      skills: ['Business Development', 'Client Relations', 'Sales Strategy']
    }
  ];

  certifications: Certification[] = [
    // General / National Certifications
    {
      name: 'TROCP Level 1 – Light-Duty',
      description: 'Towing Recovery Operator Certification Program - Light-Duty Tow Operator',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'TROCP Level 2 – Medium-Duty',
      description: 'Towing Recovery Operator Certification Program - Medium-Duty Tow Operator',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'TROCP Level 3 – Heavy Recovery',
      description: 'Towing Recovery Operator Certification Program - Heavy Recovery Specialist',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'TRSCP Certified',
      description: 'Towing & Recovery Support Certification Program',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'WreckMaster Certification',
      description: 'Professional recovery and towing certification',
      icon: 'fas fa-truck',
      year: 'Current'
    },
    {
      name: 'Traffic Incident Management',
      description: 'TIM Certificate for traffic safety and management',
      icon: 'fas fa-traffic-light',
      year: 'Current'
    },
    {
      name: 'Commercial Driver License',
      description: 'CDL - Commercial Drivers License certification',
      icon: 'fas fa-id-card',
      year: 'Current'
    },
    {
      name: 'HAZMAT Endorsement',
      description: 'Hazardous Materials handling certification',
      icon: 'fas fa-exclamation-triangle',
      year: 'Current'
    },
    {
      name: 'First Aid Certification',
      description: 'Certified in first aid and emergency response',
      icon: 'fas fa-first-aid',
      year: 'Current'
    },
    {
      name: 'CPR Certification',
      description: 'Cardiopulmonary Resuscitation certified',
      icon: 'fas fa-heartbeat',
      year: 'Current'
    },
    {
      name: 'OSHA Safety Certified',
      description: 'Occupational Safety and Health Administration certification',
      icon: 'fas fa-hard-hat',
      year: 'Current'
    },
    {
      name: 'HAZWOPER Certified',
      description: 'Hazardous Waste Operations and Emergency Response certified',
      icon: 'fas fa-shield-alt',
      year: 'Current'
    },
    {
      name: 'Insurance Certificate',
      description: 'Fully insured and licensed for all operations',
      icon: 'fas fa-file-contract',
      year: 'Current'
    },
    // Virginia (VA) Certifications
    {
      name: 'VA DCJS Tow Operator',
      description: 'Virginia Department of Criminal Justice Services certification',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'VA Tow Truck Driver',
      description: 'Virginia Tow Truck Driver Registration',
      icon: 'fas fa-user-check',
      year: 'Current'
    },
    {
      name: 'VA Tow Truck Registration',
      description: 'Virginia DMV Tow Truck Vehicle Registration',
      icon: 'fas fa-car',
      year: 'Current'
    },
    {
      name: 'VASTIM Training',
      description: 'Virginia Traffic Incident Management training certified',
      icon: 'fas fa-road',
      year: 'Current'
    },
    {
      name: 'VA Tow Laws Compliance',
      description: 'Virginia Tow Laws Compliance Certificate',
      icon: 'fas fa-gavel',
      year: 'Current'
    },
    // Maryland (MD) Certifications
    {
      name: 'MD County Tow License',
      description: 'Maryland County Tow License certified',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'MD Tow Business License',
      description: 'Maryland Tow Truck Business License',
      icon: 'fas fa-building',
      year: 'Current'
    },
    {
      name: 'MDTA Tow Permit',
      description: 'Maryland Transportation Authority tow permit',
      icon: 'fas fa-id-badge',
      year: 'Current'
    },
    // District of Columbia (DC) Certifications
    {
      name: 'DC Tow Business License',
      description: 'DC Tow Truck Business License from DCRA',
      icon: 'fas fa-certificate',
      year: 'Current'
    },
    {
      name: 'DC Tow Operator License',
      description: 'DC Tow Truck Operator License',
      icon: 'fas fa-user-tie',
      year: 'Current'
    },
    {
      name: 'DC Towing Control Number',
      description: 'DC Towing Control Number Registration',
      icon: 'fas fa-barcode',
      year: 'Current'
    }
  ];

  fleetFeatures: FleetFeature[] = [
    {
      title: 'Modern Fleet',
      description: 'State-of-the-art towing vehicles equipped with the latest technology',
      icon: 'fas fa-truck'
    },
    {
      title: 'Safety Equipment',
      description: 'Comprehensive safety gear and emergency response equipment',
      icon: 'fas fa-hard-hat'
    },
    {
      title: 'GPS Tracking',
      description: 'Real-time vehicle tracking for accurate arrival times',
      icon: 'fas fa-map-marker-alt'
    },
    {
      title: '24/7 Monitoring',
      description: 'Round-the-clock fleet monitoring and maintenance',
      icon: 'fas fa-clock'
    }
  ];

  fleetVehicles: FleetVehicle[] = [
    {
      name: 'Light Duty Tow Trucks',
      description: 'Perfect for cars, SUVs, and small trucks',
      image: '/images/photage/photage-1.jpeg'
    },
    {
      name: 'Heavy Duty Tow Trucks',
      description: 'Capable of handling large trucks and commercial vehicles',
      image: '/images/photage/photage-3.jpeg'
    },
    {
      name: 'Flatbed Trucks',
      description: 'Ideal for luxury vehicles and motorcycles',
      image: '/images/photage/photage-4.jpeg'
    },
    {
      name: 'Recovery Vehicles',
      description: 'Specialized equipment for accident recovery',
      image: '/images/photage/photage-5.jpeg'
    }
  ];

  communityInitiatives: CommunityInitiative[] = [
    {
      title: 'Local Charity Support',
      description: 'Annual donations and volunteer work with local charities and food banks',
      image: '/images/photage/photage-10.jpeg',
      year: '2024'
    },
    {
      title: 'Driver Safety Education',
      description: 'Free workshops on vehicle safety and emergency preparedness',
      image: '/images/photage/photage-11.jpeg',
      year: '2023'
    },
    {
      title: 'Youth Sports Sponsorship',
      description: 'Supporting local youth sports teams and community events',
      image: '/images/photage/photage-12.jpeg',
      year: '2024'
    },
    {
      title: 'Environmental Initiatives',
      description: 'Eco-friendly practices and community clean-up programs',
      image: '/images/photage/photage-13.jpeg',
      year: '2023'
    }
  ];

  photoGallery: PhotoGallery[] = [
    { image: "/images/photage/photage-14.jpeg", alt: "Strong Towing Team in Action" },
    { image: "/images/photage/photage-15.jpeg", alt: "Our Fleet on the Road" },
    { image: "/images/photage/photage-16.jpeg", alt: "Professional Service" },
    { image: "/images/photage/photage-17.jpeg", alt: "Emergency Response" },
    { image: "/images/photage/photage-18.jpeg", alt: "Heavy Duty Towing" },
    { image: "/images/photage/photage-1.jpeg", alt: "Customer Satisfaction" }
  ];

  constructor() { }

  ngOnInit(): void {
    // Component initialization
  }
}