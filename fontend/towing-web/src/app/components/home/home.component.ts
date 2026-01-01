import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Splide } from '@splidejs/splide';
import { HeroComponent } from '../hero/hero.component';

interface FAQ {
  question: string;
  answer: string;
  isOpen: boolean;
}

interface Testimonial {
  text: string;
  authorName: string;
  authorLocation: string;
  authorImage: string;
}

interface Blog {
  title: string;
  excerpt: string;
  date: string;
  image: string;
}

interface Partner {
  name: string;
  image: string;
}

interface Service {
  title: string;
  cssClass: string;
  linkText: string;
  additionalClasses?: string;
}

interface PhotoGallery {
  image: string;
  alt: string;
}

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterModule, HeroComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy, AfterViewInit {
  private partnersSlider: Splide | null = null;

  faqs: FAQ[] = [
    {
      question: "What areas do you serve?",
      answer: "We serve the greater metropolitan area and surrounding suburbs.",
      isOpen: false
    },
    {
      question: "Do you handle small jobs?",
      answer: "Yes, we handle both small and large towing jobs with the same care.",
      isOpen: false
    },
    {
      question: "How soon can you arrive?",
      answer: "We aim to arrive within 30–60 minutes depending on your location.",
      isOpen: false
    },
    {
      question: "Do you offer free estimates?",
      answer: "Yes, we provide no-obligation estimates free of charge.",
      isOpen: false
    },
    {
      question: "How much do your services cost?",
      answer: "Our rates are competitive and based on distance, service, and time of day.",
      isOpen: false
    },
    {
      question: "Do you offer emergency services?",
      answer: "Yes, we offer 24/7 emergency towing services.",
      isOpen: false
    }
  ];

  testimonials: Testimonial[] = [
    {
      text: "Fantastic service! The team was prompt, professional, and handled the tow quickly and safely. They even shared helpful tips to stay safe on the road. Highly recommend Strong Towing for anyone needing reliable roadside support!",
      authorName: "John R.",
      authorLocation: "Chicago, IL",
      authorImage: "/images/testimonial-1.jpg"
    },
    {
      text: "Strong Towing saved the day! Quick, careful, and super professional - they made a stressful situation easy. 100% happy with the service and definitely my go-to for any towing needs!",
      authorName: "Emily S.",
      authorLocation: "Dallas, TX",
      authorImage: "/images/testimonial-2.jpg"
    },
    {
      text: "Called Strong Towing for an emergency at night, and they arrived within 30 minutes. They handled everything quickly and professionally. Exceptional service, fair pricing — glad I found such a dependable team!",
      authorName: "Michael T.",
      authorLocation: "Orlando, FL",
      authorImage: "/images/testimonial-3.jpg"
    }
  ];

  blogs: Blog[] = [
    {
      title: "From Breakdown to Back on the Road - We've Got You",
      excerpt: "With a commitment to speed, safety, and professionalism, we bring the muscle when you need it most. From light-duty to heavy-duty towing, our team handles every job with precision and care.",
      date: "August 29, 2024",
      image: "/images/blog_1.png"
    },
    {
      title: "When the Road Gets Tough, We Get Stronger",
      excerpt: "At Strong Towing, we're more than just a towing service - we're your trusted partner for roadside emergencies.",
      date: "July 15, 2024",
      image: "/images/blog_2.png"
    }
  ];

  partners: Partner[] = [
    { name: "Partner 1", image: "/images/partners/partner-1.jpeg" },
    { name: "Partner 2", image: "/images/partners/partner-2.jpeg" },
    { name: "Partner 3", image: "/images/partners/partner-3.jpeg" },
    { name: "Partner 4", image: "/images/partners/partner-4.jpeg" },
    { name: "Partner 5", image: "/images/partners/partner-5.jpeg" },
    { name: "Partner 6", image: "/images/partners/partner-6.jpeg" },
    { name: "Partner 7", image: "/images/partners/partner-7.jpeg" },
    { name: "Partner 8", image: "/images/partners/partner-8.jpeg" },
    { name: "Partner 9", image: "/images/partners/partner-9.jpeg" }
  ];

  services: Service[] = [
    {
      title: "Towing Services",
      cssClass: "towing-services",
      linkText: "all service options"
    },
    {
      title: "Roadside Assistance Services",
      cssClass: "roadside-assistance-services",
      linkText: "all service options"
    },
    {
      title: "other-services",
      cssClass: "other-services",
      linkText: "all service options",
      additionalClasses: "transition duration-300 ease-in-out hover:text-primary-500"
    }
  ];

  photoGallery: PhotoGallery[] = [
    { image: "/images/photage/photage-1.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-2.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-3.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-4.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-5.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-6.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-7.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-8.jpeg", alt: "Strong Towing Services" },
    { image: "/images/photage/photage-9.jpeg", alt: "Strong Towing Services" }
  ];

  additionalGallery: PhotoGallery[] = [
    { image: "/images/photage/photage-10.jpeg", alt: "Professional Towing Team" },
    { image: "/images/photage/photage-11.jpeg", alt: "Fleet on the Road" },
    { image: "/images/photage/photage-12.jpeg", alt: "Emergency Response" },
    { image: "/images/photage/photage-13.jpeg", alt: "Customer Satisfaction" },
    { image: "/images/photage/photage-14.jpeg", alt: "Heavy Duty Towing" },
    { image: "/images/photage/photage-15.jpeg", alt: "Roadside Assistance" }
  ];

  ngOnInit(): void {
    this.initializeCounters();
  }

  ngAfterViewInit(): void {
    // Wait for Angular to finish rendering
    setTimeout(() => {
      this.initializePartnersSlider();
    }, 100);
  }

  ngOnDestroy(): void {
    if (this.partnersSlider) {
      this.partnersSlider.destroy();
    }
  }

  toggleAccordion(index: number): void {
    // Close all other accordions
    this.faqs.forEach((faq, i) => {
      if (i !== index) {
        faq.isOpen = false;
      }
    });
    
    // Toggle current accordion
    this.faqs[index].isOpen = !this.faqs[index].isOpen;
  }

  private initializePartnersSlider(): void {
    // Check if slider already exists
    if (this.partnersSlider) {
      this.partnersSlider.destroy();
    }

    const sliderElement = document.getElementById('partneres');
    if (sliderElement) {
      this.partnersSlider = new Splide('#partneres', {
        type: 'loop',
        perPage: 4,
        rewind: true,
        autoplay: true,
        interval: 2500,
        speed: 1000,
        arrows: true,
        pagination: true,
        breakpoints: {
          1024: { perPage: 3 },
          768: { perPage: 2 },
          480: { perPage: 1 },
        },
      });
      this.partnersSlider.mount();
    }
  }

  private initializeCounters(): void {
    // Counter animation
    const counters = document.querySelectorAll('.states-number');
    const animateCounter = (el: Element) => {
      const target = +(el.getAttribute('data-target') || 0);
      const suffix = el.getAttribute('data-suffix') || '';
      const duration = 750;
      const stepTime = Math.max(Math.floor(duration / target), 30);
      let current = 0;
      const counterInterval = setInterval(() => {
        current++;
        (el as HTMLElement).innerText = current + suffix;
        if (current >= target) {
          (el as HTMLElement).innerText = target + suffix;
          clearInterval(counterInterval);
        }
      }, stepTime);
    };

    const numberObserver = new IntersectionObserver((entries, obs) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          animateCounter(entry.target);
          obs.unobserve(entry.target);
        }
      });
    }, { threshold: 1.0 });

    counters.forEach((counter) => numberObserver.observe(counter));
  }
}